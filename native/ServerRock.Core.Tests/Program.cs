using System.Net;
using System.Text;
using System.Text.Json;
using ServerRock.Core.Infrastructure;
using ServerRock.Core.Models;
using ServerRock.Core.Services;

var discoveryHandler = new StubHandler((request, _) =>
{
    Check(request.RequestUri?.AbsoluteUri == "http://localhost:11434/v1/models", "model discovery uses the configured /v1 route");
    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent("{\"data\":[{\"id\":\"local-a\"},{\"id\":\"local-b\"}]}", Encoding.UTF8, "application/json")
    });
});
var provider = new OpenAiCompatibleProvider(new HttpClient(discoveryHandler));
var models = await provider.DiscoverModelsAsync("http://localhost:11434/v1");
Check(models.SequenceEqual(["local-a", "local-b"]), "model discovery reads model IDs");

string? streamedPayload = null;
var streamHandler = new StubHandler(async (request, cancellationToken) =>
{
    Check(request.RequestUri?.AbsoluteUri == "http://localhost:11434/v1/chat/completions", "chat endpoint is normalized");
    streamedPayload = await request.Content!.ReadAsStringAsync(cancellationToken);
    var sse = "data: {\"choices\":[{\"delta\":{\"content\":\"Hello \"}}]}\n\n" +
              "data: {\"choices\":[{\"delta\":{\"content\":[{\"type\":\"text\",\"text\":\"world\"}]}}]}\n\n" +
              "data: [DONE]\n\n";
    return new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent(sse, Encoding.UTF8, "text/event-stream")
    };
});
var streamProvider = new OpenAiCompatibleProvider(new HttpClient(streamHandler));
var received = new StringBuilder();
await streamProvider.StreamChatAsync(
    new AppSettings { Endpoint = "http://localhost:11434/v1", Model = "local-a", Temperature = 0.4 },
    [new ChatMessage("user", "Say hello")],
    chunk => received.Append(chunk),
    CancellationToken.None);
Check(received.ToString() == "Hello world", "SSE text and content-part deltas stream in order");
using (var requestJson = JsonDocument.Parse(streamedPayload!))
{
    var root = requestJson.RootElement;
    Check(root.GetProperty("model").GetString() == "local-a", "selected model is sent");
    Check(root.GetProperty("stream").GetBoolean(), "streaming is enabled");
    Check(Math.Abs(root.GetProperty("temperature").GetDouble() - 0.4) < 0.001, "temperature is sent");
    Check(root.GetProperty("messages")[0].GetProperty("role").GetString() == "user", "messages use OpenAI-compatible JSON casing");
}

var badResponseProvider = new OpenAiCompatibleProvider(new HttpClient(new StubHandler((_, _) =>
    Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("model server unavailable") }))));
try
{
    await badResponseProvider.StreamChatAsync(new AppSettings(), [new ChatMessage("user", "test")], _ => { }, CancellationToken.None);
    throw new Exception("Expected an HTTP failure to be surfaced.");
}
catch (HttpRequestException error)
{
    Check(error.Message.Contains("503", StringComparison.Ordinal), "provider HTTP errors include status");
    Check(error.Message.Contains("model server unavailable", StringComparison.Ordinal), "provider HTTP errors include a readable response body");
}

var settingsDirectory = Path.Combine(Path.GetTempPath(), "server-rock-core-check-" + Guid.NewGuid().ToString("N"));
try
{
    var settingsStore = new JsonSettingsStore(Path.Combine(settingsDirectory, "settings.json"));
    settingsStore.Save(new AppSettings { Endpoint = "http://127.0.0.1:1234/v1", Model = "persisted-model", Temperature = 1.2 });
    var reloaded = settingsStore.Load();
    Check(reloaded.Model == "persisted-model" && Math.Abs(reloaded.Temperature - 1.2) < 0.001, "settings survive save and reload");
}
finally
{
    if (Directory.Exists(settingsDirectory)) Directory.Delete(settingsDirectory, recursive: true);
}

Console.WriteLine("Native core checks passed.");

static void Check(bool condition, string description)
{
    if (!condition) throw new InvalidOperationException("Check failed: " + description);
    Console.WriteLine("PASS " + description);
}

sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => respond(request, cancellationToken);
}
