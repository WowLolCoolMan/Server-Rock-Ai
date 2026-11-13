// Model Library back end: finds installed models on known local runtimes
// (Ollama, LM Studio, …), searches Hugging Face and ModelScope, downloads/imports GGUF files, and
// pulls/deletes Ollama models.
// Depends on: HttpClient, BoundedHttpContent (size-limited responses).
// Used by: MainPage Model Library dialog and model selection.

using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using SkaiDesktop.Core.Infrastructure;
using SkaiDesktop.Core.Models;

namespace SkaiDesktop.Core.Services;

/// <summary>Catalog and installation adapters for chat-ready local model runtimes.</summary>
public sealed class ModelLibraryService(HttpClient? httpClient = null)
{
    private const int MaxModelIdentifierLength = 512;
    private const int MaxCatalogQueryLength = 256;
    private static readonly HttpClient SharedHttp = new() { Timeout = Timeout.InfiniteTimeSpan };
    private static readonly (string Provider, string Endpoint)[] LocalHosts =
    [
        ("Ollama", "http://127.0.0.1:11434/v1"),
        ("GPT4All", "http://127.0.0.1:4891/v1"),
        ("LM Studio", "http://127.0.0.1:1234/v1"),
        ("Jan", "http://127.0.0.1:1337/v1"),
        ("LocalAI / llama.cpp", "http://127.0.0.1:8080/v1"),
        ("vLLM", "http://127.0.0.1:8000/v1"),
        ("KoboldCpp", "http://127.0.0.1:5001/v1"),
        ("Text Generation WebUI", "http://127.0.0.1:5000/v1")
    ];
    private readonly HttpClient _http = httpClient ?? SharedHttp;

    public async Task<IReadOnlyList<InstalledLocalModel>> DiscoverLocalModelsAsync(string configuredEndpoint, CancellationToken cancellationToken = default)
    {
        var targets = LocalHosts.ToList();
        if (IsLoopbackEndpoint(configuredEndpoint) && !targets.Any(target => SameHost(target.Endpoint, configuredEndpoint)))
            targets.Add(("Current local endpoint", NormalizeModelsEndpoint(configuredEndpoint)));
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(3));
        var results = await Task.WhenAll(targets.Select(async target =>
        {
            try
            {
                if (target.Provider == "Ollama") return await DiscoverOllamaModelsAsync(target.Endpoint, timeout.Token);
                if (target.Provider == "LM Studio") return await DiscoverLmStudioModelsAsync(target.Endpoint, timeout.Token);
                var uri = BuildModelsUri(target.Endpoint);
                using var response = await _http.GetAsync(uri, timeout.Token);
                if (!response.IsSuccessStatusCode) return Array.Empty<InstalledLocalModel>();
                using var json = await BoundedHttpContent.ReadJsonAsync(response.Content, timeout.Token, "Local model response");
                if (!json.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                    return Array.Empty<InstalledLocalModel>();
                return data.EnumerateArray()
                    .Where(item => item.TryGetProperty("id", out _))
                    .Select(item => item.GetProperty("id").GetString())
                    .Where(IsSafeCatalogIdentifier)
                    .Select(id => new InstalledLocalModel(target.Provider, id!, target.Endpoint))
                    .ToArray();
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return Array.Empty<InstalledLocalModel>(); }
            catch (HttpRequestException) { return Array.Empty<InstalledLocalModel>(); }
            catch (JsonException) { return Array.Empty<InstalledLocalModel>(); }
            // A local port may be occupied by an unrelated or unhealthy service.
            // Inventory discovery is best-effort, so a bounded-body failure from one
            // runtime must not hide models served by every other local runtime.
            catch (InvalidDataException) { return Array.Empty<InstalledLocalModel>(); }
        }));
        return results.SelectMany(models => models)
            .DistinctBy(model => (model.Model, model.Endpoint), StringTupleComparer.Instance)
            .OrderBy(model => model.Provider, StringComparer.OrdinalIgnoreCase)
            .ThenBy(model => model.Model, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task<InstalledLocalModel[]> DiscoverOllamaModelsAsync(string endpoint, CancellationToken cancellationToken)
    {
        var root = new Uri(endpoint);
        var tagsUri = new UriBuilder(root) { Path = "/api/tags", Query = "", Fragment = "" }.Uri;
        using var response = await _http.GetAsync(tagsUri, cancellationToken);
        if (!response.IsSuccessStatusCode) return [];
        using var json = await BoundedHttpContent.ReadJsonAsync(response.Content, cancellationToken, "Local model inventory response");
        if (!json.RootElement.TryGetProperty("models", out var models) || models.ValueKind != JsonValueKind.Array) return [];
        return models.EnumerateArray()
            .Where(item => item.TryGetProperty("name", out _))
            .Select(item => new InstalledLocalModel(
                "Ollama",
                item.GetProperty("name").GetString() ?? "",
                endpoint,
                item.TryGetProperty("details", out var details) && details.TryGetProperty("family", out var family) ? family.GetString() : null,
                item.TryGetProperty("size", out var size) && size.TryGetInt64(out var bytes) ? bytes : null,
                null))
            .Where(model => IsSafeCatalogIdentifier(model.Model))
            .ToArray();
    }

    private async Task<InstalledLocalModel[]> DiscoverLmStudioModelsAsync(string endpoint, CancellationToken cancellationToken)
    {
        var root = new Uri(endpoint);
        var modelsUri = new UriBuilder(root) { Path = "/api/v1/models", Query = "", Fragment = "" }.Uri;
        using var response = await _http.GetAsync(modelsUri, cancellationToken);
        if (!response.IsSuccessStatusCode) return [];
        using var json = await BoundedHttpContent.ReadJsonAsync(response.Content, cancellationToken, "Local model metadata response");
        if (!json.RootElement.TryGetProperty("models", out var models) || models.ValueKind != JsonValueKind.Array) return [];
        return models.EnumerateArray()
            .Where(item => item.TryGetProperty("type", out var type) && type.GetString() == "llm" && item.TryGetProperty("key", out _))
            .Select(item => new InstalledLocalModel(
                "LM Studio",
                item.GetProperty("key").GetString() ?? "",
                endpoint,
                item.TryGetProperty("display_name", out var displayName) ? displayName.GetString() : null,
                item.TryGetProperty("size_bytes", out var size) && size.TryGetInt64(out var bytes) ? bytes : null,
                item.TryGetProperty("loaded_instances", out var loaded) && loaded.ValueKind == JsonValueKind.Array ? loaded.GetArrayLength() > 0 : null))
            .Where(model => IsSafeCatalogIdentifier(model.Model))
            .ToArray();
    }

    public async Task EnsureLmStudioModelLoadedAsync(string endpoint, string model, Action<string>? onStatus = null, CancellationToken cancellationToken = default)
    {
        var modelId = ValidateModelIdentifier(model, "LM Studio model");
        if (!Uri.TryCreate(endpoint.Trim().TrimEnd('/'), UriKind.Absolute, out var configured) || !configured.IsLoopback)
            throw new InvalidOperationException("LM Studio model loading is available only from a local LM Studio endpoint.");
        var root = new UriBuilder(configured) { Path = "", Query = "", Fragment = "" }.Uri;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(5));
        using (var listResponse = await _http.GetAsync(new Uri(root, "/api/v1/models"), timeout.Token))
        {
            if (!listResponse.IsSuccessStatusCode)
                throw new InvalidOperationException($"Could not check LM Studio model status ({(int)listResponse.StatusCode}). Its REST API may require an API token, which is not configured here.");
            using var list = await BoundedHttpContent.ReadJsonAsync(listResponse.Content, timeout.Token, "Ollama model load response");
            if (list.RootElement.TryGetProperty("models", out var available) && available.ValueKind == JsonValueKind.Array)
            {
                var match = available.EnumerateArray().FirstOrDefault(item => item.TryGetProperty("key", out var key) && key.GetString()?.Equals(modelId, StringComparison.OrdinalIgnoreCase) == true);
                if (match.ValueKind != JsonValueKind.Undefined && match.TryGetProperty("loaded_instances", out var loaded) && loaded.ValueKind == JsonValueKind.Array && loaded.GetArrayLength() > 0) return;
            }
        }
        onStatus?.Invoke($"Loading {modelId} into LM Studio memory… This may take a while for a large model.");
        using var loadResponse = await _http.PostAsJsonAsync(new Uri(root, "/api/v1/models/load"), new { model = modelId }, timeout.Token);
        var body = await BoundedHttpContent.ReadTextAsync(loadResponse.Content, timeout.Token, "Ollama model load response");
        if (!loadResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"LM Studio could not load {modelId} ({(int)loadResponse.StatusCode}). Check available memory and API authentication. {body}");
    }

    private static bool IsLoopbackEndpoint(string endpoint) =>
        Uri.TryCreate(endpoint.Trim().TrimEnd('/'), UriKind.Absolute, out var uri) &&
        (uri.Scheme is "http" or "https") && uri.IsLoopback && string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Fragment);

    private static bool SameHost(string left, string right) =>
        Uri.TryCreate(left.Trim().TrimEnd('/'), UriKind.Absolute, out var leftUri) &&
        Uri.TryCreate(right.Trim().TrimEnd('/'), UriKind.Absolute, out var rightUri) &&
        leftUri.Port == rightUri.Port &&
        (leftUri.IsLoopback && rightUri.IsLoopback || leftUri.Host.Equals(rightUri.Host, StringComparison.OrdinalIgnoreCase));

    private static string NormalizeModelsEndpoint(string endpoint)
    {
        var uri = new Uri(endpoint.Trim().TrimEnd('/'));
        var path = uri.AbsolutePath.TrimEnd('/');
        if (path.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)) path = path[..^"/chat/completions".Length];
        if (!path.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)) path += "/v1";
        return new UriBuilder(uri) { Path = path, Query = "", Fragment = "" }.Uri.ToString().TrimEnd('/');
    }

    private static Uri BuildModelsUri(string endpoint)
    {
        var root = new Uri(endpoint.Trim().TrimEnd('/'));
        var path = root.AbsolutePath.TrimEnd('/');
        if (!path.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)) path += "/v1";
        return new UriBuilder(root) { Path = path + "/models", Query = "", Fragment = "" }.Uri;
    }

    private sealed class StringTupleComparer : IEqualityComparer<(string Model, string Endpoint)>
    {
        public static readonly StringTupleComparer Instance = new();
        public bool Equals((string Model, string Endpoint) x, (string Model, string Endpoint) y) =>
            x.Model.Equals(y.Model, StringComparison.OrdinalIgnoreCase) && x.Endpoint.Equals(y.Endpoint, StringComparison.OrdinalIgnoreCase);
        public int GetHashCode((string Model, string Endpoint) value) => HashCode.Combine(
            StringComparer.OrdinalIgnoreCase.GetHashCode(value.Model), StringComparer.OrdinalIgnoreCase.GetHashCode(value.Endpoint));
    }

    public async Task<IReadOnlyList<HubModel>> SearchHuggingFaceAsync(string query, bool uncensored, CancellationToken cancellationToken = default)
    {
        var queryText = ValidateCatalogQuery(query);
        var searchTerms = uncensored
            ? new[] { "uncensored", "abliterated", "abliteration" }
            : new[] { queryText };
        var allModels = new List<HubModel>();
        foreach (var term in searchTerms)
        {
            var searchText = string.Join(' ', new[] { queryText, term }.Where(value => value.Length > 0));
            var uri = new Uri("https://huggingface.co/api/models?" +
                "search=" + Uri.EscapeDataString(searchText) + "&filter=gguf&sort=downloads&direction=-1&limit=50&full=true");
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.ParseAdd(ProductIdentity.UserAgent("local model library"));
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            using var response = await _http.SendAsync(request, timeout.Token);
            response.EnsureSuccessStatusCode();
            using var json = await BoundedHttpContent.ReadJsonAsync(response.Content, timeout.Token, "Model catalog response");
            allModels.AddRange(json.RootElement.EnumerateArray()
            .Where(item => item.TryGetProperty("id", out var modelId) && IsSafeCatalogIdentifier(modelId.GetString()))
            .Select(item =>
            {
                var id = item.TryGetProperty("id", out var modelId) ? modelId.GetString() ?? "" : "";
                var tags = item.TryGetProperty("tags", out var tagElement) && tagElement.ValueKind == JsonValueKind.Array
                    ? tagElement.EnumerateArray().Where(tag => tag.ValueKind == JsonValueKind.String).Select(tag => tag.GetString() ?? "").ToArray()
                    : [];
                var label = IsExplicitUncensoredLabel(id, tags);
                return new HubModel(id,
                    item.TryGetProperty("downloads", out var downloads) && downloads.TryGetInt64(out var count) ? count : 0,
                    item.TryGetProperty("likes", out var likes) && likes.TryGetInt32(out var likeCount) ? likeCount : 0,
                    label, tags);
            }));
        }
        var filtered = (uncensored ? allModels.Where(model => model.IsUncensored) : allModels.Where(model => !model.IsUncensored));
        return filtered.DistinctBy(model => model.Id, StringComparer.OrdinalIgnoreCase).OrderByDescending(model => model.Downloads).ToArray();
    }

    private static bool IsExplicitUncensoredLabel(string id, IEnumerable<string> tags) =>
        new[] { id }.Concat(tags).Any(value => value.Contains("uncensored", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("abliterat", StringComparison.OrdinalIgnoreCase));

    public async Task<IReadOnlyList<ModelScopeGgufPackage>> SearchModelScopeGgufAsync(string query, bool uncensored, CancellationToken cancellationToken = default)
    {
        var queryText = ValidateCatalogQuery(query);
        var searchTerms = uncensored ? new[] { "uncensored", "abliterated", "abliteration" } : [queryText];
        var repositories = new Dictionary<string, (long Downloads, bool IsUncensored)>(StringComparer.OrdinalIgnoreCase);
        foreach (var term in searchTerms)
        {
            var searchText = string.Join(' ', new[] { queryText, term }.Where(value => value.Length > 0));
            if (searchText.Length == 0) continue;
            var uri = new Uri("https://modelscope.cn/openapi/v1/models?search=" + Uri.EscapeDataString(searchText) + "&sort=downloads&page_size=20");
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.ParseAdd(ProductIdentity.UserAgent("local model library"));
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var json = await BoundedHttpContent.ReadJsonAsync(response.Content, cancellationToken, "ModelScope search response");
            if (!json.RootElement.TryGetProperty("data", out var data) || !data.TryGetProperty("models", out var models) || models.ValueKind != JsonValueKind.Array) continue;
            foreach (var item in models.EnumerateArray())
            {
                var id = item.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
                if (string.IsNullOrWhiteSpace(id) || !Regex.IsMatch(id, "^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$")) continue;
                var tasks = item.TryGetProperty("tasks", out var taskElement) && taskElement.ValueKind == JsonValueKind.Array
                    ? taskElement.EnumerateArray().Where(task => task.ValueKind == JsonValueKind.String).Select(task => task.GetString() ?? "").ToArray()
                    : [];
                if (!IsChatModelCatalogEntry(id, tasks)) continue;
                var tags = item.TryGetProperty("tags", out var tagElement) && tagElement.ValueKind == JsonValueKind.Array
                    ? tagElement.EnumerateArray().Where(tag => tag.ValueKind == JsonValueKind.String).Select(tag => tag.GetString() ?? "").ToArray()
                    : [];
                var isUncensored = IsExplicitUncensoredLabel(id, tags);
                if (uncensored != isUncensored) continue;
                var downloads = item.TryGetProperty("downloads", out var downloadElement) && downloadElement.TryGetInt64(out var count) ? count : 0;
                repositories[id] = (downloads, isUncensored);
            }
        }

        using var parallel = new SemaphoreSlim(4, 4);
        var manifests = await Task.WhenAll(repositories.OrderByDescending(pair => pair.Value.Downloads).Take(20).Select(async pair =>
        {
            await parallel.WaitAsync(cancellationToken);
            try { return await GetModelScopeGgufFilesAsync(pair.Key, pair.Value.Downloads, pair.Value.IsUncensored, cancellationToken); }
            finally { parallel.Release(); }
        }));
        return manifests.SelectMany(value => value)
            .OrderByDescending(value => value.Downloads)
            .ThenBy(value => value.Repository, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task<IReadOnlyList<ModelScopeGgufPackage>> GetModelScopeGgufFilesAsync(string repository, long downloads, bool isUncensored, CancellationToken cancellationToken)
    {
        var uri = new Uri("https://modelscope.cn/api/v1/models/" + repository + "/repo/files?Revision=master&Recursive=True");
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.UserAgent.ParseAdd(ProductIdentity.UserAgent("local model library"));
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode) return [];
        using var json = await BoundedHttpContent.ReadJsonAsync(response.Content, cancellationToken, "ModelScope file manifest");
        if (!json.RootElement.TryGetProperty("Data", out var data) || !data.TryGetProperty("Files", out var filesElement)) return [];
        if (filesElement.ValueKind == JsonValueKind.Object && filesElement.TryGetProperty("Files", out var nestedFiles)) filesElement = nestedFiles;
        if (filesElement.ValueKind != JsonValueKind.Array) return [];
        var files = filesElement.EnumerateArray()
            .Where(item => item.TryGetProperty("Type", out var type) && type.GetString() == "blob" && item.TryGetProperty("Path", out var path) && path.GetString()?.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase) == true)
            .Where(item =>
            {
                var name = item.GetProperty("Path").GetString()!.Split('/').Last();
                return !name.StartsWith("mmproj", StringComparison.OrdinalIgnoreCase) && !name.Contains("projector", StringComparison.OrdinalIgnoreCase);
            })
            .Select(item => new ModelScopeGgufFile(
                item.GetProperty("Path").GetString()!,
                item.TryGetProperty("Size", out var size) && size.TryGetInt64(out var bytes) ? bytes : 0,
                item.TryGetProperty("Sha256", out var hash) ? hash.GetString() : null))
            .Where(file => !string.IsNullOrWhiteSpace(file.Sha256) && Regex.IsMatch(file.Sha256, "^[a-fA-F0-9]{64}$"))
            .ToArray();
        return files
            .GroupBy(file => Regex.Replace(file.Path, @"-\d{5}-of-\d{5}(?=\.gguf$)", "", RegexOptions.IgnoreCase), StringComparer.OrdinalIgnoreCase)
            .Select(group => new ModelScopeGgufPackage(repository, group.Key, group.OrderBy(file => file.Path, StringComparer.OrdinalIgnoreCase).ToArray(), downloads, isUncensored))
            .ToArray();
    }

    private static bool IsChatModelCatalogEntry(string repository, IEnumerable<string> tasks)
    {
        var name = repository.ToLowerInvariant();
        if (new[] { "embedding", "reranker", "rerank", "guard", "reward-model", "reward_model", "-asr", "asr-", "-tts", "tts-", "text-to-speech", "whisper", "speech-recognition", "-audio", "audio-", "classifier", "classification" }
            .Any(nonChat => name.Contains(nonChat, StringComparison.Ordinal)))
            return false;

        var taskList = tasks.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        if (taskList.Length > 0)
            return taskList.Any(task => task.Equals("text-generation", StringComparison.OrdinalIgnoreCase) || task.Equals("chat", StringComparison.OrdinalIgnoreCase));

        return true;
    }

    public async Task<string> DownloadAndImportModelScopeGgufAsync(string endpoint, ModelScopeGgufPackage package, Action<string>? onProgress = null, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(endpoint.Trim().TrimEnd('/'), UriKind.Absolute, out var configured) || configured.Scheme is not ("http" or "https") || !configured.IsLoopback)
            throw new InvalidOperationException("ModelScope GGUF imports require a local Ollama server.");
        var repositoryId = ValidateRepositoryIdentifier(package.Repository, "ModelScope repository");
        var packageName = ValidateModelIdentifier(package.Name, "ModelScope model");
        if (package.Files.Count == 0) throw new InvalidOperationException("The selected ModelScope entry contains no downloadable GGUF files.");

        var root = new UriBuilder(configured) { Path = "", Query = "", Fragment = "" }.Uri;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromHours(6));
        var temporaryDirectory = Path.Combine(Path.GetTempPath(), "server-rock-modelscope-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryDirectory);
        try
        {
            var blobMap = new Dictionary<string, string>(StringComparer.Ordinal);
            long completedBytes = 0;
            var totalBytes = package.Files.Sum(file => Math.Max(0, file.SizeBytes));
            foreach (var file in package.Files)
            {
                timeout.Token.ThrowIfCancellationRequested();
                if (!IsSafeModelScopeFilePath(file.Path) || string.IsNullOrWhiteSpace(file.Sha256) || !Regex.IsMatch(file.Sha256, "^[a-fA-F0-9]{64}$"))
                    throw new InvalidOperationException("The ModelScope file manifest contains an invalid filename or SHA-256 digest.");
                var fileName = file.Path.Split('/').Last();
                if (blobMap.ContainsKey(fileName)) throw new InvalidOperationException("The selected ModelScope package contains duplicate GGUF shard filenames.");
                var downloadUri = new Uri("https://modelscope.cn/api/v1/models/" + repositoryId + "/repo?Revision=master&FilePath=" + Uri.EscapeDataString(file.Path));
                using var request = new HttpRequestMessage(HttpMethod.Get, downloadUri);
                request.Headers.UserAgent.ParseAdd(ProductIdentity.UserAgent("local model library"));
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"ModelScope file download failed ({(int)response.StatusCode}) for {file.Path}.");
                if (response.Content.Headers.ContentLength is > 0 and var contentLength && file.SizeBytes > 0 && contentLength != file.SizeBytes)
                    throw new InvalidDataException($"ModelScope reported an unexpected file size for {file.Path}.");
                var localPath = Path.Combine(temporaryDirectory, fileName);
                await using var destination = new FileStream(localPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
                using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                await using var source = await response.Content.ReadAsStreamAsync(timeout.Token);
                var buffer = new byte[1024 * 1024];
                long fileBytes = 0;
                while (true)
                {
                    var read = await source.ReadAsync(buffer, timeout.Token);
                    if (read == 0) break;
                    await destination.WriteAsync(buffer.AsMemory(0, read), timeout.Token);
                    hash.AppendData(buffer, 0, read);
                    fileBytes += read;
                    onProgress?.Invoke(totalBytes > 0
                        ? $"Downloading {Path.GetFileName(file.Path)} · {(completedBytes + fileBytes) / 1073741824d:0.00} / {totalBytes / 1073741824d:0.00} GB"
                        : $"Downloading {Path.GetFileName(file.Path)} · {(completedBytes + fileBytes) / 1048576d:0.0} MB");
                }
                await destination.FlushAsync(timeout.Token);
                if (file.SizeBytes > 0 && fileBytes != file.SizeBytes) throw new InvalidDataException($"The downloaded size does not match ModelScope's manifest for {file.Path}.");
                var actualHash = Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
                if (!actualHash.Equals(file.Sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException($"SHA-256 verification failed for {file.Path}; the model was not imported.");
                await destination.DisposeAsync();
                await using (var signatureFile = File.OpenRead(localPath))
                {
                    var signature = new byte[4];
                    if (await signatureFile.ReadAsync(signature, timeout.Token) != 4 || !signature.SequenceEqual("GGUF"u8.ToArray()))
                        throw new InvalidDataException($"The selected file {file.Path} does not have a GGUF signature; the model was not imported.");
                }

                var digest = "sha256:" + actualHash;
                onProgress?.Invoke($"Importing {fileName} into Ollama…");
                using (var blobRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(root, "/api/blobs/" + digest)))
                {
                    var blobStream = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
                    blobRequest.Content = new StreamContent(blobStream);
                    blobRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                    using var blobResponse = await _http.SendAsync(blobRequest, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                    if (!blobResponse.IsSuccessStatusCode) throw new InvalidOperationException($"Ollama rejected the verified model blob ({(int)blobResponse.StatusCode}).");
                }
                blobMap[fileName] = digest;
                completedBytes += fileBytes;
            }

            var baseName = Regex.Replace(repositoryId.Split('/').Last() + "-" + Path.GetFileNameWithoutExtension(packageName), "[^A-Za-z0-9_.-]", "-").Trim('-', '.', '_').ToLowerInvariant();
            var modelName = "ms-" + (baseName.Length > 70 ? baseName[..70] : baseName);
            if (modelName.Length <= 3) modelName = "modelscope-model";
            onProgress?.Invoke($"Building {modelName} in Ollama…");
            using var createResponse = await _http.PostAsJsonAsync(new Uri(root, "/api/create"), new { model = modelName, files = blobMap, stream = true }, timeout.Token);
            if (!createResponse.IsSuccessStatusCode)
            {
                var body = await BoundedHttpContent.ReadTextAsync(createResponse.Content, timeout.Token, "Ollama model creation error");
                throw new InvalidOperationException($"Ollama could not create the model ({(int)createResponse.StatusCode}). {body}");
            }
            await using var createStream = await createResponse.Content.ReadAsStreamAsync(timeout.Token);
            using var reader = new StreamReader(createStream);
        var lineReader = BoundedHttpContent.CreateLineReader(reader);
            var succeeded = false;
        while (await lineReader.ReadLineAsync(timeout.Token) is { } line)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                using var status = JsonDocument.Parse(line);
                var message = status.RootElement.TryGetProperty("status", out var state) ? state.GetString() : null;
                if (status.RootElement.TryGetProperty("error", out var error)) throw new InvalidOperationException("Ollama: " + error.GetString());
                if (message is not null) onProgress?.Invoke(message);
                if (string.Equals(message, "success", StringComparison.OrdinalIgnoreCase)) succeeded = true;
            }
            if (!succeeded) throw new InvalidOperationException("Ollama closed the model import stream before confirming completion.");
            return modelName;
        }
        finally
        {
            try { Directory.Delete(temporaryDirectory, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static bool IsSafeModelScopeFilePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.StartsWith("/", StringComparison.Ordinal) || path.Contains('\\')) return false;
        var segments = path.Split('/');
        if (segments.Any(segment => segment is "" or "." or "..")) return false;
        var fileName = segments[^1];
        return fileName.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase) &&
            !fileName.Any(character => char.IsControl(character) || "<>:\"|?*".Contains(character));
    }

    private static string ValidateRepositoryIdentifier(string repository, string label)
    {
        var value = repository.Trim();
        if (value.Length > MaxModelIdentifierLength ||
            !Regex.IsMatch(value, @"^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$") ||
            value.Split('/').Any(segment => segment is "." or ".."))
            throw new InvalidOperationException($"The {label} identifier is invalid.");
        return value;
    }

    private static string ValidateModelIdentifier(string model, string label)
    {
        var value = model.Trim();
        if (value.Length == 0 || value.Length > MaxModelIdentifierLength ||
            value.Any(character => char.IsControl(character) || !(char.IsLetterOrDigit(character) || "_.-:/".Contains(character))))
            throw new InvalidOperationException($"The {label} identifier is invalid.");
        return value;
    }

    private static string ValidateCatalogQuery(string query)
    {
        var value = (query ?? "").Trim();
        if (value.Length > MaxCatalogQueryLength || value.Any(char.IsControl))
            throw new InvalidOperationException($"Model search text must be {MaxCatalogQueryLength} characters or fewer and contain no control characters.");
        return value;
    }

    private static bool IsSafeCatalogIdentifier(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= MaxModelIdentifierLength && !value.Any(char.IsControl);

    public async Task DownloadFromHuggingFaceAsync(string endpoint, string repository, string? quantization = null, Action<string>? onProgress = null, CancellationToken cancellationToken = default)
    {
        var repositoryId = ValidateRepositoryIdentifier(repository, "Hugging Face repository");
        var quantizationId = string.IsNullOrWhiteSpace(quantization) ? null : ValidateModelIdentifier(quantization, "quantization");
        if (quantizationId is { Length: > 128 }) throw new InvalidOperationException("The quantization identifier is too long.");
        if (!Uri.TryCreate(endpoint.Trim().TrimEnd('/'), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || !uri.IsLoopback)
            throw new InvalidOperationException("Set a valid LM Studio local server endpoint before downloading a Hugging Face model.");
        var root = new UriBuilder(uri) { Path = "", Query = "", Fragment = "" }.Uri;
        var downloadUri = new Uri(root, "/api/v1/models/download");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(45));
        var payload = quantizationId is null
            ? new Dictionary<string, string> { ["model"] = repositoryId }
            : new Dictionary<string, string> { ["model"] = repositoryId, ["quantization"] = quantizationId };
        using var response = await _http.PostAsJsonAsync(downloadUri, payload, timeout.Token);
        var body = await BoundedHttpContent.ReadTextAsync(response.Content, timeout.Token, "LM Studio download response");
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"LM Studio could not start this download ({(int)response.StatusCode}). Make sure its local server is running and the selected repository is supported. {body}");
        using var json = JsonDocument.Parse(body);
        var jobId = json.RootElement.TryGetProperty("job_id", out var id) ? id.GetString() : null;
        if (string.IsNullOrWhiteSpace(jobId)) throw new InvalidOperationException("LM Studio accepted the request but returned no download job ID.");
        while (true)
        {
            timeout.Token.ThrowIfCancellationRequested();
            await Task.Delay(TimeSpan.FromSeconds(1), timeout.Token);
            using var statusResponse = await _http.GetAsync(new Uri(root, "/api/v1/models/download/status/" + Uri.EscapeDataString(jobId)), timeout.Token);
            var statusBody = await BoundedHttpContent.ReadTextAsync(statusResponse.Content, timeout.Token, "LM Studio download status");
            if (!statusResponse.IsSuccessStatusCode)
                throw new InvalidOperationException($"Could not check LM Studio download progress ({(int)statusResponse.StatusCode}). {statusBody}");
            using var statusJson = JsonDocument.Parse(statusBody);
            var state = statusJson.RootElement.TryGetProperty("status", out var status) ? status.GetString() : null;
            var downloaded = statusJson.RootElement.TryGetProperty("downloaded_bytes", out var done) && done.TryGetInt64(out var bytes) ? bytes : 0;
            var total = statusJson.RootElement.TryGetProperty("total_size", out var sizeElement) && sizeElement.TryGetInt64(out var size) ? size : 0;
            onProgress?.Invoke(total > 0
                ? $"Hugging Face download · {downloaded / 1048576d:0.0} / {total / 1048576d:0.0} MB"
                : $"Hugging Face download · {state ?? "working"}");
            if (state is "completed" or "success") return;
            if (state is "failed" or "cancelled") throw new InvalidOperationException("LM Studio model download " + state + ". " + statusBody);
        }
    }

    public async Task PullOllamaAsync(string endpoint, string model, Action<string>? onProgress = null, CancellationToken cancellationToken = default)
    {
        var modelId = ValidateModelIdentifier(model, "Ollama model");
        if (!Uri.TryCreate(endpoint.Trim().TrimEnd('/'), UriKind.Absolute, out var endpointUri) || !endpointUri.IsLoopback)
            throw new InvalidOperationException("Ollama downloads are supported only from a local Ollama server.");
        var root = new UriBuilder(endpointUri) { Path = "", Query = "", Fragment = "" }.Uri;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromHours(2));
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(root, "/api/pull")) { Content = JsonContent.Create(new { name = modelId, stream = true }) };
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Ollama could not start the model pull ({(int)response.StatusCode}). Check the model name and local Ollama server.");
        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
        using var reader = new StreamReader(stream);
        var lineReader = BoundedHttpContent.CreateLineReader(reader);
        while (await lineReader.ReadLineAsync(timeout.Token) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            using var json = JsonDocument.Parse(line);
            if (json.RootElement.TryGetProperty("error", out var error)) throw new InvalidOperationException("Ollama: " + error.GetString());
            var status = json.RootElement.TryGetProperty("status", out var state) ? state.GetString() : "Downloading";
            long completed = json.RootElement.TryGetProperty("completed", out var c) && c.TryGetInt64(out var bytes) ? bytes : 0;
            long total = json.RootElement.TryGetProperty("total", out var t) && t.TryGetInt64(out var size) ? size : 0;
            onProgress?.Invoke(total > 0 ? $"{status} · {completed / 1048576d:0.0} / {total / 1048576d:0.0} MB" : status ?? "Downloading");
            if (string.Equals(status, "success", StringComparison.OrdinalIgnoreCase)) return;
        }
        throw new InvalidOperationException("Ollama closed the download stream before confirming completion.");
    }

    public async Task DeleteOllamaAsync(string endpoint, string model, CancellationToken cancellationToken = default)
    {
        var modelId = ValidateModelIdentifier(model, "Ollama model");
        if (!Uri.TryCreate(endpoint.Trim().TrimEnd('/'), UriKind.Absolute, out var endpointUri) || !endpointUri.IsLoopback)
            throw new InvalidOperationException("Ollama model deletion is supported only from a local Ollama server.");
        var root = new UriBuilder(endpointUri) { Path = "", Query = "", Fragment = "" }.Uri;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));
        using var response = await _http.SendAsync(new HttpRequestMessage(HttpMethod.Delete, new Uri(root, "/api/delete"))
        {
            Content = JsonContent.Create(new { name = modelId })
        }, timeout.Token);
        if (!response.IsSuccessStatusCode)
        {
            var body = await BoundedHttpContent.ReadTextAsync(response.Content, timeout.Token, "Ollama delete response");
            throw new InvalidOperationException($"Ollama could not delete {modelId} ({(int)response.StatusCode}). {body}");
        }
    }
}

public sealed record HubModel(string Id, long Downloads, int Likes, bool IsUncensored = false, string[]? Tags = null);
public sealed record ModelScopeGgufFile(string Path, long SizeBytes, string? Sha256);
public sealed record ModelScopeGgufPackage(string Repository, string Name, IReadOnlyList<ModelScopeGgufFile> Files, long Downloads, bool IsUncensored = false)
{
    public long SizeBytes => Files.Sum(file => Math.Max(0, file.SizeBytes));
}
public sealed record InstalledLocalModel(string Provider, string Model, string Endpoint, string? DisplayName = null, long? SizeBytes = null, bool? IsLoaded = null);
