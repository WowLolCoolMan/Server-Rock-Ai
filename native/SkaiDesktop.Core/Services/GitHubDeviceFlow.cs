// GitHub App OAuth device flow (begin, poll with back-off, refresh) and the
// redirect-free GitHub HttpClient factory. Holds no credentials itself.
// Depends on: BoundedHttpContent. Used by: MainPage.GitHub.cs sign-in.

using System.Net;
using System.Text.Json;
using SkaiDesktop.Core.Infrastructure;

namespace SkaiDesktop.Core.Services;

public static class GitHubHttpClientFactory
{
    /// <summary>Creates an HTTP client for GitHub that cannot follow redirects or retain cookies.</summary>
    public static HttpClient Create() => new(new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
    });
}

public sealed class GitHubDeviceAuthorization
{
    internal GitHubDeviceAuthorization(string clientId, string deviceCode, string userCode, Uri verificationUri,
        DateTimeOffset expiresAt, int intervalSeconds, DateTimeOffset nextPollAt)
    {
        ClientId = clientId;
        DeviceCode = deviceCode;
        UserCode = userCode;
        VerificationUri = verificationUri;
        ExpiresAt = expiresAt;
        IntervalSeconds = intervalSeconds;
        NextPollAt = nextPollAt;
    }

    internal string ClientId { get; }
    internal string DeviceCode { get; }
    public string UserCode { get; }
    public Uri VerificationUri { get; }
    public DateTimeOffset ExpiresAt { get; }
    public int IntervalSeconds { get; internal set; }
    public DateTimeOffset NextPollAt { get; internal set; }
}

public sealed record GitHubUserCredential(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset? AccessTokenExpiresAt,
    DateTimeOffset? RefreshTokenExpiresAt);

public sealed record GitHubDevicePollResult(bool IsPending, GitHubUserCredential? Credential = null, int? RetryAfterSeconds = null);

/// <summary>Implements GitHub App OAuth device authorization without storing credentials.</summary>
public sealed class GitHubDeviceFlow
{
    public static readonly Uri DeviceCodeEndpoint = new("https://github.com/login/device/code");
    public static readonly Uri AccessTokenEndpoint = new("https://github.com/login/oauth/access_token");
    private static readonly Uri VerificationEndpoint = new("https://github.com/login/device");
    private readonly HttpClient _httpClient;
    private readonly Func<DateTimeOffset> _utcNow;

    public GitHubDeviceFlow() : this(GitHubHttpClientFactory.Create()) { }

    public GitHubDeviceFlow(HttpClient httpClient, Func<DateTimeOffset>? utcNow = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public async Task<GitHubDeviceAuthorization> BeginAsync(string clientId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 80 ||
            !clientId.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-'))
            throw new ArgumentException("Enter a valid GitHub App client ID.", nameof(clientId));

        using var request = new HttpRequestMessage(HttpMethod.Post, DeviceCodeEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["scope"] = "offline_access"
            })
        };
        request.Headers.Accept.ParseAdd("application/json");
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        var body = await ReadJsonAsync(response, "GitHub device authorization could not start.", cancellationToken).ConfigureAwait(false);
        var deviceCode = ReadString(body, "device_code");
        var userCode = ReadString(body, "user_code");
        if (deviceCode.Length is < 20 or > 500 || userCode.Length is < 3 or > 32)
            throw new InvalidOperationException(ReadError(body, "GitHub returned an invalid device authorization response."));

        var verification = ReadString(body, "verification_uri");
        if (!Uri.TryCreate(verification, UriKind.Absolute, out var verificationUri) ||
            verificationUri.Scheme != Uri.UriSchemeHttps || !verificationUri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ||
            verificationUri.AbsolutePath != "/login/device")
            throw new InvalidOperationException("GitHub returned an unexpected verification address. The sign-in was stopped.");

        var now = _utcNow();
        var expiresIn = ReadBoundedInteger(body, "expires_in", 900, 60, 1800);
        var interval = ReadBoundedInteger(body, "interval", 5, 5, 60);
        return new GitHubDeviceAuthorization(clientId, deviceCode, userCode, verificationUri,
            now.AddSeconds(expiresIn), interval, now.AddSeconds(interval));
    }

    public async Task<GitHubDevicePollResult> PollAsync(GitHubDeviceAuthorization authorization, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        var now = _utcNow();
        if (now >= authorization.ExpiresAt)
            throw new InvalidOperationException("The GitHub sign-in code expired. Start sign-in again.");
        if (now < authorization.NextPollAt)
            return new(true, RetryAfterSeconds: Math.Max(1, (int)Math.Ceiling((authorization.NextPollAt - now).TotalSeconds)));

        using var request = new HttpRequestMessage(HttpMethod.Post, AccessTokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = authorization.ClientId,
                ["device_code"] = authorization.DeviceCode,
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code"
            })
        };
        request.Headers.Accept.ParseAdd("application/json");
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        var body = await ReadJsonAsync(response, "GitHub authorization failed.", cancellationToken).ConfigureAwait(false);
        var error = ReadString(body, "error");
        if (error.Equals("authorization_pending", StringComparison.Ordinal))
        {
            authorization.NextPollAt = _utcNow().AddSeconds(authorization.IntervalSeconds);
            return new(true, RetryAfterSeconds: authorization.IntervalSeconds);
        }
        if (error.Equals("slow_down", StringComparison.Ordinal))
        {
            authorization.IntervalSeconds = Math.Min(authorization.IntervalSeconds + 5, 120);
            authorization.NextPollAt = _utcNow().AddSeconds(authorization.IntervalSeconds);
            return new(true, RetryAfterSeconds: authorization.IntervalSeconds);
        }
        if (error.Length > 0)
        {
            var message = error switch
            {
                "access_denied" => "GitHub sign-in was denied. You can try again when ready.",
                "expired_token" or "token_expired" => "The GitHub sign-in code expired. Start sign-in again.",
                "incorrect_client_credentials" => "GitHub rejected this app client ID. Check that device flow is enabled for its GitHub App.",
                _ => ReadError(body, "GitHub authorization failed.")
            };
            throw new InvalidOperationException(message);
        }

        var accessToken = ReadString(body, "access_token");
        if (accessToken.Length is < 10 or > 16_384)
            throw new InvalidOperationException(ReadError(body, "GitHub did not return an access token."));
        var refreshToken = ReadString(body, "refresh_token");
        var accessExpiresIn = ReadBoundedInteger(body, "expires_in", 0, 0, 31_536_000);
        var refreshExpiresIn = ReadBoundedInteger(body, "refresh_token_expires_in", 0, 0, 31_536_000);
        return new(false, new GitHubUserCredential(accessToken, refreshToken,
            accessExpiresIn > 0 ? _utcNow().AddSeconds(accessExpiresIn) : null,
            refreshExpiresIn > 0 ? _utcNow().AddSeconds(refreshExpiresIn) : null));
    }

    public async Task<GitHubUserCredential> RefreshAsync(string clientId, GitHubUserCredential credential, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(credential);
        if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 80 ||
            !clientId.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-') ||
            credential.RefreshToken.Length is < 10 or > 16_384 || credential.RefreshToken.Any(char.IsControl))
            throw new InvalidOperationException("The saved GitHub sign-in cannot be refreshed. Sign in again.");

        using var request = new HttpRequestMessage(HttpMethod.Post, AccessTokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = credential.RefreshToken
            })
        };
        request.Headers.Accept.ParseAdd("application/json");
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        var body = await ReadJsonAsync(response, "GitHub sign-in expired. Sign in again.", cancellationToken).ConfigureAwait(false);
        var error = ReadString(body, "error");
        if (error.Length > 0)
            throw new InvalidOperationException(error is "bad_refresh_token" or "expired_token"
                ? "GitHub sign-in expired. Sign in again."
                : ReadError(body, "GitHub sign-in could not be refreshed. Sign in again."));

        var accessToken = ReadString(body, "access_token");
        var refreshToken = ReadString(body, "refresh_token");
        if (accessToken.Length is < 10 or > 16_384 || accessToken.Any(char.IsControl) || refreshToken.Length is > 16_384 || refreshToken.Any(char.IsControl))
            throw new InvalidOperationException("GitHub returned an invalid refreshed sign-in. Sign in again.");
        var accessExpiresIn = ReadBoundedInteger(body, "expires_in", 0, 0, 31_536_000);
        var refreshExpiresIn = ReadBoundedInteger(body, "refresh_token_expires_in", 0, 0, 31_536_000);
        return new(accessToken, refreshToken.Length == 0 ? credential.RefreshToken : refreshToken,
            accessExpiresIn > 0 ? _utcNow().AddSeconds(accessExpiresIn) : null,
            refreshExpiresIn > 0 ? _utcNow().AddSeconds(refreshExpiresIn) : credential.RefreshTokenExpiresAt);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response, string fallback, CancellationToken cancellationToken)
    {
        if ((int)response.StatusCode is >= 300 and < 400)
            throw new InvalidOperationException("GitHub redirected an authorization request; the redirect was blocked.");
        JsonDocument document;
        try { document = await BoundedHttpContent.ReadJsonAsync(response.Content, cancellationToken, "GitHub authorization response").ConfigureAwait(false); }
        catch (Exception exception) when (exception is JsonException or InvalidDataException) { throw new InvalidOperationException(fallback); }
        using (document)
        {
            var root = document.RootElement.Clone();
            if (!response.IsSuccessStatusCode && ReadString(root, "error").Length == 0)
                throw new InvalidOperationException($"{fallback} (HTTP {(int)response.StatusCode})");
            return root;
        }
    }

    private static string ReadString(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? "" : "";

    private static int ReadBoundedInteger(JsonElement element, string property, int fallback, int minimum, int maximum)
    {
        if (!element.TryGetProperty(property, out var value) || !value.TryGetInt32(out var parsed)) return fallback;
        return Math.Clamp(parsed, minimum, maximum);
    }

    private static string ReadError(JsonElement element, string fallback) =>
        ReadString(element, "error_description") is { Length: > 0 and <= 300 } description ? description : fallback;
}
