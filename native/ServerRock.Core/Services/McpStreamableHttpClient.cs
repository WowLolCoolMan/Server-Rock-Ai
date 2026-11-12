using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ServerRock.Core.Infrastructure;
using ServerRock.Core.Models;

namespace ServerRock.Core.Services;

/// <summary>Bounded JSON response path for an MCP Streamable HTTP endpoint.</summary>
public sealed class McpStreamableHttpClient : IAsyncDisposable
{
    private const int MaxMessageCharacters = 256 * 1024;
    private const int MaxArgumentsCharacters = 32 * 1024;
    private const int MaxResultCharacters = 1 * 1024 * 1024;
    private readonly string _serverId;
    private readonly Uri _endpoint;
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;
    private readonly McpConnectionRegistry? _connectionRegistry;
    private readonly SemaphoreSlim _requestLock = new(1, 1);
    private readonly TimeSpan _requestTimeout;
    private int _nextRequestId;
    private bool _initialized;
    private string? _sessionId;
    private string? _protocolVersion;

    public McpStreamableHttpClient(string serverId, string endpoint, HttpClient? httpClient = null, TimeSpan? requestTimeout = null, McpConnectionRegistry? connectionRegistry = null)
    {
        if (string.IsNullOrWhiteSpace(serverId) || serverId.Length > 128 || serverId.Any(char.IsControl)) throw new ArgumentException("The MCP server identity is invalid.", nameof(serverId));
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var parsedEndpoint) || parsedEndpoint.Scheme is not ("https" or "http") || !string.IsNullOrEmpty(parsedEndpoint.UserInfo) || !parsedEndpoint.Query.Equals("", StringComparison.Ordinal) || !parsedEndpoint.Fragment.Equals("", StringComparison.Ordinal) || (parsedEndpoint.Scheme == "http" && !IsLoopback(parsedEndpoint))) throw new ArgumentException("The MCP endpoint must be HTTPS, or loopback HTTP without credentials or query data.", nameof(endpoint));
        _endpoint = parsedEndpoint;
        _serverId = serverId;
        _connectionRegistry = connectionRegistry;
        _requestTimeout = requestTimeout ?? TimeSpan.FromSeconds(30);
        if (_requestTimeout < TimeSpan.FromSeconds(1) || _requestTimeout > TimeSpan.FromMinutes(5)) throw new ArgumentOutOfRangeException(nameof(requestTimeout));
        _http = httpClient ?? new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        _ownsHttp = httpClient is null;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        using var response = await SendRequestAsync("initialize", new { protocolVersion = "2025-06-18", capabilities = new { }, clientInfo = new { name = ProductIdentity.ClientName, version = "0.1" } }, cancellationToken).ConfigureAwait(false);
        if (!response.RootElement.TryGetProperty("result", out var result) || !result.TryGetProperty("protocolVersion", out var version) || version.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(version.GetString())) throw new InvalidDataException("The MCP initialize response did not declare a protocol version.");
        _protocolVersion = version.GetString();
        _initialized = true;
    }

    public async Task<IReadOnlyList<McpToolDescriptor>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        using var response = await SendRequestAsync("tools/list", new { }, cancellationToken).ConfigureAwait(false);
        if (!response.RootElement.TryGetProperty("result", out var result) || !result.TryGetProperty("tools", out var tools) || tools.ValueKind != JsonValueKind.Array || tools.GetArrayLength() > 256) throw new InvalidDataException("The MCP tools/list response is missing a bounded tools array.");
        var descriptors = new List<McpToolDescriptor>(tools.GetArrayLength());
        foreach (var tool in tools.EnumerateArray())
        {
            if (!tool.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String) throw new InvalidDataException("The MCP tool manifest contains a tool without a name.");
            var readOnly = tool.TryGetProperty("annotations", out var annotations) && annotations.ValueKind == JsonValueKind.Object && annotations.TryGetProperty("readOnlyHint", out var hint) && hint.ValueKind == JsonValueKind.True;
            descriptors.Add(new McpToolDescriptor { ServerId = _serverId, ToolName = name.GetString() ?? "", Description = tool.TryGetProperty("description", out var description) && description.ValueKind == JsonValueKind.String ? description.GetString() ?? "" : "", InputSchemaJson = tool.TryGetProperty("inputSchema", out var schema) ? schema.GetRawText() : "{}", IsMutating = !readOnly, RequiredCapabilities = [readOnly ? McpCapabilities.InvokeRead : McpCapabilities.InvokeWrite] });
        }
        return descriptors;
    }

    public async Task<McpToolCallResult> CallToolAsync(McpToolDescriptor descriptor, McpToolGrant grant, JsonElement arguments, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        if (arguments.ValueKind != JsonValueKind.Object) throw new ArgumentException("MCP tool arguments must be a JSON object.", nameof(arguments));
        if (Encoding.UTF8.GetByteCount(arguments.GetRawText()) > MaxArgumentsCharacters) throw new InvalidDataException("MCP tool arguments exceed the bounded payload limit.");
        if (_connectionRegistry is null) McpToolPermissionGate.Authorize(descriptor, grant); else _connectionRegistry.Authorize(descriptor, grant);
        using var response = await SendRequestAsync("tools/call", new { name = descriptor.ToolName, arguments }, cancellationToken).ConfigureAwait(false);
        if (!response.RootElement.TryGetProperty("result", out var result)) throw new InvalidDataException("The MCP tools/call response is missing a result.");
        var isError = result.TryGetProperty("isError", out var error) && error.ValueKind == JsonValueKind.True;
        var text = new StringBuilder();
        if (result.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
            foreach (var item in content.EnumerateArray())
                if (item.TryGetProperty("text", out var value) && value.ValueKind == JsonValueKind.String)
                {
                    var part = value.GetString() ?? "";
                    if (text.Length + part.Length > MaxResultCharacters) throw new InvalidDataException("The MCP tool result exceeds the bounded output limit.");
                    text.Append(part);
                }
        return new McpToolCallResult(isError, text.ToString());
    }

    private async Task<JsonDocument> SendRequestAsync(string method, object parameters, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_requestTimeout);
        await _requestLock.WaitAsync(timeout.Token).ConfigureAwait(false);
        try
        {
            var id = Interlocked.Increment(ref _nextRequestId);
            var body = JsonSerializer.Serialize(new { jsonrpc = "2.0", id, method, @params = parameters });
            if (body.Length > MaxMessageCharacters) throw new InvalidDataException("The MCP request exceeds the bounded message limit.");
            using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
            if (!string.IsNullOrWhiteSpace(_protocolVersion)) request.Headers.TryAddWithoutValidation("MCP-Protocol-Version", _protocolVersion);
            if (!string.IsNullOrWhiteSpace(_sessionId)) request.Headers.TryAddWithoutValidation("Mcp-Session-Id", _sessionId);
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            if ((int)response.StatusCode is >= 300 and < 400) throw new HttpRequestException("MCP endpoint redirects are refused.");
            if (!response.IsSuccessStatusCode) throw new HttpRequestException($"The MCP endpoint returned {(int)response.StatusCode} {response.ReasonPhrase}.");
            using var document = await ReadJsonRpcResponseAsync(response.Content, timeout.Token).ConfigureAwait(false);
            if (string.Equals(method, "initialize", StringComparison.Ordinal) && response.Headers.TryGetValues("Mcp-Session-Id", out var sessionValues))
            {
                var session = sessionValues.SingleOrDefault();
                if (string.IsNullOrWhiteSpace(session) || session.Length > 256 || session.Any(char.IsControl)) throw new InvalidDataException("The MCP session identifier is invalid.");
                _sessionId = session;
            }
            if (!document.RootElement.TryGetProperty("id", out var responseId) || responseId.ValueKind != JsonValueKind.Number || responseId.GetInt32() != id) throw new InvalidDataException("The MCP response did not match the outstanding request.");
            if (document.RootElement.TryGetProperty("error", out var error)) throw new InvalidOperationException($"The MCP server rejected {method}: {error.GetRawText()}");
            return JsonDocument.Parse(document.RootElement.GetRawText());
        }
        finally { _requestLock.Release(); }
    }

    private void EnsureInitialized() { if (!_initialized) throw new InvalidOperationException("The MCP server must be initialized before a tool call."); }
    private static bool IsLoopback(Uri uri) => uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || System.Net.IPAddress.TryParse(uri.Host, out var address) && System.Net.IPAddress.IsLoopback(address);

    private static async Task<JsonDocument> ReadJsonRpcResponseAsync(HttpContent content, CancellationToken cancellationToken)
    {
        var mediaType = content.Headers.ContentType?.MediaType;
        if (!string.Equals(mediaType, "text/event-stream", StringComparison.OrdinalIgnoreCase)) return await BoundedHttpContent.ReadJsonAsync(content, cancellationToken, "MCP response").ConfigureAwait(false);
        await using var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: false);
        var lines = BoundedHttpContent.CreateLineReader(reader, MaxMessageCharacters);
        while (await lines.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;
            var data = line[5..].Trim();
            if (data is "" or "[DONE]") continue;
            try { return JsonDocument.Parse(data); }
            catch (JsonException exception) { throw new InvalidDataException("The MCP event stream returned invalid JSON.", exception); }
        }
        throw new EndOfStreamException("The MCP event stream closed without a JSON-RPC response.");
    }

    public ValueTask DisposeAsync()
    {
        if (_ownsHttp) _http.Dispose();
        _requestLock.Dispose();
        return ValueTask.CompletedTask;
    }
}
