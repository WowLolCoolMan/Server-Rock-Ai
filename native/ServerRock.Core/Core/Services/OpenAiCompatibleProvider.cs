using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ServerRock.Core.Infrastructure;
using ServerRock.Core.Models;

namespace ServerRock.Core.Services;

/// <summary>Transport for local and compatible chat-completions servers.</summary>
public sealed class OpenAiCompatibleProvider(HttpClient? httpClient = null)
{
    private const int MaxMessages = 500;
    private const int MaxMessageCharacters = 1_000_000;
    private const int MaxStreamedResponseCharacters = 1_000_000;
    private const int MaxRequestCharacters = 8_000_000;
    private const int MaxModelCharacters = 512;
    private static readonly HttpClient SharedHttp = new() { Timeout = Timeout.InfiniteTimeSpan };
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http = httpClient ?? SharedHttp;

    public async Task StreamChatAsync(
        AppSettings settings,
        IReadOnlyList<ChatMessage> messages,
        Action<string> onText,
        CancellationToken cancellationToken,
        int? maxTokens = null)
    {
        ValidateRequest(settings, messages);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(5));
        var requestBody = new Dictionary<string, object?>
        {
            ["model"] = settings.Model,
            ["temperature"] = settings.Temperature,
            ["stream"] = true,
            ["messages"] = messages.Select(message => new
            {
                message.Role,
                content = BuildModelContent(message, UsesOllamaImageUrlString(settings))
            }).ToArray()
        };
        if (maxTokens is > 0) requestBody["max_tokens"] = Math.Clamp(maxTokens.Value, 1, 4096);
        var requestJson = JsonSerializer.Serialize(requestBody, JsonOptions);
        if (Encoding.UTF8.GetByteCount(requestJson) > MaxRequestCharacters)
            throw new InvalidOperationException("The model request exceeds the 8 MB payload limit.");
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildChatUri(settings.Endpoint))
        {
            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
        };
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        if (!response.IsSuccessStatusCode)
        {
            var body = await BoundedHttpContent.ReadTextAsync(response.Content, timeout.Token, "Model server error", BoundedHttpContent.MaxTextBytes);
            throw new HttpRequestException(
                $"The model server returned {(int)response.StatusCode} {response.ReasonPhrase}. {body}",
                null,
                response.StatusCode);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
        using var reader = new StreamReader(stream);
        var lineReader = BoundedHttpContent.CreateLineReader(reader);
        var receivedContent = false;
        var receivedCharacters = 0;
        while (await lineReader.ReadLineAsync(timeout.Token) is { } line)
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
            if (text.Length > MaxStreamedResponseCharacters - receivedCharacters)
                throw new InvalidDataException($"The model response exceeds the {MaxStreamedResponseCharacters:N0}-character limit.");
            receivedCharacters += text.Length;
            receivedContent = true;
            onText(text);
        }
        if (!receivedContent) throw new InvalidOperationException("The model server closed the stream without returning a response.");
    }

    private static object BuildModelContent(ChatMessage message, bool ollamaImageUrlString)
    {
        var images = message.Attachments?.Where(attachment => attachment.IsImage).ToArray() ?? [];
        if (images.Length == 0) return message.Content;
        long totalImageBytes = 0;
        foreach (var image in images)
        {
            if (!ChatAttachmentPolicy.IsAllowedImageMediaType(image.MediaType) ||
                ChatAttachmentPolicy.GetImageMediaType(image.FileName) != image.MediaType)
                throw new InvalidOperationException("An attached image has an unsupported media type. Add a PNG or JPEG image and try again.");
            byte[] bytes;
            try { bytes = Convert.FromBase64String(image.Content); }
            catch (FormatException error) { throw new InvalidOperationException("An attached image is not valid base64 data. Remove it and attach the image again.", error); }
            if (bytes.Length > ChatAttachmentPolicy.MaxFileBytes)
                throw new InvalidOperationException("An attached image exceeds the 2 MB per-image limit. Remove it and attach a smaller image.");
            if (!ChatAttachmentPolicy.HasValidImageSignature(image.MediaType, bytes))
                throw new InvalidOperationException($"{image.FileName} does not contain valid {image.MediaType} image data. Remove it and attach the image again.");
            totalImageBytes += bytes.Length;
        }
        if (totalImageBytes > ChatAttachmentPolicy.MaxTotalImageBytes)
            throw new InvalidOperationException("Attached images exceed the 6 MB total limit. Remove one or more images and try again.");
        var parts = new List<object>();
        if (!string.IsNullOrWhiteSpace(message.Content)) parts.Add(new { type = "text", text = message.Content });
        parts.AddRange(images.Select(image => ollamaImageUrlString
            ? (object)new { type = "image_url", image_url = $"data:{image.MediaType};base64,{image.Content}" }
            : new { type = "image_url", image_url = new { url = $"data:{image.MediaType};base64,{image.Content}", detail = "auto" } }));
        return parts;
    }

    private static void ValidateRequest(AppSettings settings, IReadOnlyList<ChatMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(messages);
        if (messages.Count is < 1 or > MaxMessages)
            throw new InvalidOperationException($"A chat request must contain between 1 and {MaxMessages} messages.");
        if (string.IsNullOrWhiteSpace(settings.Model) || settings.Model.Length > MaxModelCharacters || settings.Model.Any(char.IsControl))
            throw new InvalidOperationException("The selected model identifier is invalid.");

        var totalCharacters = 0L;
        foreach (var message in messages)
        {
            if (message is null || string.IsNullOrWhiteSpace(message.Role) || message.Role.Length > 64 || message.Role.Any(char.IsControl))
                throw new InvalidOperationException("A chat message contains an invalid role.");
            if (message.Content is null)
                throw new InvalidOperationException("A chat message must contain text.");
            if (message.Content.Length > MaxMessageCharacters)
                throw new InvalidOperationException("A chat message exceeds the 1 MB content limit.");
            totalCharacters += message.Role.Length + message.Content.Length;
            if (message.Attachments is { Count: > ChatAttachmentPolicy.MaxFiles })
                throw new InvalidOperationException($"A chat message can contain at most {ChatAttachmentPolicy.MaxFiles} attachments.");
            foreach (var attachment in message.Attachments ?? [])
            {
                var maxAttachmentCharacters = attachment?.IsImage == true
                    ? ((ChatAttachmentPolicy.MaxFileBytes + 2) / 3) * 4 + 4
                    : ChatAttachmentPolicy.MaxCharactersPerFile;
                if (attachment is null || attachment.Content is null || attachment.Content.Length > maxAttachmentCharacters)
                    throw new InvalidOperationException("A chat attachment exceeds its supported content limit.");
                totalCharacters += attachment.Content.Length;
            }
            if (totalCharacters > MaxRequestCharacters)
                throw new InvalidOperationException("The model request exceeds the 8 MB content limit.");
        }
    }

    private static bool UsesOllamaImageUrlString(AppSettings settings)
    {
        if (settings.VisionApiFormat.Equals("ollama", StringComparison.OrdinalIgnoreCase)) return true;
        if (settings.VisionApiFormat.Equals("openai", StringComparison.OrdinalIgnoreCase)) return false;
        return Uri.TryCreate(settings.Endpoint.Trim().TrimEnd('/'), UriKind.Absolute, out var endpoint) &&
            endpoint.IsLoopback && endpoint.Port == 11434;
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
        using var json = await BoundedHttpContent.ReadJsonAsync(response.Content, timeout.Token, "Model server model list");
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
            || uri.Scheme == "http" && !uri.IsLoopback
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Fragment))
            throw new InvalidOperationException("Enter an HTTP or HTTPS model endpoint without embedded credentials or a URL fragment.");
        return uri;
    }
}
