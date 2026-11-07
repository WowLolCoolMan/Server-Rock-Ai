// Image Studio back end: queues a user-supplied ComfyUI workflow on a loopback-only
// endpoint, polls for the result, and returns image URLs.
// Depends on: HttpClient, BoundedHttpContent. Used by: MainPage Image Studio dialog.

using System.Net.Http.Json;
using System.Text.Json;
using ServerRock.Core.Infrastructure;

namespace ServerRock.Core.Services;

public sealed record ComfyGeneratedImage(Uri Url, string FileName);

/// <summary>Queues an explicitly configured workflow on a local ComfyUI server.</summary>
public sealed class ComfyImageProvider(HttpClient? httpClient = null)
{
    private const int MaxWorkflowCharacters = 1_000_000;
    private const int MaxPromptCharacters = 8_000;
    private readonly HttpClient _http = httpClient ?? new HttpClient { Timeout = Timeout.InfiniteTimeSpan };

    public async Task<IReadOnlyList<ComfyGeneratedImage>> GenerateAsync(string endpoint, string workflowTemplate, string prompt, Action<string>? progress = null, CancellationToken cancellationToken = default)
    {
        var root = ValidateEndpoint(endpoint);
        if (string.IsNullOrWhiteSpace(prompt) || prompt.Length > MaxPromptCharacters)
            throw new ArgumentException($"Enter an image prompt up to {MaxPromptCharacters:N0} characters.", nameof(prompt));
        if (string.IsNullOrWhiteSpace(workflowTemplate) || workflowTemplate.Length > MaxWorkflowCharacters || !workflowTemplate.Contains("{{prompt}}", StringComparison.Ordinal))
            throw new ArgumentException("Add a ComfyUI API workflow up to 1 MB and put {{prompt}} inside its prompt text value.", nameof(workflowTemplate));

        var escapedPrompt = JsonSerializer.Serialize(prompt.Trim())[1..^1];
        JsonElement workflow;
        try { using var document = JsonDocument.Parse(workflowTemplate.Replace("{{prompt}}", escapedPrompt, StringComparison.Ordinal)); workflow = document.RootElement.Clone(); }
        catch (JsonException error) { throw new ArgumentException("The ComfyUI workflow is not valid JSON after the prompt was inserted.", nameof(workflowTemplate), error); }

        progress?.Invoke("Queueing image with local ComfyUI…");
        using var queueResponse = await _http.PostAsJsonAsync(new Uri(root, "prompt"), new { prompt = workflow, client_id = Guid.NewGuid().ToString("N") }, cancellationToken).ConfigureAwait(false);
        // Once ComfyUI has accepted the POST, finish reading its bounded queue
        // receipt even if the caller cancels. We need the returned prompt ID to
        // remove exactly that already-queued job rather than leaving it running.
        using var queueJson = await BoundedHttpContent.ReadJsonAsync(queueResponse.Content, CancellationToken.None, "ComfyUI queue response").ConfigureAwait(false);
        if (!queueResponse.IsSuccessStatusCode) throw new InvalidOperationException($"ComfyUI could not queue the image ({(int)queueResponse.StatusCode}).");
        if (!queueJson.RootElement.TryGetProperty("prompt_id", out var id) || string.IsNullOrWhiteSpace(id.GetString()) || id.GetString()!.Length > 200)
            throw new InvalidOperationException("ComfyUI accepted the request but returned no usable prompt ID.");
        var promptId = id.GetString()!;

        try
        {
            for (var attempt = 0; attempt < 180; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Invoke($"Generating image locally… {attempt + 1}s");
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
                using var historyResponse = await _http.GetAsync(new Uri(root, "history/" + Uri.EscapeDataString(promptId)), cancellationToken).ConfigureAwait(false);
                if (!historyResponse.IsSuccessStatusCode) continue;
                using var history = await BoundedHttpContent.ReadJsonAsync(historyResponse.Content, cancellationToken, "ComfyUI history").ConfigureAwait(false);
                if (!history.RootElement.TryGetProperty(promptId, out var job) || !job.TryGetProperty("outputs", out var outputs)) continue;
                var images = new List<ComfyGeneratedImage>();
                foreach (var output in outputs.EnumerateObject())
                {
                    if (!output.Value.TryGetProperty("images", out var values) || values.ValueKind != JsonValueKind.Array) continue;
                    foreach (var image in values.EnumerateArray())
                    {
                        var file = image.TryGetProperty("filename", out var name) ? name.GetString() : null;
                        if (string.IsNullOrWhiteSpace(file) || file.Length > 512 || file.Contains('/') || file.Contains('\\')) continue;
                        var subfolder = image.TryGetProperty("subfolder", out var sub) ? sub.GetString() ?? "" : "";
                        if (subfolder.Contains("..", StringComparison.Ordinal) || subfolder.Contains('\\')) continue;
                        var type = image.TryGetProperty("type", out var kind) ? kind.GetString() ?? "output" : "output";
                        var query = $"view?filename={Uri.EscapeDataString(file)}&subfolder={Uri.EscapeDataString(subfolder)}&type={Uri.EscapeDataString(type)}";
                        images.Add(new ComfyGeneratedImage(new Uri(root, query), file));
                    }
                }
                if (images.Count > 0) return images;
                throw new InvalidOperationException("ComfyUI finished without returning an image output.");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await CancelQueuedPromptAsync(root, promptId).ConfigureAwait(false);
            throw;
        }
        throw new TimeoutException("Timed out waiting for ComfyUI after 3 minutes.");
    }

    private async Task CancelQueuedPromptAsync(Uri root, string promptId)
    {
        // This app submits the legacy /prompt workflow API, which returns a prompt_id.
        // Delete only that pending prompt and interrupt only that matching execution.
        // Both calls are best effort: local cancellation must remain responsive even
        // when an older or unavailable ComfyUI instance does not support one route.
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            using var remove = await _http.PostAsJsonAsync(new Uri(root, "queue"), new { delete = new[] { promptId } }, timeout.Token).ConfigureAwait(false);
            using var interrupt = await _http.PostAsJsonAsync(new Uri(root, "interrupt"), new { prompt_id = promptId }, timeout.Token).ConfigureAwait(false);
        }
        catch (HttpRequestException) { }
        catch (OperationCanceledException) { }
    }

    private static Uri ValidateEndpoint(string endpoint)
    {
        if (!Uri.TryCreate(endpoint.Trim().TrimEnd('/') + "/", UriKind.Absolute, out var root) || root.Scheme is not ("http" or "https") || !root.IsLoopback)
            throw new InvalidOperationException("ComfyUI must use a loopback HTTP or HTTPS endpoint.");
        return root;
    }
}
