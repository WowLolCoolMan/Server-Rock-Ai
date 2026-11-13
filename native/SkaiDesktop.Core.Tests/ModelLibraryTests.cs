// Model Library: local runtime discovery, Hugging Face and ModelScope search,
// GGUF download/import checks, Ollama pull/delete (ModelLibraryService).
// Uses TestSupport (Check, StubHandler, …). Run by Program.cs.

using System.Net;
using System.Text;
using System.Text.Json;
using SkaiDesktop.Core.Infrastructure;
using SkaiDesktop.Core.Models;
using SkaiDesktop.Core.Services;
using static TestSupport;

internal static class ModelLibraryTests
{
    public static async Task RunAsync()
    {
        HttpRequestMessage? deleteRequest = null;
        var deleteService = new ModelLibraryService(new HttpClient(new StubHandler((request, _) =>
        {
            deleteRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        })));
        await deleteService.DeleteOllamaAsync("http://127.0.0.1:11434", "local-model");
        Check(deleteRequest?.Method == HttpMethod.Delete && deleteRequest.RequestUri?.AbsolutePath == "/api/delete",
            "Ollama model deletion uses the bounded local delete endpoint");

        var hfSearch = new ModelLibraryService(new HttpClient(new StubHandler((request, _) =>
        {
            var requestUri = request.RequestUri;
            Check(requestUri?.Host == "huggingface.co" && requestUri.Query.Contains("filter=gguf", StringComparison.Ordinal), "model library searches Hugging Face GGUF repositories");
            var data = requestUri?.Query.Contains("abliterated", StringComparison.OrdinalIgnoreCase) == true
                ? "[{\"id\":\"org/model-abliterated-gguf\",\"downloads\":1234,\"likes\":12,\"tags\":[\"gguf\",\"abliterated\"]}]"
                : "[{\"id\":\"org/model-gguf\",\"downloads\":1234,\"likes\":12,\"tags\":[\"gguf\"]},{\"id\":\"org/model-uncensored-gguf\",\"downloads\":9,\"tags\":[\"uncensored\"]}]";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(data) });
        })));
        var hubModels = await hfSearch.SearchHuggingFaceAsync("small instruct", uncensored: false);
        Check(hubModels.Count == 1 && hubModels[0].Id == "org/model-gguf" && hubModels[0].Downloads == 1234, "model library parses Hub IDs and popularity metadata");
        var uncensoredHubModels = await hfSearch.SearchHuggingFaceAsync("small instruct", uncensored: true);
        Check(uncensoredHubModels.Count >= 1 && uncensoredHubModels.All(model => model.IsUncensored), "uncensored catalog includes only explicitly labeled repositories");
        var boundedCatalog = new ModelLibraryService(new HttpClient(new StubHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[{\"id\":\"" + new string('x', 513) + "\"}]") }))));
        try
        {
            await boundedCatalog.SearchHuggingFaceAsync(new string('q', 257), uncensored: false);
            throw new Exception("Expected oversized model search text to be rejected.");
        }
        catch (InvalidOperationException)
        {
            Check(true, "model catalog search text is bounded before network access");
        }
        var ignoredCatalogModels = await boundedCatalog.SearchHuggingFaceAsync("small", uncensored: false);
        Check(ignoredCatalogModels.Count == 0, "model catalog ignores oversized remote model IDs");
        var localInventory = new ModelLibraryService(new HttpClient(new StubHandler((request, _) =>
        {
            var models = (request.RequestUri?.Port, request.RequestUri?.AbsolutePath) switch
            {
                (11434, "/api/tags") => "{\"models\":[{\"name\":\"llama3.2\",\"size\":123456789,\"details\":{\"family\":\"llama\"}},{\"name\":\"qwen3:8b\"}]}",
                (1234, "/api/v1/models") => "{\"models\":[{\"type\":\"llm\",\"key\":\"qwen3:8b\",\"display_name\":\"Qwen 3 8B\",\"size_bytes\":1234567890,\"loaded_instances\":[]}]}",
                _ => "{\"data\":[]}"
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(models) });
        })));
        var discoveredLocalModels = await localInventory.DiscoverLocalModelsAsync("http://localhost:1234/v1");
        Check(discoveredLocalModels.Count == 3, "model library finds installed models across multiple local runtimes");
        Check(discoveredLocalModels.Count(model => model.Model == "qwen3:8b") == 2 && discoveredLocalModels.Select(model => model.Endpoint).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 2, "same model ID remains distinguishable when installed in separate runtimes");
        Check(discoveredLocalModels.Any(model => model.Provider == "LM Studio" && model.IsLoaded == false && model.DisplayName == "Qwen 3 8B") && discoveredLocalModels.Any(model => model.Provider == "Ollama" && model.SizeBytes == 123456789), "runtime-native inventories include installed state and model metadata");
        var resilientInventory = new ModelLibraryService(new HttpClient(new StubHandler((request, _) =>
        {
            if (request.RequestUri?.Port == 1234 && request.RequestUri.AbsolutePath == "/api/v1/models")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"models\":[{\"type\":\"llm\",\"key\":\"healthy-local-model\"}]}", Encoding.UTF8, "application/json")
                });
            }
            var oversized = new StringContent("{}");
            oversized.Headers.ContentLength = BoundedHttpContent.MaxJsonBytes + 1L;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = oversized });
        })));
        var resilientModels = await resilientInventory.DiscoverLocalModelsAsync("http://localhost:1234/v1");
        Check(resilientModels.Count == 1 && resilientModels[0].Model == "healthy-local-model" && resilientModels[0].Provider == "LM Studio",
            "one malformed local runtime response does not block AI SuperMarket inventory from healthy runtimes");

        var ollamaProgress = new List<string>();
        var ollamaPull = new ModelLibraryService(new HttpClient(new StubHandler((request, _) =>
        {
            Check(request.RequestUri?.AbsoluteUri == "http://localhost:11434/api/pull", "Ollama library pulls through the local native API");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"status\":\"pulling\",\"completed\":524288,\"total\":1048576}\n{\"status\":\"success\"}\n") });
        })));
        await ollamaPull.PullOllamaAsync("http://localhost:11434/v1", "qwen3:8b", ollamaProgress.Add);
        Check(ollamaProgress.Count == 2 && ollamaProgress[0].Contains("0.5 / 1.0 MB", StringComparison.Ordinal), "Ollama model pull reports streaming byte progress");
        var invalidModelLibrary = new ModelLibraryService(new HttpClient(new StubHandler((_, _) => throw new Exception("Invalid model identifiers must be rejected before network access."))));
        try
        {
            await invalidModelLibrary.PullOllamaAsync("http://localhost:11434/v1", new string('x', 513));
            throw new Exception("Expected an oversized Ollama model identifier to be rejected.");
        }
        catch (InvalidOperationException)
        {
            Check(true, "Ollama model identifiers are bounded before network access");
        }
        try
        {
            await invalidModelLibrary.DownloadFromHuggingFaceAsync("http://localhost:1234", "not-a-repository");
            throw new Exception("Expected an invalid Hugging Face repository identifier to be rejected.");
        }
        catch (InvalidOperationException)
        {
            Check(true, "Hugging Face repository identifiers are validated before network access");
        }

        var modelScopeSearch = new ModelLibraryService(new HttpClient(new StubHandler((request, _) =>
        {
            var uri = request.RequestUri!;
            if (uri.AbsolutePath == "/openapi/v1/models")
            {
                var uncensored = uri.Query.Contains("abliterated", StringComparison.OrdinalIgnoreCase) || uri.Query.Contains("uncensored", StringComparison.OrdinalIgnoreCase);
                var results = uncensored
                    ? "{\"data\":{\"models\":[{\"id\":\"org/model-abliterated\",\"downloads\":12,\"tasks\":[\"text-generation\"]}]}}"
                    : "{\"data\":{\"models\":[{\"id\":\"org/model-gguf\",\"downloads\":120,\"tasks\":[\"text-generation\"]},{\"id\":\"org/model-embedding\",\"downloads\":300,\"tasks\":[\"sentence-embedding\"]},{\"id\":\"org/model-reranker\",\"downloads\":90,\"tasks\":[\"text-generation\"]},{\"id\":\"org/Qwen3Guard-Gen-0.6B\",\"downloads\":85,\"tasks\":[\"text-generation\"]},{\"id\":\"org/qwen3-asr\",\"downloads\":80,\"tasks\":[]}]}}";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(results) });
            }
            var pathParts = uri.AbsolutePath.Split('/');
            var repository = pathParts.Length > 5 ? pathParts[4] + "/" + pathParts[5] : "";
            var hash = new string('a', 64);
            var files = repository.Contains("abliterated", StringComparison.OrdinalIgnoreCase)
                ? $"{{\"Data\":{{\"Files\":[{{\"Path\":\"model-abliterated-Q4_K_M.gguf\",\"Type\":\"blob\",\"Size\":8,\"Sha256\":\"{hash}\"}}]}}}}"
                : $"{{\"Data\":{{\"Files\":[{{\"Path\":\"model-Q4_K_M-00001-of-00002.gguf\",\"Type\":\"blob\",\"Size\":8,\"Sha256\":\"{hash}\"}},{{\"Path\":\"model-Q4_K_M-00002-of-00002.gguf\",\"Type\":\"blob\",\"Size\":8,\"Sha256\":\"{hash}\"}},{{\"Path\":\"mmproj-model-Q4_K_M.gguf\",\"Type\":\"blob\",\"Size\":8,\"Sha256\":\"{hash}\"}},{{\"Path\":\"README.md\",\"Type\":\"blob\",\"Size\":10,\"Sha256\":\"{hash}\"}}]}}}}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(files) });
        })));
        var modelScopeRegular = await modelScopeSearch.SearchModelScopeGgufAsync("small instruct", uncensored: false);
        Check(modelScopeRegular.Count == 1 && modelScopeRegular[0].Repository == "org/model-gguf" && modelScopeRegular[0].Files.Count == 2 && modelScopeRegular[0].SizeBytes == 16, "ModelScope catalog groups chat GGUF shards and excludes embedding, reranker, guard, ASR, and projector files even when task tags conflict");
        var modelScopeUncensored = await modelScopeSearch.SearchModelScopeGgufAsync("small instruct", uncensored: true);
        Check(modelScopeUncensored.Count == 1 && modelScopeUncensored[0].IsUncensored && modelScopeUncensored[0].Repository.Contains("abliterated", StringComparison.OrdinalIgnoreCase), "ModelScope uncensored search includes only explicitly labeled repositories");

        var ggufBytes = "GGUFtest"u8.ToArray();
        var ggufHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(ggufBytes)).ToLowerInvariant();
        var importedFileDigest = "";
        var importedCreateBody = "";
        var modelScopeImport = new ModelLibraryService(new HttpClient(new StubHandler(async (request, _) =>
        {
            if (request.Method == HttpMethod.Get)
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(ggufBytes) };
            if (request.RequestUri!.AbsolutePath.StartsWith("/api/blobs/", StringComparison.Ordinal))
            {
                importedFileDigest = request.RequestUri.AbsolutePath.Split('/').Last();
                return new HttpResponseMessage(HttpStatusCode.Created);
            }
            if (request.RequestUri!.AbsolutePath == "/api/create")
            {
                importedCreateBody = await request.Content!.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"status\":\"parsing GGUF\"}\n{\"status\":\"success\"}\n") };
            }
            throw new Exception("Unexpected ModelScope import request: " + request.RequestUri);
        })));
        var importedModelName = await modelScopeImport.DownloadAndImportModelScopeGgufAsync("http://localhost:11434/v1",
            new ModelScopeGgufPackage("org/model", "model-Q4_K_M.gguf", [new ModelScopeGgufFile("model-Q4_K_M.gguf", ggufBytes.Length, ggufHash)], 1));
        using (var createJson = JsonDocument.Parse(importedCreateBody))
            Check(importedFileDigest == "sha256:" + ggufHash && createJson.RootElement.GetProperty("files").GetProperty("model-Q4_K_M.gguf").GetString() == importedFileDigest && importedModelName.StartsWith("ms-model-", StringComparison.Ordinal), "ModelScope GGUF downloads are SHA-256 checked and imported through Ollama's blob/create APIs");
        var rejectedImportHandler = new StubHandler((request, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(ggufBytes) }));
        var rejectedImport = new ModelLibraryService(new HttpClient(rejectedImportHandler));
        try
        {
            await rejectedImport.DownloadAndImportModelScopeGgufAsync("http://localhost:11434/v1",
                new ModelScopeGgufPackage("org/model", "model-Q4_K_M.gguf", [new ModelScopeGgufFile("model-Q4_K_M.gguf", ggufBytes.Length, new string('0', 64))], 1));
            throw new Exception("Expected a mismatched ModelScope file checksum to be rejected.");
        }
        catch (InvalidDataException) { Check(true, "ModelScope GGUF import rejects mismatched SHA-256 before Ollama upload"); }
        var invalidGgufBytes = "NOPEtest"u8.ToArray();
        var invalidGgufHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(invalidGgufBytes)).ToLowerInvariant();
        var invalidGgufImport = new ModelLibraryService(new HttpClient(new StubHandler((request, _) =>
        {
            if (request.Method == HttpMethod.Post) throw new Exception("Invalid GGUF data must not be uploaded to Ollama.");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(invalidGgufBytes) });
        })));
        try
        {
            await invalidGgufImport.DownloadAndImportModelScopeGgufAsync("http://localhost:11434/v1",
                new ModelScopeGgufPackage("org/model", "model-Q4_K_M.gguf", [new ModelScopeGgufFile("model-Q4_K_M.gguf", invalidGgufBytes.Length, invalidGgufHash)], 1));
            throw new Exception("Expected a non-GGUF file signature to be rejected.");
        }
        catch (InvalidDataException) { Check(true, "ModelScope GGUF import rejects invalid file signatures before Ollama upload"); }
        var traversalImport = new ModelLibraryService(new HttpClient(new StubHandler((_, _) => throw new Exception("Unsafe local paths must be rejected before network access."))));
        try
        {
            await traversalImport.DownloadAndImportModelScopeGgufAsync("http://localhost:11434/v1",
                new ModelScopeGgufPackage("org/model", "../model.gguf", [new ModelScopeGgufFile("../model.gguf", 1, new string('a', 64))], 1));
            throw new Exception("Expected unsafe ModelScope archive paths to be rejected.");
        }
        catch (InvalidOperationException) { Check(true, "ModelScope GGUF import rejects path traversal before network access"); }

        // Local speed averaging (moved from MainPage into Core).
        Check(LocalModelPerformance.Key(" HTTP://LocalHost:1234/v1/ ", "Qwen3") == "http://localhost:1234/v1\nQwen3",
            "local speed key normalizes the endpoint but keeps the model ID");
        var measuredAt = DateTimeOffset.Parse("2026-11-07T00:00:00Z");
        var firstSample = LocalModelPerformance.AddSample(null, 400, 1100, 100, measuredAt);
        Check(firstSample.Samples == 1 && Math.Abs(firstSample.EstimatedTokensPerSecond - 100) < 0.001 && firstSample.TimeToFirstTokenMilliseconds == 100,
            "first local speed sample estimates characters/4 per second after the first token");
        var secondSample = LocalModelPerformance.AddSample(firstSample, 200, 1300, 300, measuredAt);
        Check(secondSample.Samples == 2 && Math.Abs(secondSample.EstimatedTokensPerSecond - 75) < 0.001 && secondSample.TimeToFirstTokenMilliseconds == 200,
            "later local speed samples update the running averages");
    }
}
