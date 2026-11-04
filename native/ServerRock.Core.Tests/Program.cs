using System.Net;
using System.Text;
using System.Text.Json;
using ServerRock.Core.Infrastructure;
using ServerRock.Core.Models;
using ServerRock.Core.Services;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

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
try
{
    var remoteProvider = new OpenAiCompatibleProvider(new HttpClient());
    await remoteProvider.DiscoverModelsAsync("http://example.com/v1");
    throw new Exception("Expected remote HTTP model endpoint to be rejected.");
}
catch (InvalidOperationException)
{
    Check(true, "native providers reject remote plain HTTP endpoints");
}
var attachmentProbePath = Path.Combine(Path.GetTempPath(), "server-rock-attachment-" + Guid.NewGuid().ToString("N") + ".png");
try
{
    File.WriteAllBytes(attachmentProbePath, new byte[ChatAttachmentPolicy.MaxFileBytes + 1]);
    try
    {
        _ = await ChatAttachmentPolicy.ReadBytesAsync(attachmentProbePath, ChatAttachmentPolicy.MaxFileBytes);
        throw new Exception("Expected oversized attachment to be rejected.");
    }
    catch (InvalidDataException)
    {
        Check(true, "native attachment reads enforce byte limits");
    }
}
finally { if (File.Exists(attachmentProbePath)) File.Delete(attachmentProbePath); }
var oversizedNativeJson = new StringContent("{}");
oversizedNativeJson.Headers.ContentLength = BoundedHttpContent.MaxJsonBytes + 1L;
try
{
    _ = await BoundedHttpContent.ReadJsonAsync(oversizedNativeJson, label: "Native test response");
    throw new Exception("Expected oversized native HTTP JSON to be rejected.");
}
catch (InvalidDataException)
{
    Check(true, "native HTTP JSON bodies are bounded before parsing");
}
using (var lineStream = new MemoryStream(Encoding.UTF8.GetBytes("first\nsecond\n")))
using (var lineSource = new StreamReader(lineStream))
{
    var lineReader = BoundedHttpContent.CreateLineReader(lineSource);
    Check(await lineReader.ReadLineAsync() == "first" && await lineReader.ReadLineAsync() == "second" && await lineReader.ReadLineAsync() is null,
        "native stream line reader preserves adjacent lines");
}
try
{
    using var oversizedLineStream = new MemoryStream(Encoding.UTF8.GetBytes(new string('x', BoundedHttpContent.MaxStreamLineCharacters + 1)));
    using var oversizedLineSource = new StreamReader(oversizedLineStream);
    var oversizedLineReader = BoundedHttpContent.CreateLineReader(oversizedLineSource);
    _ = await oversizedLineReader.ReadLineAsync();
    throw new Exception("Expected an oversized native stream line to be rejected.");
}
catch (InvalidDataException)
{
    Check(true, "native stream line reader rejects oversized lines before JSON parsing");
}
var oversizedStorePath = Path.Combine(Path.GetTempPath(), "server-rock-oversized-store-" + Guid.NewGuid().ToString("N") + ".json");
try
{
    File.WriteAllText(oversizedStorePath, new string('x', BoundedFileReader.MaxJsonBytes + 1));
    try
    {
        _ = BoundedFileReader.ReadText(oversizedStorePath);
        throw new Exception("Expected oversized native persistence to be rejected.");
    }
    catch (InvalidDataException)
    {
        Check(true, "native persistence files are bounded before deserialization");
    }
}
finally { if (File.Exists(oversizedStorePath)) File.Delete(oversizedStorePath); }
var invalidUtf8Path = Path.Combine(Path.GetTempPath(), "server-rock-invalid-utf8-" + Guid.NewGuid().ToString("N") + ".json");
try
{
    File.WriteAllBytes(invalidUtf8Path, [0xC3, 0x28]);
    _ = BoundedFileReader.ReadText(invalidUtf8Path);
    throw new Exception("Expected invalid UTF-8 persistence to be rejected.");
}
catch (InvalidDataException)
{
    Check(true, "native persistence rejects invalid UTF-8 before deserialization");
}
finally { if (File.Exists(invalidUtf8Path)) File.Delete(invalidUtf8Path); }

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
var portableChat = new ChatSession
{
    Title = "Portable conversation",
    Mode = "Coding Agent",
    ProjectName = "Sample project",
    ProjectInstructions = "Keep changes focused.",
    Messages = [new ChatMessage("user", "Review this file", [new ChatAttachment("notes.md", "A local note")]), new ChatMessage("assistant", "Here is a review.")]
};
var portableArchive = ConversationArchive.Parse(ConversationArchive.Serialize([portableChat]));
Check(portableArchive.Count == 1 && portableArchive[0].Mode == "Coding Agent" && portableArchive[0].ProjectInstructions == "Keep changes focused." && portableArchive[0].Messages[0].Attachments?[0].Content == "A local note", "conversation archive round-trips local project context and attachments");
try
{
    _ = ConversationArchive.Serialize(Enumerable.Repeat(new ChatSession(), ConversationArchive.MaxConversations + 1));
    throw new Exception("Expected an archive with too many conversations to be rejected.");
}
catch (InvalidDataException)
{
    Check(true, "conversation archive count limits apply before materializing unbounded input");
}
var unsafeArchive = ConversationArchive.Serialize([new ChatSession { Messages = [new ChatMessage("system", "Injected instruction")] }]);
try
{
    ConversationArchive.Parse(unsafeArchive);
    throw new Exception("Expected an unsupported imported role to be rejected.");
}
catch (InvalidDataException) { Check(true, "conversation archive rejects untrusted system and tool roles"); }
var secretAttachmentArchive = ConversationArchive.Serialize([new ChatSession { Messages = [new ChatMessage("user", "Attached", [new ChatAttachment(".env", "SECRET=value")])] }]);
try
{
    ConversationArchive.Parse(secretAttachmentArchive);
    throw new Exception("Expected an imported sensitive filename to be rejected.");
}
catch (InvalidDataException) { Check(true, "conversation archive rejects secret-like attachment names"); }
try
{
    ConversationArchive.Parse("not json");
    throw new Exception("Expected malformed archive data to be rejected.");
}
catch (InvalidDataException) { Check(true, "conversation archive reports malformed data without touching saved history"); }
try
{
    ConversationArchive.Parse("{\"messages\":[{\"role\":\"user\",\"content\":\"" + new string('\u4E00', 8_500_000) + "\"}]}");
    throw new Exception("Expected a UTF-8 oversized archive to be rejected.");
}
catch (InvalidDataException)
{
    Check(true, "conversation archives enforce a UTF-8 byte limit before parsing");
}
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

var searchProvider = new WebSearchProvider(new HttpClient(new StubHandler((request, _) =>
{
    Check(request.RequestUri?.Host == "html.duckduckgo.com", "opt-in web search uses the configured public search endpoint");
    var html = "<a class=\"result__a\" href=\"//duckduckgo.com/l/?uddg=https%3A%2F%2Fexample.org%2Fdocs&amp;rut=abc\">Local &amp; private</a><a class=\"result__snippet\">Read <b>official</b> docs.</a>";
    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(html) });
})));
var searchResults = await searchProvider.SearchAsync("Uno Linux");
Check(searchResults.Count == 1 && searchResults[0].Title == "Local & private", "web search decodes and extracts result titles");
Check(searchResults[0].Url == "https://example.org/docs" && searchResults[0].Snippet == "Read official docs.", "web search returns safe HTTP source URLs and clean snippets");

var githubNow = DateTimeOffset.Parse("2026-11-03T12:00:00Z");
var githubResponses = new Queue<string>([
    "{\"device_code\":\"device-code-long-value-0123456789\",\"user_code\":\"ABCD-EFGH\",\"verification_uri\":\"https://github.com/login/device\",\"expires_in\":900,\"interval\":5}",
    "{\"error\":\"authorization_pending\"}",
    "{\"error\":\"slow_down\"}",
    "{\"access_token\":\"gho_12345678901234567890\",\"refresh_token\":\"ghr_12345678901234567890\",\"expires_in\":28800,\"refresh_token_expires_in\":15897600}"
]);
var githubRequestCount = 0;
var githubFlow = new GitHubDeviceFlow(new HttpClient(new StubHandler((request, _) =>
{
    githubRequestCount++;
    Check(request.Headers.Accept.Any(item => item.MediaType == "application/json"), "GitHub device flow requests JSON responses");
    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(githubResponses.Dequeue(), Encoding.UTF8, "application/json") });
})), () => githubNow);
var githubAuthorization = await githubFlow.BeginAsync("Iv1.example-client-id");
Check(githubAuthorization.UserCode == "ABCD-EFGH" && githubAuthorization.VerificationUri.AbsoluteUri == "https://github.com/login/device", "GitHub device flow returns a verified browser address and user code");
Check(githubAuthorization.IntervalSeconds == 5 && githubAuthorization.ExpiresAt == githubNow.AddMinutes(15), "GitHub device flow honors expiry and initial polling interval");
var earlyPoll = await githubFlow.PollAsync(githubAuthorization);
Check(earlyPoll.IsPending && githubRequestCount == 1, "GitHub device flow never polls before its required interval");
githubNow = githubAuthorization.NextPollAt;
var pendingPoll = await githubFlow.PollAsync(githubAuthorization);
Check(pendingPoll.IsPending && githubAuthorization.NextPollAt == githubNow.AddSeconds(5), "GitHub authorization_pending schedules the next poll at the supplied interval");
githubNow = githubAuthorization.NextPollAt;
var slowedPoll = await githubFlow.PollAsync(githubAuthorization);
Check(slowedPoll.IsPending && githubAuthorization.IntervalSeconds == 10, "GitHub slow_down increases the polling interval by five seconds");
githubNow = githubAuthorization.NextPollAt;
var completedPoll = await githubFlow.PollAsync(githubAuthorization);
Check(!completedPoll.IsPending && completedPoll.Credential?.AccessToken == "gho_12345678901234567890" && completedPoll.Credential.RefreshToken == "ghr_12345678901234567890", "GitHub device flow returns access and refresh credentials only after authorization");
Check(completedPoll.Credential?.AccessTokenExpiresAt == githubNow.AddHours(8) && completedPoll.Credential.RefreshTokenExpiresAt == githubNow.AddDays(184), "GitHub token expiries are retained from the authorization response");
var refreshBody = "";
var refreshedFlow = new GitHubDeviceFlow(new HttpClient(new StubHandler(async (request, _) =>
{
    refreshBody = await request.Content!.ReadAsStringAsync();
    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"access_token\":\"gho_refreshed-access-token-12345\",\"refresh_token\":\"ghr_refreshed-refresh-token-12345\",\"expires_in\":28800,\"refresh_token_expires_in\":15897600}", Encoding.UTF8, "application/json") };
})), () => githubNow);
var refreshedCredential = await refreshedFlow.RefreshAsync("Iv1.example-client-id", completedPoll.Credential!);
Check(refreshBody.Contains("grant_type=refresh_token", StringComparison.Ordinal) && refreshBody.Contains("ghr_12345678901234567890", StringComparison.Ordinal), "GitHub refresh requests use the required refresh_token grant");
Check(refreshedCredential.AccessToken == "gho_refreshed-access-token-12345" && refreshedCredential.RefreshToken == "ghr_refreshed-refresh-token-12345" && refreshedCredential.AccessTokenExpiresAt == githubNow.AddHours(8), "GitHub refresh rotates and persists expiring credentials");
try
{
    await refreshedFlow.RefreshAsync("bad/client/id", completedPoll.Credential!);
    throw new Exception("Expected malformed GitHub refresh client IDs to be rejected.");
}
catch (InvalidOperationException)
{
    Check(true, "GitHub refresh validates the client ID before network access");
}
try
{
    await githubFlow.BeginAsync("bad/client/id");
    throw new Exception("Expected an invalid GitHub client ID to be rejected.");
}
catch (ArgumentException)
{
    Check(githubRequestCount == 4, "GitHub client ID validation stops malformed IDs before any network request");
}
if (OperatingSystem.IsWindows())
{
    var credentialPath = Path.Combine(Path.GetTempPath(), "server-rock-github-credential-" + Guid.NewGuid().ToString("N"), "github-token.bin");
    var credentialStore = new GitHubCredentialStore(credentialPath);
    await credentialStore.WriteAsync("gho_test-access-token-value");
    Check(File.Exists(credentialPath) && !File.ReadAllText(credentialPath).Contains("gho_test-access-token-value", StringComparison.Ordinal), "Windows GitHub credentials are stored as DPAPI ciphertext");
    Check(await credentialStore.ReadAsync() == "gho_test-access-token-value", "Windows GitHub credentials decrypt for the same user");
    await credentialStore.DeleteAsync();
    Check(!File.Exists(credentialPath), "Windows GitHub credential disconnect removes the protected file");
    Directory.Delete(Path.GetDirectoryName(credentialPath)!, recursive: true);
}
Check(GitHubApiClient.ParseRepositoryReference("owner/project") == "owner/project", "GitHub repository input accepts owner/name references");
Check(GitHubApiClient.ParseRepositoryReference("https://github.com/owner/project.git") == "owner/project", "GitHub repository input safely normalizes HTTPS clone URLs");
var parsedGitStatus = GitHubWorkspaceService.ParseStatus("# branch.oid 0123456789abcdef\n# branch.head main\n# branch.upstream origin/main\n# branch.ab +2 -1\n1 .M N... 100644 100644 100644 00000000 11111111 path with spaces.txt\n? new file.md\n", "owner/project");
Check(parsedGitStatus.Branch == "main" && parsedGitStatus.CommitsAhead == 2 && parsedGitStatus.CommitsBehind == 1, "GitHub workspace parses branch status and ahead/behind counts");
Check(parsedGitStatus.Changes.Count == 2 && parsedGitStatus.Changes[0].Path == "path with spaces.txt" && parsedGitStatus.Changes[1].Status == "??", "GitHub workspace preserves modified and untracked paths for review");
foreach (var unsafeReference in new[] { "https://evil.example/owner/project", "https://user:pass@github.com/owner/project", "owner/../settings", "owner/project?tab=code" })
{
    try
    {
        GitHubApiClient.ParseRepositoryReference(unsafeReference);
        throw new Exception("Expected unsafe GitHub repository reference to be rejected.");
    }
    catch (ArgumentException) { }
}
Check(true, "GitHub repository input rejects alternate hosts, credentials, traversal, and URL query data");
var unsafeGitPaths = GitHubWorkspaceService.FindUnsafeChangePaths([
    new GitWorkspaceFileChange("??", ".env.production"),
    new GitWorkspaceFileChange("??", "config/credentials.json"),
    new GitWorkspaceFileChange("??", "certs/client.p12"),
    new GitWorkspaceFileChange("??", ".env.example"),
    new GitWorkspaceFileChange("??", "src/app.cs")
]);
Check(unsafeGitPaths.Count == 3 && unsafeGitPaths.Contains(".env.production") && unsafeGitPaths.Contains("config/credentials.json") && unsafeGitPaths.Contains("certs/client.p12"), "GitHub commit safety blocks secret-like paths");
const string repositoryJson = "{\"id\":42,\"full_name\":\"owner/project\",\"name\":\"project\",\"private\":true,\"default_branch\":\"main\",\"description\":\"Demo\",\"updated_at\":\"2026-11-03T12:00:00Z\",\"clone_url\":\"https://github.com/owner/project.git\"}";
var githubApi = new GitHubApiClient(new HttpClient(new StubHandler((request, _) =>
{
    Check(request.RequestUri?.Host == "api.github.com", "GitHub API calls stay on the official API host");
    Check(request.Headers.Authorization?.Scheme == "Bearer" && request.Headers.Authorization.Parameter == "test-access-token", "GitHub API calls use the signed-in user's bearer token");
    Check(request.Headers.Contains("X-GitHub-Api-Version"), "GitHub API calls declare a stable API version");
    var responseBody = request.RequestUri?.AbsolutePath == "/user/repos"
        ? "[" + repositoryJson + "]"
        : repositoryJson;
    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(responseBody, Encoding.UTF8, "application/json") });
})));
var resolvedRepository = await githubApi.ResolveRepositoryAsync("test-access-token", "owner/project");
Check(resolvedRepository.FullName == "owner/project" && resolvedRepository.IsPrivate && resolvedRepository.DefaultBranch == "main", "GitHub API resolves repository identity and clone metadata");
var listedRepositories = await githubApi.ListRepositoriesAsync("test-access-token");
Check(listedRepositories.Count == 1 && listedRepositories[0].Id == 42 && listedRepositories[0].CloneUrl.AbsoluteUri == "https://github.com/owner/project.git", "GitHub API lists authorized repositories with safe HTTPS clone URLs");
var accountApi = new GitHubApiClient(new HttpClient(new StubHandler((_, _) =>
    Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent("{\"login\":\"octocat\",\"name\":\"Octo\",\"avatar_url\":\"https://github.com/" + new string('a', 2050) + "\"}", Encoding.UTF8, "application/json")
    }))));
var account = await accountApi.GetAccountAsync("test-access-token");
Check(account.AvatarUrl is null, "GitHub API drops oversized avatar URLs at the trust boundary");
var oversizedBranchApi = new GitHubApiClient(new HttpClient(new StubHandler((_, _) =>
    Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent(repositoryJson.Replace("\"main\"", "\"" + new string('b', 257) + "\"", StringComparison.Ordinal), Encoding.UTF8, "application/json")
    }))));
try
{
    await oversizedBranchApi.ResolveRepositoryAsync("test-access-token", "owner/project");
    throw new Exception("Expected an oversized GitHub branch name to be rejected.");
}
catch (InvalidOperationException)
{
    Check(true, "GitHub API bounds repository branch identifiers");
}
Check(ChatAttachmentPolicy.IsAllowedName("notes.md") && ChatAttachmentPolicy.IsAllowedName("src/Program.cs"), "chat attachment policy allows supported text files");
Check(ChatAttachmentPolicy.IsAllowedPdfName("brief.pdf") && ChatAttachmentPolicy.IsAllowedName("brief.pdf") && !ChatAttachmentPolicy.IsAllowedPdfName(".env.pdf"), "chat attachment policy allows PDFs but blocks secret-like names");
Check(ChatAttachmentPolicy.IsAllowedName("photo.png") && ChatAttachmentPolicy.GetImageMediaType("photo.jpg") == "image/jpeg" && ChatAttachmentPolicy.GetImageMediaType("vector.svg") is null, "chat attachment policy accepts PNG and JPEG images and rejects active SVG images");
Check(!ChatAttachmentPolicy.IsAllowedName(".env.png"), "chat attachment policy blocks secret-named files even with an image extension");
Check(!ChatAttachmentPolicy.IsAllowedName(".env") && !ChatAttachmentPolicy.IsAllowedName("id_rsa") && !ChatAttachmentPolicy.IsAllowedName("archive.zip"), "chat attachment policy blocks secrets and unsupported binary files");
Check(ChatAttachmentPolicy.BoundContent(new string('x', ChatAttachmentPolicy.MaxCharactersPerFile + 50)).Length == ChatAttachmentPolicy.MaxCharactersPerFile, "chat attachments are bounded per file");
var pdfBuilder = new PdfDocumentBuilder();
var pdfPage = pdfBuilder.AddPage(PageSize.A4);
var pdfFont = pdfBuilder.AddStandard14Font(Standard14Font.Helvetica);
var pdfExpectedText = string.Concat(Enumerable.Repeat("Local PDF text stays on this device. ", 12));
pdfPage.AddText(pdfExpectedText, 12, new PdfPoint(25, 700), pdfFont);
var generatedPdf = pdfBuilder.Build();
var extractedPdfText = PdfAttachmentTextExtractor.Extract(generatedPdf, 1200);
Check(PdfAttachmentTextExtractor.HasPdfSignature(generatedPdf) && extractedPdfText.Contains("Local PDF text stays on this device.", StringComparison.Ordinal), "PDF attachment extractor reads selectable text locally");
var truncatedPdfText = PdfAttachmentTextExtractor.Extract(generatedPdf, 96);
Check(truncatedPdfText.Length <= 96 && truncatedPdfText.Contains("omitted", StringComparison.Ordinal), "PDF extraction stays within its requested text bound and marks truncation");
try
{
    PdfAttachmentTextExtractor.Extract(Encoding.ASCII.GetBytes("not a PDF"), 100);
    throw new Exception("Expected invalid PDF bytes to be rejected.");
}
catch (InvalidDataException) { Check(true, "PDF attachment extractor rejects invalid PDF signatures"); }
Check(ChatAttachmentPolicy.HasValidImageSignature("image/png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]) && ChatAttachmentPolicy.HasValidImageSignature("image/jpeg", [0xFF, 0xD8, 0xFF, 0xD9]) && !ChatAttachmentPolicy.HasValidImageSignature("image/png", [0, 1, 2]), "chat attachment policy checks PNG/JPEG file signatures");
Check(ChatAttachmentPolicy.RemainingImageBytes([new ChatAttachment("photo.png", Convert.ToBase64String([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]), "image/png")]) == ChatAttachmentPolicy.MaxTotalImageBytes - 8, "chat attachment policy budgets decoded image bytes");
var editedTurn = ConversationTurnEditor.ReplaceUserTurn(
    [new ChatMessage("user", "Original question"), new ChatMessage("assistant", "Old answer"), new ChatMessage("user", "Later question"), new ChatMessage("assistant", "Later answer")],
    0,
    "Revised question",
    [new ChatAttachment("notes.md", "context")]);
Check(editedTurn.Count == 1 && editedTurn[0].Content == "Revised question", "editing an earlier turn drops only that turn and its later branch");
Check(editedTurn[0].Attachments is [{ FileName: "notes.md", Content: "context" }], "editing a turn keeps its selected attachment context");
var laterTurnEdit = ConversationTurnEditor.ReplaceUserTurn(
    [new ChatMessage("user", "First"), new ChatMessage("assistant", "First answer"), new ChatMessage("user", "Second"), new ChatMessage("assistant", "Second answer")],
    2,
    "Revised second");
Check(laterTurnEdit.Count == 3 && laterTurnEdit[0].Content == "First" && laterTurnEdit[^1].Content == "Revised second", "editing a later turn preserves earlier conversation context");
try
{
    ConversationTurnEditor.ReplaceUserTurn([new ChatMessage("assistant", "Not editable")], 0, "replacement");
    throw new Exception("Expected assistant messages to be rejected by the turn editor.");
}
catch (ArgumentException)
{
    Check(true, "turn editor rejects attempts to edit assistant messages");
}
var localOnlyMessage = new ChatMessage("assistant", "With references", Sources: [new ChatSource("Docs", "https://example.org", "A snippet")]);
Check(localOnlyMessage.ForModel(localOnlyMessage.Content).Sources is null, "web source metadata stays out of model requests");
var pngSignature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
var modelImageMessage = new ChatMessage("user", "Describe this", [new ChatAttachment("sample.png", Convert.ToBase64String(pngSignature), "image/png")]).ForModel("Describe this");
Check(modelImageMessage.Attachments is [{ IsImage: true }], "model messages retain image attachments while stripping text attachment metadata");

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

var settingsDirectory = Path.Combine(Path.GetTempPath(), "server-rock-core-check-" + Guid.NewGuid().ToString("N"));
try
{
    var settingsStore = new JsonSettingsStore(Path.Combine(settingsDirectory, "settings.json"));
    settingsStore.Save(new AppSettings { Endpoint = "http://127.0.0.1:1234/v1", Model = "persisted-model", ModelProvider = "LM Studio", PinnedModels = ["persisted-model", "second-model"], PinnedModelConnections = [new("LM Studio", "persisted-model", "http://127.0.0.1:1234/v1"), new("Ollama", "llama3.2", "http://127.0.0.1:11434/v1")], LocalModelPerformance = new() { ["http://127.0.0.1:1234/v1\npersisted-model"] = new(3, 28.5, 410, DateTimeOffset.Parse("2026-11-03T12:00:00Z")) }, VisionApiFormat = "ollama", Temperature = 1.2, WebSearchEnabled = true, GitHubAppClientId = "Iv1.example-client-id", ChatFolders = ["Research", "Writing"] });
    var reloaded = settingsStore.Load();
    Check(reloaded.Model == "persisted-model" && Math.Abs(reloaded.Temperature - 1.2) < 0.001 && reloaded.WebSearchEnabled, "settings survive save and reload");
    Check(reloaded.PinnedModels.SequenceEqual(["persisted-model", "second-model"]), "pinned model hotswapper selection survives settings save and reload");
    Check(reloaded.ModelProvider == "LM Studio" && reloaded.PinnedModelConnections.Count == 2 && reloaded.PinnedModelConnections[1].Endpoint.EndsWith(":11434/v1", StringComparison.Ordinal), "pinned model endpoints survive settings save and reload");
    Check(reloaded.LocalModelPerformance["http://127.0.0.1:1234/v1\npersisted-model"].Samples == 3, "local model benchmark measurements survive settings save and reload");
    Check(reloaded.VisionApiFormat == "ollama", "vision API format selection survives save and reload");
    Check(reloaded.GitHubAppClientId == "Iv1.example-client-id", "GitHub App client ID persists as a non-secret preference");
    Check(reloaded.ChatFolders.SequenceEqual(["Research", "Writing"]), "chat folders survive settings save and reload");

    var corruptSettingsPath = Path.Combine(settingsDirectory, "corrupt-settings.json");
    File.WriteAllText(corruptSettingsPath, "{broken");
    var recoveredSettings = new JsonSettingsStore(corruptSettingsPath).Load();
    Check(recoveredSettings is not null && Directory.EnumerateFiles(settingsDirectory, "corrupt-settings.json.corrupt-*").Any(), "corrupt settings are preserved for recovery");

    var conversationsPath = Path.Combine(settingsDirectory, "conversations.json");
    var conversationStore = new ConversationStore(conversationsPath);
    try
    {
        conversationStore.Save([new ChatSession { Messages = [new ChatMessage("user", new string('x', BoundedFileReader.MaxJsonBytes))] }]);
        throw new Exception("Expected oversized conversation persistence to be rejected.");
    }
    catch (InvalidDataException)
    {
        Check(true, "native persistence rejects oversized serialized conversations before writing");
    }
    conversationStore.Save([
        new ChatSession
        {
            Id = "chat-one",
            Title = "Persisted local conversation",
            UpdatedAt = DateTimeOffset.UtcNow,
            ProjectId = "project-demo",
            ProjectName = "Demo project",
            ProjectInstructions = "Use concise answers.",
            Mode = "Coding Agent",
            IncludeProjectFilesInChat = true,
            FolderName = "Research",
            Messages = [new ChatMessage("user", "Keep this on device", [new ChatAttachment("notes.md", "Attached note")]), new ChatMessage("assistant", "Saved locally.", Sources: [new ChatSource("Docs", "https://example.org", "A snippet")])]
        },
        new ChatSession
        {
            Id = "chat-two",
            Title = "Another conversation",
            Messages = [new ChatMessage("user", "A separate thread", [new ChatAttachment("scene.jpg", Convert.ToBase64String([0xFF, 0xD8, 0xFF, 0xD9]), "image/jpeg")])]
        }
    ]);
    var restoredChats = conversationStore.Load();
    Check(restoredChats.Count == 2, "conversation store keeps multiple chats");
    Check(restoredChats[0].Messages[1].Content == "Saved locally.", "conversation messages survive save and reload");
    Check(restoredChats[0].Messages[0].Attachments is [{ FileName: "notes.md", Content: "Attached note" }], "text attachments survive conversation save and reload");
    Check(restoredChats[1].Messages[0].Attachments is [{ FileName: "scene.jpg", MediaType: "image/jpeg", Content: "/9j/2Q==" }], "image attachments survive local conversation save and reload");
    Check(restoredChats[0].Messages[1].Sources is [{ Title: "Docs", Url: "https://example.org", Snippet: "A snippet" }], "web source cards survive conversation save and reload");
    Check(restoredChats[0].ProjectId == "project-demo" && restoredChats[0].ProjectName == "Demo project" && restoredChats[0].ProjectInstructions == "Use concise answers.", "project context stays attached to its conversation");
    Check(restoredChats[0].Mode == "Coding Agent", "chat and coding-agent workspace identity survives save and reload");
    Check(restoredChats[0].IncludeProjectFilesInChat, "per-conversation project file context opt-in survives save and reload");
    Check(restoredChats[0].FolderName == "Research", "conversation folder assignment survives save and reload");
    Check(restoredChats[1].FolderName is null, "older conversations without folder metadata remain unfiled");
    var editedChats = restoredChats.ToList();
    editedChats[0].Title = "Renamed thread";
    editedChats.RemoveAt(1);
    conversationStore.Save(editedChats);
    var reloadedEdits = conversationStore.Load();
    Check(reloadedEdits.Count == 1 && reloadedEdits[0].Title == "Renamed thread", "renaming and deleting conversations survive save and reload");

    var projectsPath = Path.Combine(settingsDirectory, "projects.json");
    var projectStore = new ProjectStore(projectsPath);
    projectStore.Save([
        new LocalProject { Id = "project-demo", Name = "Demo project", Instructions = "Use concise answers." },
        new LocalProject { Id = "project-notes", Name = "Notes", Instructions = "Prefer bullet points." }
    ]);
    var restoredProjects = projectStore.Load();
    Check(restoredProjects.Count == 2, "project store keeps multiple projects");
    Check(restoredProjects[0].Instructions == "Use concise answers." && restoredProjects[1].Instructions == "Prefer bullet points.", "project instructions survive save and reload independently");

    var workspacePath = Path.Combine(settingsDirectory, "workspace");
    Directory.CreateDirectory(Path.Combine(workspacePath, "src"));
    Directory.CreateDirectory(Path.Combine(workspacePath, ".git"));
    Directory.CreateDirectory(Path.Combine(workspacePath, "node_modules"));
    File.WriteAllText(Path.Combine(workspacePath, "README.md"), "Workspace overview");
    File.WriteAllText(Path.Combine(workspacePath, ".env"), "API_KEY=secret");
    File.WriteAllText(Path.Combine(workspacePath, ".git", "config"), "private git data");
    File.WriteAllText(Path.Combine(workspacePath, "node_modules", "package.json"), "dependency data");
    File.WriteAllText(Path.Combine(workspacePath, "src", "Program.cs"), "Console.WriteLine(\"local\");");
    var workspaceFiles = WorkspaceFileContext.ReadSelectedFiles([workspacePath]);
    Check(workspaceFiles.Any(item => item.Path == "README.md"), "selected workspace context reads project documentation");
    Check(workspaceFiles.Any(item => item.Path == Path.Combine("src", "Program.cs")), "selected workspace context reads nested source files");
    Check(workspaceFiles.All(item => !item.Path.Contains(".git", StringComparison.OrdinalIgnoreCase) && !item.Path.Contains("node_modules", StringComparison.OrdinalIgnoreCase) && !item.Path.EndsWith(".env", StringComparison.OrdinalIgnoreCase)), "workspace context skips common secrets and dependency metadata");
    Check(workspaceFiles.Count <= WorkspaceFileContext.MaxFiles && workspaceFiles.Sum(item => item.Content.Length) <= WorkspaceFileContext.MaxTotalCharacters, "workspace context stays within its file and character budgets");

    var commandRunner = new WorkspaceCommandRunner();
    var commandResponse = "Check the result first.\n<workspace-command cwd=\"src\" timeout-seconds=\"999\">  printf 'one\\ntwo'  </workspace-command>\n<workspace-command>echo next</workspace-command>";
    var proposedCommands = WorkspaceCommandProposalParser.Parse(commandResponse);
    Check(proposedCommands.Count == 2 && proposedCommands[0].RelativeWorkingDirectory == "src" && proposedCommands[0].Command.Contains("printf", StringComparison.Ordinal), "Coding Agent command proposals preserve command and selected project subfolder");
    Check(proposedCommands[0].TimeoutSeconds == 120 && proposedCommands[1].TimeoutSeconds == 30, "Coding Agent command proposals clamp long timeouts and default missing timeouts");
    Check(!WorkspaceCommandProposalParser.RemoveProposals(commandResponse).Contains("workspace-command", StringComparison.Ordinal), "internal command proposal markup is hidden from the conversation text");
    try
    {
        await commandRunner.RunAsync(workspacePath, ".", "echo too-fast", TimeSpan.FromMilliseconds(500));
        throw new Exception("Expected a sub-second command timeout to be rejected.");
    }
    catch (ArgumentOutOfRangeException)
    {
        Check(true, "project command rejects sub-second timeouts");
    }
    const string secretEnvironmentName = "SERVER_ROCK_TEST_SECRET";
    var previousSecretEnvironment = Environment.GetEnvironmentVariable(secretEnvironmentName);
    Environment.SetEnvironmentVariable(secretEnvironmentName, "must-not-leak");
    try
    {
        var environmentResult = await commandRunner.RunAsync(workspacePath, ".",
            OperatingSystem.IsWindows() ? $"Write-Output $env:{secretEnvironmentName}" : $"printf \"${secretEnvironmentName}\"",
            TimeSpan.FromSeconds(10));
        Check(!environmentResult.Output.Contains("must-not-leak", StringComparison.Ordinal), "project commands do not inherit secret-shaped environment variables");
    }
    finally
    {
        Environment.SetEnvironmentVariable(secretEnvironmentName, previousSecretEnvironment);
    }
    var commandResult = await commandRunner.RunAsync(workspacePath, "src",
        OperatingSystem.IsWindows() ? "Write-Output server-rock-command-ok" : "printf server-rock-command-ok",
        TimeSpan.FromSeconds(10));
    Check(commandResult.ExitCode == 0 && commandResult.Output.Contains("server-rock-command-ok", StringComparison.Ordinal), "approved project command runs in the selected project subfolder and captures output");
    Check(commandResult.WorkingDirectory == Path.Combine(workspacePath, "src"), "project command reports its resolved working directory");
    var timedCommand = await commandRunner.RunAsync(workspacePath, ".",
        OperatingSystem.IsWindows() ? "Start-Sleep -Seconds 5" : "sleep 5",
        TimeSpan.FromSeconds(1));
    Check(timedCommand.TimedOut, "project command timeout stops the process instead of leaving a task running");
    await using (var terminalSession = new WorkspaceTerminalSession())
    {
        await terminalSession.StartAsync(workspacePath, shell: OperatingSystem.IsWindows() ? "Command Prompt" : null);
        var terminalCommand = OperatingSystem.IsWindows()
            ? "echo server-rock-pty-ok & exit"
            : "printf 'server-rock-pty-ok\\n'; exit";
        await terminalSession.SendTextAsync(terminalCommand + "\r");
        var terminalExitCode = await terminalSession.Completion.WaitAsync(TimeSpan.FromSeconds(15));
        var terminalScreen = terminalSession.ScreenText;
        Check(terminalExitCode == 0 && terminalScreen.Contains("server-rock-pty-ok", StringComparison.Ordinal), "interactive project terminal accepts input and renders a real PTY response");
    }
    try
    {
        await commandRunner.RunAsync(workspacePath, "../", "echo outside", TimeSpan.FromSeconds(1));
        throw new Exception("Expected a project command working-directory traversal to be rejected.");
    }
    catch (ArgumentException)
    {
        Check(true, "project command refuses working directories outside the selected folder");
    }

    var editor = new WorkspaceFileEditor();
    var oversizedPreviewPath = Path.Combine(workspacePath, "src", "Oversized.cs");
    File.WriteAllText(oversizedPreviewPath, new string('x', WorkspaceFileEditor.MaxCharacters * 4 + 1));
    try
    {
        _ = editor.ReadText(workspacePath, Path.Combine("src", "Oversized.cs"));
        throw new Exception("Expected oversized workspace preview to be rejected.");
    }
    catch (InvalidOperationException)
    {
        Check(true, "workspace editor bounds oversized file previews");
    }
    try
    {
        _ = editor.GetCurrentHash(workspacePath, Path.Combine("src", "Oversized.cs"));
        throw new Exception("Expected oversized workspace hashing to be rejected.");
    }
    catch (InvalidOperationException)
    {
        Check(true, "workspace editor bounds oversized file hashing before undo state capture");
    }
    var replacementDiff = WorkspaceTextDiff.Create("one\ntwo\nthree\n", "one\nTWO\nthree\n");
    Check(replacementDiff.AddedLineCount == 1 && replacementDiff.RemovedLineCount == 1, "workspace diff reports one replaced line as one addition and removal");
    Check(replacementDiff.Lines.Any(item => item.Kind == WorkspaceDiffLineKind.Removed && item.Text == "two") && replacementDiff.Lines.Any(item => item.Kind == WorkspaceDiffLineKind.Added && item.Text == "TWO"), "workspace diff preserves removed and added replacement text");
    var identicalDiff = WorkspaceTextDiff.Create("same\n", "same\n");
    Check(!identicalDiff.HasTextChanges && identicalDiff.Lines.Count == 0, "workspace diff marks identical files as having no text changes");
    var newlineDiff = WorkspaceTextDiff.Create("line\n", "line");
    Check(newlineDiff.HasTextChanges && newlineDiff.Lines.Any(item => item.Kind == WorkspaceDiffLineKind.Metadata && item.Text.Contains("No newline at end of proposed", StringComparison.Ordinal)), "workspace diff exposes a missing final newline");
    var largeDiff = WorkspaceTextDiff.Create(string.Join('\n', Enumerable.Range(1, WorkspaceTextDiff.MaxComparableLines + 1)), "small");
    Check(largeDiff.IsTooLarge && largeDiff.Lines.Count == 0, "workspace diff declines oversized files instead of rendering a misleading preview");
    try
    {
        editor.Apply(workspacePath, "../outside.txt", "should not escape", null);
        throw new Exception("Expected workspace traversal to be rejected.");
    }
    catch (InvalidOperationException)
    {
        Check(!File.Exists(Path.Combine(settingsDirectory, "outside.txt")), "workspace editor rejects paths outside the selected folder");
    }
    try
    {
        editor.Apply(workspacePath, ".env", "API_KEY=changed", null);
        throw new Exception("Expected protected secrets to be rejected.");
    }
    catch (InvalidOperationException)
    {
        Check(File.ReadAllText(Path.Combine(workspacePath, ".env")) == "API_KEY=secret", "workspace editor refuses to modify secret files");
    }
    var createdReceipt = editor.Apply(workspacePath, Path.Combine("src", "NewFile.cs"), "// proposed", null);
    Check(File.ReadAllText(Path.Combine(workspacePath, "src", "NewFile.cs")) == "// proposed", "approved workspace write creates the reviewed file");
    editor.Undo(createdReceipt);
    Check(!File.Exists(Path.Combine(workspacePath, "src", "NewFile.cs")), "undo removes a file created by the agent");
    var readmePath = Path.Combine(workspacePath, "README.md");
    var readmeHash = editor.GetCurrentHash(workspacePath, "README.md");
    var replacedReceipt = editor.Apply(workspacePath, "README.md", "Updated overview", readmeHash);
    editor.Undo(replacedReceipt);
    Check(File.ReadAllText(readmePath) == "Workspace overview", "undo restores the previous text file contents");
    var undoHistoryPath = Path.Combine(settingsDirectory, "workspace-undo.json");
    var undoStore = new WorkspaceUndoStore(undoHistoryPath);
    var durableReceipt = editor.Apply(workspacePath, Path.Combine("src", "Durable.cs"), "// durable change", null);
    var durableEntry = new WorkspaceUndoEntry("durable-entry", DateTimeOffset.UtcNow, "Test project", workspacePath,
        Path.Combine("src", "Durable.cs"), durableReceipt.PreviousContent, durableReceipt.AppliedHash);
    undoStore.Add([], durableEntry);
    var reloadedUndoStore = new WorkspaceUndoStore(undoHistoryPath);
    var reloadedUndo = reloadedUndoStore.Load();
    Check(reloadedUndo.Count == 1 && reloadedUndo[0].Id == durableEntry.Id, "approved file undo history survives store reload");
    reloadedUndoStore.Undo(reloadedUndo[0], editor);
    reloadedUndoStore.Remove(reloadedUndo, durableEntry.Id);
    Check(!File.Exists(Path.Combine(workspacePath, "src", "Durable.cs")) && reloadedUndoStore.Load().Count == 0, "persisted undo safely restores and removes an approved new file");
    var durableConflictReceipt = editor.Apply(workspacePath, Path.Combine("src", "Durable.cs"), "// approved", null);
    var durableConflictEntry = new WorkspaceUndoEntry("conflict-entry", DateTimeOffset.UtcNow, "Test project", workspacePath,
        Path.Combine("src", "Durable.cs"), durableConflictReceipt.PreviousContent, durableConflictReceipt.AppliedHash);
    reloadedUndoStore.Add([], durableConflictEntry);
    File.WriteAllText(Path.Combine(workspacePath, "src", "Durable.cs"), "// newer user change");
    try
    {
        reloadedUndoStore.Undo(reloadedUndoStore.Load()[0], editor);
        throw new Exception("Expected persisted undo to preserve newer work.");
    }
    catch (IOException)
    {
        Check(File.ReadAllText(Path.Combine(workspacePath, "src", "Durable.cs")) == "// newer user change", "persisted undo refuses to overwrite newer file changes");
    }
    try
    {
        editor.Undo(workspacePath, Path.Combine("..", "outside.txt"), durableConflictReceipt);
        throw new Exception("Expected project-relative undo to reject traversal.");
    }
    catch (InvalidOperationException)
    {
        Check(true, "workspace undo refuses paths outside the selected project");
    }
    var conflictReceipt = editor.Apply(workspacePath, "README.md", "Reviewed change", readmeHash);
    File.WriteAllText(readmePath, "User changed after review");
    try
    {
        editor.Undo(conflictReceipt);
        throw new Exception("Expected undo to preserve a newer user change.");
    }
    catch (IOException)
    {
        Check(File.ReadAllText(readmePath) == "User changed after review", "undo refuses to overwrite edits made after agent approval");
    }

    projectStore.Save([new LocalProject { Id = "project-folders", Name = "Folder project", FolderPaths = [workspacePath] }]);
    Check(projectStore.Load()[0].FolderPaths.SequenceEqual([workspacePath]), "selected project folders survive save and reload");

    File.WriteAllText(conversationsPath, "{invalid json");
    try
    {
        conversationStore.Load();
        throw new Exception("Expected corrupt history to be rejected.");
    }
    catch (InvalidDataException)
    {
        Check(File.ReadAllText(conversationsPath) == "{invalid json", "corrupt conversation data is preserved for recovery");
    }
    File.WriteAllText(projectsPath, "{invalid json");
    try
    {
        projectStore.Load();
        throw new Exception("Expected corrupt project data to be rejected.");
    }
    catch (InvalidDataException)
    {
        Check(File.ReadAllText(projectsPath) == "{invalid json", "corrupt project data is preserved for recovery");
    }
    File.WriteAllText(undoHistoryPath, "{invalid json");
    try
    {
        reloadedUndoStore.Load();
        throw new Exception("Expected corrupt undo history to be rejected.");
    }
    catch (InvalidDataException)
    {
        Check(File.ReadAllText(undoHistoryPath) == "{invalid json", "corrupt undo history is preserved for recovery");
    }
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
