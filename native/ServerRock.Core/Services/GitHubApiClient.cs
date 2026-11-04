using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using ServerRock.Core.Infrastructure;

namespace ServerRock.Core.Services;

public sealed record GitHubRepository(
    long Id,
    string FullName,
    string Name,
    bool IsPrivate,
    string DefaultBranch,
    string Description,
    DateTimeOffset? UpdatedAt,
    Uri CloneUrl);

public sealed record GitHubAccount(string Login, string DisplayName, Uri? AvatarUrl);

/// <summary>Reads the signed-in user's GitHub identity and repositories without following redirects.</summary>
public sealed class GitHubApiClient
{
    public static readonly Uri ApiBaseUri = new("https://api.github.com/");
    private const string ApiVersion = "2022-11-28";
    private readonly HttpClient _httpClient;

    public GitHubApiClient() : this(GitHubHttpClientFactory.Create()) { }

    public GitHubApiClient(HttpClient httpClient) => _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    public async Task<GitHubAccount> GetAccountAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync("user", accessToken, cancellationToken).ConfigureAwait(false);
        using var document = await ReadJsonAsync(response, cancellationToken).ConfigureAwait(false);
        var root = document.RootElement;
        var login = String(root, "login");
        if (login.Length is < 1 or > 39) throw new InvalidOperationException("GitHub did not return a valid account name.");
        var name = String(root, "name");
        if (name.Length > 200) name = name[..200];
        var avatar = AbsoluteHttpsUri(String(root, "avatar_url"));
        return new(login, name.Length == 0 ? login : name, avatar);
    }

    public async Task<IReadOnlyList<GitHubRepository>> ListRepositoriesAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var repositories = new List<GitHubRepository>();
        for (var page = 1; page <= 10; page++)
        {
            using var response = await SendAsync($"user/repos?sort=updated&per_page=100&page={page}&affiliation=owner%2Ccollaborator%2Corganization_member", accessToken, cancellationToken).ConfigureAwait(false);
            using var document = await ReadJsonAsync(response, cancellationToken).ConfigureAwait(false);
            if (document.RootElement.ValueKind != JsonValueKind.Array) throw new InvalidOperationException("GitHub returned an invalid repository list.");
            var pageCount = 0;
            foreach (var item in document.RootElement.EnumerateArray())
            {
                repositories.Add(ParseRepository(item));
                pageCount++;
            }
            if (pageCount < 100) break;
        }
        return repositories;
    }

    public async Task<GitHubRepository> ResolveRepositoryAsync(string accessToken, string reference, CancellationToken cancellationToken = default)
    {
        var fullName = ParseRepositoryReference(reference);
        var parts = fullName.Split('/');
        using var response = await SendAsync($"repos/{Uri.EscapeDataString(parts[0])}/{Uri.EscapeDataString(parts[1])}", accessToken, cancellationToken).ConfigureAwait(false);
        using var document = await ReadJsonAsync(response, cancellationToken).ConfigureAwait(false);
        var repository = ParseRepository(document.RootElement);
        if (!repository.FullName.Equals(fullName, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("GitHub returned a different repository than the one requested.");
        return repository;
    }

    public static string ParseRepositoryReference(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference) || reference.Length > 300)
            throw new ArgumentException("Enter a repository as owner/name or a GitHub repository URL.", nameof(reference));

        var value = reference.Trim();
        if (value.Contains("//", StringComparison.Ordinal) || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || value.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
                !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) || uri.UserInfo.Length > 0 || uri.Port != 443 ||
                uri.Query.Length > 0 || uri.Fragment.Length > 0)
                throw new ArgumentException("Only HTTPS GitHub repository URLs are supported.", nameof(reference));
            value = uri.AbsolutePath.Trim('/');
            if (value.EndsWith(".git", StringComparison.OrdinalIgnoreCase)) value = value[..^4];
        }

        var segments = value.Split('/');
        if (segments.Length != 2 || !IsRepositorySegment(segments[0]) || !IsRepositorySegment(segments[1]) || segments.Any(segment => segment is "." or ".."))
            throw new ArgumentException("Enter a repository as owner/name or a GitHub repository URL.", nameof(reference));
        return value;
    }

    private async Task<HttpResponseMessage> SendAsync(string relativePath, string accessToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken) || accessToken.Length > 16_384 || accessToken.Any(char.IsControl))
            throw new ArgumentException("A valid GitHub access token is required.", nameof(accessToken));
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(ApiBaseUri, relativePath));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        request.Headers.UserAgent.ParseAdd("ServerRock-AI/1.0");
        request.Headers.TryAddWithoutValidation("X-GitHub-Api-Version", ApiVersion);
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if ((int)response.StatusCode is >= 300 and < 400)
        {
            response.Dispose();
            throw new InvalidOperationException("GitHub redirected an API request; the redirect was blocked.");
        }
        return response;
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            try { return await BoundedHttpContent.ReadJsonAsync(response.Content, cancellationToken, "GitHub API response").ConfigureAwait(false); }
            catch (JsonException) { throw new InvalidOperationException("GitHub returned an invalid API response."); }
        }
        var body = await BoundedHttpContent.ReadTextAsync(response.Content, cancellationToken, "GitHub API error").ConfigureAwait(false);
        string message;
        try { using var document = JsonDocument.Parse(body); message = String(document.RootElement, "message"); }
        catch (JsonException) { message = ""; }
        if (message.Length > 240) message = message[..240] + "…";
        var suffix = message.Length == 0 ? "" : ": " + message;
        throw new InvalidOperationException($"GitHub API request failed (HTTP {(int)response.StatusCode}){suffix}");
    }

    private static GitHubRepository ParseRepository(JsonElement item)
    {
        var fullName = String(item, "full_name");
        var parsedName = ParseRepositoryReference(fullName);
        if (!parsedName.Equals(fullName, StringComparison.Ordinal)) throw new InvalidOperationException("GitHub returned an invalid repository name.");
        var name = String(item, "name");
        if (name.Length is < 1 or > 100 || name != fullName.Split('/')[1]) throw new InvalidOperationException("GitHub returned an inconsistent repository name.");
        if (!Uri.TryCreate(String(item, "clone_url"), UriKind.Absolute, out var cloneUrl) || cloneUrl.Scheme != Uri.UriSchemeHttps ||
            !cloneUrl.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) || cloneUrl.Port != 443 || cloneUrl.UserInfo.Length > 0 || cloneUrl.Query.Length > 0 || cloneUrl.Fragment.Length > 0)
            throw new InvalidOperationException("GitHub returned an unsafe clone URL.");
        var updatedText = String(item, "updated_at");
        DateTimeOffset? updatedAt = DateTimeOffset.TryParse(updatedText, out var parsedDate) ? parsedDate : null;
        var id = item.TryGetProperty("id", out var idValue) && idValue.TryGetInt64(out var parsedId) ? parsedId : 0;
        var description = String(item, "description");
        if (description.Length > 1000) description = description[..1000] + "…";
        var defaultBranch = String(item, "default_branch");
        if (defaultBranch.Length > 256 || defaultBranch.Any(char.IsControl))
            throw new InvalidOperationException("GitHub returned an invalid default branch name.");
        return new(id, fullName, name, item.TryGetProperty("private", out var privacy) && privacy.ValueKind == JsonValueKind.True,
            defaultBranch, description, updatedAt, cloneUrl);
    }

    private static string String(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? "" : "";

    private static Uri? AbsoluteHttpsUri(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.AbsoluteUri.Length <= 2048 &&
        uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Fragment) ? uri : null;
    private static bool IsRepositorySegment(string value) => value.Length is > 0 and <= 100 && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-');
}
