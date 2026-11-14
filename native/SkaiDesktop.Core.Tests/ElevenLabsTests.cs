using System.Net;
using System.Text.Json;
using SkaiDesktop.Core.Services;
using static TestSupport;

internal static class ElevenLabsTests
{
    public static async Task RunAsync()
    {
        HttpRequestMessage? captured = null;
        var capturedBody = "";
        var handler = new StubHandler(request =>
        {
            captured = request;
            capturedBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? "";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent([0x49, 0x44, 0x33, 0x04]),
            };
        });
        using var client = new HttpClient(handler);
        var provider = new ElevenLabsTtsProvider(client);
        var audio = await provider.SynthesizeAsync("Hello Skai", "voice-1", "eleven_multilingual_v2", "xi-test-key");
        Check(audio.MediaType == "audio/mpeg" && audio.Bytes.Length == 4, "ElevenLabs adapter returns bounded audio bytes");
        Check(captured?.Method == HttpMethod.Post && captured.RequestUri?.AbsoluteUri.Contains("voice-1/stream", StringComparison.Ordinal) == true, "ElevenLabs adapter uses the voice streaming endpoint");
        Check(captured?.Headers.TryGetValues("xi-api-key", out var values) == true && values.Single() == "xi-test-key", "ElevenLabs adapter sends the key through the request header");
        using (var document = JsonDocument.Parse(capturedBody))
        {
            Check(document.RootElement.GetProperty("text").GetString() == "Hello Skai" && document.RootElement.GetProperty("model_id").GetString() == "eleven_multilingual_v2", "ElevenLabs adapter sends the bounded text and selected model");
        }

        await ExpectArgumentExceptionAsync(() => provider.SynthesizeAsync("   ", "voice-1", "model", "key"), "ElevenLabs adapter rejects empty speech");
        await ExpectArgumentExceptionAsync(() => provider.SynthesizeAsync("hello", "voice id", "model", "key"), "ElevenLabs adapter rejects unsafe voice identifiers");
    }

    private static async Task ExpectArgumentExceptionAsync(Func<Task> action, string description)
    {
        try
        {
            await action();
            Check(false, description);
        }
        catch (ArgumentException)
        {
            Check(true, description);
        }
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(responder(request));
    }
}
