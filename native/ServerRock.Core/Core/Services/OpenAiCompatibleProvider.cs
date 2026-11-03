using System.Net.Http.Json;
using System.Text.Json;
using ServerRock.Core.Models;

namespace ServerRock.Core.Services;

/// <summary>Transport for local and compatible chat-completions servers.</summary>
public sealed class OpenAiCompatibleProvider(HttpClient? httpClient = null)
{
    private static readonly HttpClient SharedHttp = new() { Timeout = Timeout.InfiniteTimeSpan };
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http = httpClient ?? SharedHttp;

    public async Task StreamChatAsync(
        AppSettings settings,
        IReadOnlyList<ChatMessage> messages,
        Action<string> onText,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(5));
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildChatUri(settings.Endpoint))
        {
            Content = JsonContent.Create(new
            {
                model = settings.Model,
                temperature = settings.Temperature,
                stream = true,
                messages
            }, options: JsonOptions)
        };
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(timeout.Token);
            throw new HttpRequestException(
                $"The model server returned {(int)response.StatusCode} {response.ReasonPhrase}. {body}",
                null,
                response.StatusCode);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
        using var reader = new StreamReader(stream);
        var receivedContent = false;
        while (await reader.ReadLineAsync(timeout.Token) is { } line)
        {
            timeout.Token.ThrowIfCancellationRequested();
            if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;
            var data = line[5..].Trim();
            if (data is "[DONE]" or "") continue;

            using var chunk = JsonDocument.Parse(data);
            if (!chunk.RootElement.TryGetProperty("choices", out var choices)
                || choices.ValueKind != JsonValueKind.Array
                || choices.GetArrayLength() == 0
                || !choices[0].TryGetProperty("delta", out var delta)
                || !delta.TryGetProperty("content", out var content)) continue;

            var text = content.ValueKind switch
            {
                JsonValueKind.String => content.GetString(),
                JsonValueKind.Array => string.Join("", content.EnumerateArray()
                    .Where(part => part.TryGetProperty("text", out _))
                    .Select(part => part.GetProperty("text").GetString())),
                _ => null
            };
            if (string.IsNullOrEmpty(text)) continue;
            receivedContent = true;
            onText(text);
        }
        if (!receivedContent) throw new InvalidOperationException("The model server closed the stream without returning a response.");
    }

    public async Task<IReadOnlyList<string>> DiscoverModelsAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        var root = BuildBaseUri(endpoint);
        var path = root.AbsolutePath.TrimEnd('/');
        var modelsUri = path.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
            ? new UriBuilder(root) { Path = path + "/models", Query = "" }.Uri
            : new UriBuilder(root) { Path = path + "/v1/models", Query = "" }.Uri;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        using var response = await _http.GetAsync(modelsUri, timeout.Token);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
        if (!json.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("The provider response did not include a model list.");
        var models = data.EnumerateArray()
            .Where(item => item.TryGetProperty("id", out _))
            .Select(item => item.GetProperty("id").GetString())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (models.Length == 0) throw new InvalidOperationException("The provider returned no models.");
        return models;
    }

    private static Uri BuildChatUri(string endpoint)
    {
        var uri = BuildBaseUri(endpoint);
        var path = uri.AbsolutePath.TrimEnd('/');
        if (!path.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
        {
            if (!path.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)) path += "/v1";
            path += "/chat/completions";
        }
        return new UriBuilder(uri) { Path = path, Query = "" }.Uri;
    }

    private static Uri BuildBaseUri(string endpoint)
    {
        if (!Uri.TryCreate(endpoint.Trim().TrimEnd('/'), UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https")
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Fragment))
            throw new InvalidOperationException("Enter an HTTP or HTTPS model endpoint without embedded credentials or a URL fragment.");
        return uri;
    }
}
