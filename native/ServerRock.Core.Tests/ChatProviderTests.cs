// OpenAI-compatible provider: model discovery, OpenRouter key handling, SSE streaming,
// request shape, image payloads, and size limits (OpenAiCompatibleProvider).
// Uses TestSupport (Check, StubHandler, …). Run by Program.cs.

using System.Net;
using System.Text;
using System.Text.Json;
using ServerRock.Core.Infrastructure;
using ServerRock.Core.Models;
using ServerRock.Core.Services;
using static TestSupport;

internal static class ChatProviderTests
{
    public static async Task RunAsync()
    {
        // Same PNG header bytes that ChatMessageTests uses.
        var pngSignature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
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
        var profileProvider = new OpenAiCompatibleProvider(new HttpClient(new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"data\":[{\"id\":\"vision-model\",\"capabilities\":[\"text\",\"vision\"]},{\"id\":\"text-model\",\"modalities\":[\"text\"]},{\"id\":\"flagged-model\",\"supports_vision\":true}]}", Encoding.UTF8, "application/json")
        }))));
        var profiles = await profileProvider.DiscoverModelProfilesAsync("http://localhost:11434/v1");
        Check(profiles.Single(profile => profile.Model == "vision-model").Vision == CapabilitySupport.Supported
            && profiles.Single(profile => profile.Model == "flagged-model").Vision == CapabilitySupport.Supported
            && profiles.Single(profile => profile.Model == "text-model").Vision == CapabilitySupport.Unknown,
            "model discovery parses bounded provider capability metadata");
        var openRouterAuthHandler = new StubHandler((request, _) =>
        {
            Check(request.RequestUri?.AbsoluteUri == "https://openrouter.ai/api/v1/models" && request.Headers.Authorization?.Scheme == "Bearer" && request.Headers.Authorization.Parameter == "sk-or-v1-test", "OpenRouter model discovery sends the session API key without persisting it");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":[{\"id\":\"openrouter/free\"}]}", Encoding.UTF8, "application/json")
            });
        });
        var openRouterModels = await new OpenAiCompatibleProvider(new HttpClient(openRouterAuthHandler)).DiscoverModelsAsync("https://openrouter.ai/api/v1", apiKey: "sk-or-v1-test");
        Check(openRouterModels.SequenceEqual(["openrouter/free"]), "OpenRouter free router is discoverable through the OpenAI-compatible API");

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
            [new ChatMessage("user", "Say hello", [new ChatAttachment("notes.md", "private note")], [new ChatSource("Docs", "https://example.org", "A snippet")]).ForModel("Say hello with selected context")],
            chunk => received.Append(chunk),
            CancellationToken.None);
        Check(received.ToString() == "Hello world", "SSE text and content-part deltas stream in order");
        var excessiveSse = string.Concat(Enumerable.Range(0, 8).Select(_ =>
            "data: {\"choices\":[{\"delta\":{\"content\":\"" + new string('x', 131_072) + "\"}}]}\n\n"));
        var excessiveStreamProvider = new OpenAiCompatibleProvider(new HttpClient(new StubHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(excessiveSse, Encoding.UTF8, "text/event-stream")
            }))));
        try
        {
            await excessiveStreamProvider.StreamChatAsync(new AppSettings { Endpoint = "http://localhost:11434/v1", Model = "local-a" },
                [new ChatMessage("user", "Return a bounded answer")], _ => { }, CancellationToken.None);
            throw new Exception("Expected excessive streamed output to be rejected.");
        }
        catch (InvalidDataException)
        {
            Check(true, "native provider bounds cumulative streamed response text");
        }
        using (var requestJson = JsonDocument.Parse(streamedPayload!))
        {
            var root = requestJson.RootElement;
            Check(root.GetProperty("model").GetString() == "local-a", "selected model is sent");
            Check(root.GetProperty("stream").GetBoolean(), "streaming is enabled");
            Check(Math.Abs(root.GetProperty("temperature").GetDouble() - 0.4) < 0.001, "temperature is sent");
            Check(root.GetProperty("messages")[0].GetProperty("role").GetString() == "user", "messages use OpenAI-compatible JSON casing");
            Check(root.GetProperty("messages")[0].GetProperty("content").GetString() == "Say hello with selected context" && !root.GetProperty("messages")[0].TryGetProperty("attachments", out _) && !root.GetProperty("messages")[0].TryGetProperty("sources", out _), "provider payload includes selected context but excludes local-only metadata");
            Check(!root.TryGetProperty("max_tokens", out _), "normal chat requests omit benchmark-only output limits");
        }

        string? benchmarkPayload = null;
        var benchmarkProvider = new OpenAiCompatibleProvider(new HttpClient(new StubHandler(async (request, cancellationToken) =>
        {
            benchmarkPayload = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("data: {\"choices\":[{\"delta\":{\"content\":\"A fixed benchmark response.\"}}]}\n\ndata: [DONE]\n\n", Encoding.UTF8, "text/event-stream") };
        })));
        await benchmarkProvider.StreamChatAsync(new AppSettings { Endpoint = "http://localhost:11434/v1", Model = "model-to-compare", Temperature = 0 }, [new ChatMessage("user", "same fixed benchmark prompt")], _ => { }, CancellationToken.None, maxTokens: 96);
        using (var benchmarkRequest = JsonDocument.Parse(benchmarkPayload!))
        {
            Check(benchmarkRequest.RootElement.GetProperty("model").GetString() == "model-to-compare" && benchmarkRequest.RootElement.GetProperty("temperature").GetDouble() == 0, "local model benchmark uses the selected model with deterministic temperature");
            Check(benchmarkRequest.RootElement.GetProperty("max_tokens").GetInt32() == 96, "local model benchmark requests a bounded output length");
        }
        var oversizedProvider = new OpenAiCompatibleProvider(new HttpClient(new StubHandler((_, _) => throw new Exception("Oversized native provider input must be rejected before network access."))));
        try
        {
            await oversizedProvider.StreamChatAsync(new AppSettings { Endpoint = "http://localhost:11434/v1", Model = "local-a" },
                [new ChatMessage("user", new string('x', 1_000_001))], _ => { }, CancellationToken.None);
            throw new Exception("Expected oversized native provider content to be rejected.");
        }
        catch (InvalidOperationException)
        {
            Check(true, "native provider request content is bounded before serialization and network access");
        }

        string? compactedPayload = null;
        var compactionProvider = new OpenAiCompatibleProvider(new HttpClient(new StubHandler(async (request, cancellationToken) =>
        {
            compactedPayload = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("data: {\"choices\":[{\"delta\":{\"content\":\"compacted response\"}}]}\n\ndata: [DONE]\n\n", Encoding.UTF8, "text/event-stream") };
        })));
        var oversizedHistory = Enumerable.Range(0, 10).Select(index => new ChatMessage(index % 2 == 0 ? "user" : "assistant", index == 9 ? "latest request" : new string((char)('a' + index), 999_000))).ToList();
        await compactionProvider.StreamChatAsync(new AppSettings { Endpoint = "http://localhost:11434/v1", Model = "local-a" }, oversizedHistory, _ => { }, CancellationToken.None);
        Check(compactedPayload is not null, "native provider sends a bounded oversized-history request");
        Check(compactedPayload!.Contains("Earlier conversation omitted", StringComparison.Ordinal) && compactedPayload.Contains("latest request", StringComparison.Ordinal), "native provider compacts oversized conversation context before network access");

        string? imagePayload = null;
        var imageProvider = new OpenAiCompatibleProvider(new HttpClient(new StubHandler(async (request, cancellationToken) =>
        {
            imagePayload = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("data: {\"choices\":[{\"delta\":{\"content\":\"I see an image.\"}}]}\n\ndata: [DONE]\n\n", Encoding.UTF8, "text/event-stream")
            };
        })));
        await imageProvider.StreamChatAsync(
            new AppSettings { Endpoint = "http://localhost:1234/v1", Model = "vision-local", VisionApiFormat = "openai" },
            [new ChatMessage("user", "Describe this", [new ChatAttachment("sample.png", Convert.ToBase64String(pngSignature), "image/png")]).ForModel("Describe this")],
            _ => { },
            CancellationToken.None);
        using (var imageRequest = JsonDocument.Parse(imagePayload!))
        {
            var content = imageRequest.RootElement.GetProperty("messages")[0].GetProperty("content");
            Check(content.ValueKind == JsonValueKind.Array && content[0].GetProperty("type").GetString() == "text" && content[1].GetProperty("type").GetString() == "image_url", "vision attachments use OpenAI-compatible text and image content parts");
            Check(content[1].GetProperty("image_url").GetProperty("url").GetString() == "data:image/png;base64," + Convert.ToBase64String(pngSignature), "vision image payload carries a bounded data URL");
        }
        string? ollamaImagePayload = null;
        var ollamaImageProvider = new OpenAiCompatibleProvider(new HttpClient(new StubHandler(async (request, cancellationToken) =>
        {
            ollamaImagePayload = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("data: {\"choices\":[{\"delta\":{\"content\":\"Ollama image received.\"}}]}\n\ndata: [DONE]\n\n", Encoding.UTF8, "text/event-stream")
            };
        })));
        await ollamaImageProvider.StreamChatAsync(
            new AppSettings { Endpoint = "http://localhost:11434/v1", Model = "vision-local", VisionApiFormat = "auto" },
            [new ChatMessage("user", "Describe this", [new ChatAttachment("sample.png", Convert.ToBase64String(pngSignature), "image/png")]).ForModel("Describe this")],
            _ => { },
            CancellationToken.None);
        using (var ollamaImageRequest = JsonDocument.Parse(ollamaImagePayload!))
        {
            var content = ollamaImageRequest.RootElement.GetProperty("messages")[0].GetProperty("content");
            Check(content[1].GetProperty("image_url").ValueKind == JsonValueKind.String, "Ollama's default local endpoint uses its documented image URL string shape");
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

        // Local endpoint check and error messages (moved from MainPage into Core).
        Check(LocalEndpoint.IsLocal("http://localhost:11434/v1") && LocalEndpoint.IsLocal("http://127.0.0.1:1234/v1") && LocalEndpoint.IsLocal("http://[::1]:8080"),
            "loopback and localhost endpoints count as on-device");
        Check(!LocalEndpoint.IsLocal("http://192.168.1.20:11434/v1") && !LocalEndpoint.IsLocal("https://openrouter.ai/api/v1") && !LocalEndpoint.IsLocal("not a url"),
            "LAN, cloud, and malformed endpoints do not count as on-device");
        var profile = ModelCapabilityResolver.Resolve(new AppSettings { ModelProvider = "Ollama", Model = "llama3.2", Endpoint = "http://localhost:11434/v1" });
        Check(profile.EndpointPrivacy == "local" && profile.Streaming == CapabilitySupport.Supported && profile.Vision == CapabilitySupport.Unknown,
            "capability profile reports bounded local routing facts without guessing vision support");
        var nonVisionProfile = ModelCapabilityResolver.Resolve(new AppSettings { ModelProvider = "Custom endpoint", Model = "text-embedding-3-small", Endpoint = "https://example.test/v1" });
        Check(nonVisionProfile.Vision == CapabilitySupport.Unsupported && nonVisionProfile.EndpointPrivacy == "remote",
            "capability profile identifies definitive non-vision model markers");
        var overrideProfile = ModelCapabilityResolver.Resolve(new AppSettings { Model = "text-embedding-3-small", VisionCapabilityOverride = "supported" });
        Check(overrideProfile.Vision == CapabilitySupport.Supported && overrideProfile.Evidence.Contains("override", StringComparison.Ordinal),
            "capability profile honors an explicit local vision override");
        var blockedImageProvider = new OpenAiCompatibleProvider(new HttpClient(new StubHandler((_, _) => throw new Exception("Definitive capability rejection must happen before network access."))));
        try
        {
            await blockedImageProvider.StreamChatAsync(
                new AppSettings { Model = "text-embedding-3-small", Endpoint = "https://example.test/v1" },
                [new ChatMessage("user", "describe", [new ChatAttachment("sample.png", Convert.ToBase64String(pngSignature), "image/png")])],
                _ => { }, CancellationToken.None);
            throw new Exception("Expected a definitive non-vision model to reject image input.");
        }
        catch (InvalidOperationException error)
        {
            Check(error.Message.Contains("non-vision", StringComparison.OrdinalIgnoreCase), "provider blocks image input for a definitive non-vision model");
        }
        Check(ProviderErrorText.Describe(new HttpRequestException("boom", null, HttpStatusCode.InternalServerError)) == "Model server error: boom"
            && ProviderErrorText.Describe(new HttpRequestException("refused")).StartsWith("Could not reach the model server.", StringComparison.Ordinal)
            && ProviderErrorText.Describe(new TaskCanceledException()) == "The request timed out or was interrupted.",
            "provider errors distinguish server errors, unreachable servers, and timeouts");
        Check(ProviderErrorText.Describe(new HttpRequestException("bad", null, HttpStatusCode.BadRequest), hasImageAttachments: true).StartsWith("This model endpoint rejected the image.", StringComparison.Ordinal)
            && ProviderErrorText.Describe(new HttpRequestException("bad", null, HttpStatusCode.BadRequest), hasImageAttachments: false) == "Model server error: bad",
            "a 400 is explained as an image rejection only when images were attached");
        Check(ProviderErrorText.Classify(new InvalidOperationException("The model server closed the stream without returning a response.")) == ProviderFailureKind.ModelCompatibility
            && ProviderErrorText.Classify(new HttpRequestException("refused")) == ProviderFailureKind.EndpointUnavailable
            && ProviderErrorText.Classify(new HttpRequestException("bad", null, HttpStatusCode.BadRequest), hasImageAttachments: true) == ProviderFailureKind.ImageRejected
            && ProviderErrorText.Classify(new TaskCanceledException()) == ProviderFailureKind.Timeout,
            "provider failures classify model compatibility separately from endpoint, image, and timeout failures");
        var selection = ModelSelectionPolicy.Evaluate("missing-model", ["local-a", "local-b"]);
        Check(!selection.IsAvailable && selection.SelectedModel == "missing-model"
            && ModelSelectionPolicy.Evaluate("local-b", ["local-a", "local-b"]).IsAvailable,
            "unavailable selected models remain explicit until the user chooses a listed model");
        Check(ConnectionSettingsValidator.Validate("http://localhost:11434/v1", "local-a", true).IsValid,
            "valid local connection settings pass validation");
        Check(!ConnectionSettingsValidator.Validate("not an endpoint", "local-a", false).IsValid
            && !ConnectionSettingsValidator.Validate("http://localhost:11434/v1", "", false).IsValid,
            "invalid endpoint and missing model are rejected before save");
        Check(!ConnectionSettingsValidator.Validate("https://example.com/v1", "remote-a", true).IsValid,
            "local-only settings reject remote endpoints before save");
        Check(ConnectionSettingsValidator.Validate("http://localhost:11434/v1", "local-a", true).IsValid
            && !ConnectionSettingsValidator.Validate("https://example.com/v1", "remote-a", true).IsValid,
            "local-only route selection rejects a pinned remote endpoint before switching");
        var drafts = new WorkspaceDraftStore();
        drafts.Set("ChatBot", "draft for chat");
        drafts.Set("CodingAgent", "draft for coding");
        Check(drafts.Get("ChatBot") == "draft for chat" && drafts.Get("CodingAgent") == "draft for coding"
            && drafts.Get("missing") == string.Empty,
            "workspace drafts remain isolated by workspace");
    }
}
