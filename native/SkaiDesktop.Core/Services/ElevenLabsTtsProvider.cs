using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SkaiDesktop.Core.Infrastructure;

namespace SkaiDesktop.Core.Services;

/// <summary>Bounded transport for the ElevenLabs text-to-speech streaming endpoint.</summary>
public sealed class ElevenLabsTtsProvider(HttpClient? httpClient = null)
{
    private const int MaxTextCharacters = 4_000;
    private const int MaxIdentifierCharacters = 128;
    private const int MaxAudioBytes = 8 * 1024 * 1024;
    private static readonly Uri Endpoint = new("https://api.elevenlabs.io/v1/text-to-speech/");
    private static readonly HttpClient SharedHttp = new() { Timeout = Timeout.InfiniteTimeSpan };
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http = httpClient ?? SharedHttp;

    public async Task<ElevenLabsAudio> SynthesizeAsync(
        string text,
        string voiceId,
        string modelId,
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        var boundedText = ValidateText(text);
        var boundedVoiceId = ValidateIdentifier(voiceId, "voiceId");
        var boundedModelId = ValidateIdentifier(modelId, "modelId");
        var boundedApiKey = ProviderCredentialStore.ValidateValue(apiKey);
        var uri = new Uri(Endpoint, Uri.EscapeDataString(boundedVoiceId) + "/stream?output_format=mp3_22050_32");
        var body = JsonSerializer.Serialize(new
        {
            text = boundedText,
            model_id = boundedModelId,
        }, JsonOptions);

        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.TryAddWithoutValidation("xi-api-key", boundedApiKey);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(45));
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var message = await BoundedHttpContent.ReadTextAsync(response.Content, timeout.Token, "ElevenLabs voice error").ConfigureAwait(false);
            throw new HttpRequestException($"ElevenLabs returned {(int)response.StatusCode} {response.ReasonPhrase}. {message}", null, response.StatusCode);
        }

        var bytes = await BoundedHttpContent.ReadBytesAsync(response.Content, MaxAudioBytes, timeout.Token, "ElevenLabs audio").ConfigureAwait(false);
        if (bytes.Length == 0) throw new InvalidDataException("ElevenLabs returned an empty audio response.");
        return new(bytes, "audio/mpeg");
    }

    private static string ValidateText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Speech text is required.", nameof(value));
        var text = value.Trim();
        if (text.Length > MaxTextCharacters || text.Any(character => char.IsControl(character) && character is not ('\r' or '\n' or '\t')))
            throw new ArgumentException($"Speech text must be at most {MaxTextCharacters:N0} characters.", nameof(value));
        return text.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ').Trim();
    }

    private static string ValidateIdentifier(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"{name} is required.", name);
        var identifier = value.Trim();
        if (identifier.Length > MaxIdentifierCharacters || identifier.Any(char.IsControl) || identifier.Any(char.IsWhiteSpace))
            throw new ArgumentException($"{name} has an invalid format.", name);
        return identifier;
    }
}

public sealed record ElevenLabsAudio(byte[] Bytes, string MediaType);
