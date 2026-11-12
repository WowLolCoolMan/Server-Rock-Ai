using System.Text;
using System.Text.Json;
using ServerRock.Core.Infrastructure;
using ServerRock.Core.Models;

namespace ServerRock.Core.Services;

/// <summary>Small bounded MCP JSON-RPC client for a supervised stdio transport.</summary>
public sealed class McpStdioClient : IAsyncDisposable
{
    private const int MaxMessageCharacters = 256 * 1024;
    private const int MaxArgumentsCharacters = 32 * 1024;
    private const int MaxResultCharacters = 1 * 1024 * 1024;
    private readonly string _serverId;
    private readonly McpConnectionRegistry? _connectionRegistry;
    private readonly StreamReader _input;
    private readonly StreamWriter _output;
    private readonly BoundedHttpContent.LimitedLineReader _lines;
    private readonly SemaphoreSlim _requestLock = new(1, 1);
    private readonly TimeSpan _requestTimeout;
    private int _nextRequestId;
    private bool _initialized;

    public McpStdioClient(string serverId, Stream input, Stream output, TimeSpan? requestTimeout = null, McpConnectionRegistry? connectionRegistry = null)
    {
        if (string.IsNullOrWhiteSpace(serverId) || serverId.Length > 128 || serverId.Any(char.IsControl)) throw new ArgumentException("The MCP server identity is invalid.", nameof(serverId));
        _serverId = serverId;
        _connectionRegistry = connectionRegistry;
        _requestTimeout = requestTimeout ?? TimeSpan.FromSeconds(30);
        if (_requestTimeout < TimeSpan.FromSeconds(1) || _requestTimeout > TimeSpan.FromMinutes(5)) throw new ArgumentOutOfRangeException(nameof(requestTimeout));
        _input = new StreamReader(input ?? throw new ArgumentNullException(nameof(input)), new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
        _output = new StreamWriter(output ?? throw new ArgumentNullException(nameof(output)), new UTF8Encoding(false, true), bufferSize: 4096, leaveOpen: true) { AutoFlush = true, NewLine = "\n" };
        _lines = BoundedHttpContent.CreateLineReader(_input, MaxMessageCharacters);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        using var result = await SendRequestAsync("initialize", new
        {
            protocolVersion = "2025-06-18",
            capabilities = new { },
            clientInfo = new { name = ProductIdentity.ClientName, version = "0.1" }
        }, cancellationToken).ConfigureAwait(false);
        if (!result.RootElement.TryGetProperty("result", out var initializeResult) || !initializeResult.TryGetProperty("protocolVersion", out var protocolVersion) || protocolVersion.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(protocolVersion.GetString()))
            throw new InvalidDataException("The MCP initialize response did not declare a protocol version.");
        await SendNotificationAsync("notifications/initialized", new { }, cancellationToken).ConfigureAwait(false);
        _initialized = true;
    }

    public async Task<IReadOnlyList<McpToolDescriptor>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        using var result = await SendRequestAsync("tools/list", new { }, cancellationToken).ConfigureAwait(false);
        if (!result.RootElement.TryGetProperty("result", out var listResult) || !listResult.TryGetProperty("tools", out var tools) || tools.ValueKind != JsonValueKind.Array || tools.GetArrayLength() > 256)
            throw new InvalidDataException("The MCP tools/list response is missing a bounded tools array.");
        var descriptors = new List<McpToolDescriptor>(tools.GetArrayLength());
        foreach (var tool in tools.EnumerateArray())
        {
            if (!tool.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String) throw new InvalidDataException("The MCP tool manifest contains a tool without a name.");
            var toolName = name.GetString() ?? "";
            var description = tool.TryGetProperty("description", out var descriptionValue) && descriptionValue.ValueKind == JsonValueKind.String ? descriptionValue.GetString() ?? "" : "";
            var schema = tool.TryGetProperty("inputSchema", out var schemaValue) ? schemaValue.GetRawText() : "{}";
            var readOnly = tool.TryGetProperty("annotations", out var annotations) && annotations.ValueKind == JsonValueKind.Object && annotations.TryGetProperty("readOnlyHint", out var hint) && hint.ValueKind == JsonValueKind.True;
            descriptors.Add(new McpToolDescriptor
            {
                ServerId = _serverId,
                ToolName = toolName,
                Description = description,
                InputSchemaJson = schema,
                IsMutating = !readOnly,
                RequiredCapabilities = [readOnly ? McpCapabilities.InvokeRead : McpCapabilities.InvokeWrite]
            });
        }
        return descriptors;
    }

    public async Task<McpToolCallResult> CallToolAsync(McpToolDescriptor descriptor, McpToolGrant grant, JsonElement arguments, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        if (arguments.ValueKind != JsonValueKind.Object) throw new ArgumentException("MCP tool arguments must be a JSON object.", nameof(arguments));
        var argumentsJson = arguments.GetRawText();
        if (Encoding.UTF8.GetByteCount(argumentsJson) > MaxArgumentsCharacters) throw new InvalidDataException("MCP tool arguments exceed the bounded payload limit.");
        if (_connectionRegistry is null) McpToolPermissionGate.Authorize(descriptor, grant);
        else _connectionRegistry.Authorize(descriptor, grant);
        using var result = await SendRequestAsync("tools/call", new { name = descriptor.ToolName, arguments }, cancellationToken).ConfigureAwait(false);
        if (!result.RootElement.TryGetProperty("result", out var callResult)) throw new InvalidDataException("The MCP tools/call response is missing a result.");
        var isError = callResult.TryGetProperty("isError", out var error) && error.ValueKind == JsonValueKind.True;
        var text = new StringBuilder();
        if (callResult.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in content.EnumerateArray())
            {
                if (!item.TryGetProperty("text", out var value) || value.ValueKind != JsonValueKind.String) continue;
                var part = value.GetString() ?? "";
                if (text.Length + part.Length > MaxResultCharacters) throw new InvalidDataException("The MCP tool result exceeds the bounded output limit.");
                text.Append(part);
            }
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
            await WriteMessageAsync(new { jsonrpc = "2.0", id, method, @params = parameters }, timeout.Token).ConfigureAwait(false);
            var line = await _lines.ReadLineAsync(timeout.Token).ConfigureAwait(false) ?? throw new EndOfStreamException("The MCP server closed its stdio stream.");
            if (line.Length > MaxMessageCharacters) throw new InvalidDataException("The MCP response exceeds the bounded message limit.");
            JsonDocument document;
            try { document = JsonDocument.Parse(line); } catch (JsonException exception) { throw new InvalidDataException("The MCP server returned invalid JSON.", exception); }
            if (!document.RootElement.TryGetProperty("id", out var responseId) || responseId.ValueKind != JsonValueKind.Number || responseId.GetInt32() != id)
            {
                document.Dispose();
                throw new InvalidDataException("The MCP response did not match the outstanding request.");
            }
            if (document.RootElement.TryGetProperty("error", out var error))
            {
                var message = error.TryGetProperty("message", out var errorMessage) && errorMessage.ValueKind == JsonValueKind.String ? errorMessage.GetString() : "unknown MCP error";
                document.Dispose();
                throw new InvalidOperationException($"The MCP server rejected {method}: {message}");
            }
            return document;
        }
        finally { _requestLock.Release(); }
    }

    private async Task SendNotificationAsync(string method, object parameters, CancellationToken cancellationToken) => await WriteMessageAsync(new { jsonrpc = "2.0", method, @params = parameters }, cancellationToken).ConfigureAwait(false);

    private async Task WriteMessageAsync(object message, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(message);
        if (json.Length > MaxMessageCharacters) throw new InvalidDataException("The MCP request exceeds the bounded message limit.");
        await _output.WriteLineAsync(json.AsMemory(), cancellationToken).ConfigureAwait(false);
    }

    private void EnsureInitialized()
    {
        if (!_initialized) throw new InvalidOperationException("The MCP server must be initialized before a tool call.");
    }

    public ValueTask DisposeAsync()
    {
        _output.Dispose();
        _input.Dispose();
        _requestLock.Dispose();
        return ValueTask.CompletedTask;
    }
}
