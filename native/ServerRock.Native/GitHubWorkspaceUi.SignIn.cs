// GitHub sign-in plumbing for the GitHub workspace UI (a MainPage partial):
// polls the device-flow authorization until the user approves it, and loads, refreshes,
// and saves the GitHub credential.
// Depends on: GitHubDeviceFlow, GitHubApiClient, GitHubCredentialStore (ServerRock.Core) and
// the fields declared in GitHubWorkspaceUi.cs.
// Called by: GitHubWorkspaceDialogAsync (GitHubWorkspaceUi.cs) and GitHubActionsClickAsync
// (GitHubWorkspaceUi.Changes.cs).

using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ServerRock.Core.Services;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private async Task PollGitHubAuthorizationAsync(GitHubDeviceAuthorization authorization,
        TextBlock connection, TextBlock signInStatus, Button connect, Button disconnect,
        TextBox code, Button verification, Button cancelSignIn,
        Func<string, Task> refreshRepositoriesAsync, CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                var delay = Math.Max(1, (int)Math.Ceiling((authorization.NextPollAt - DateTimeOffset.UtcNow).TotalSeconds));
                var remaining = Math.Max(0, (int)Math.Ceiling((authorization.ExpiresAt - DateTimeOffset.UtcNow).TotalMinutes));
                signInStatus.Text = $"Waiting for GitHub approval · code expires in about {remaining} minute(s).";
                await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);
                var result = await _githubDeviceFlow.PollAsync(authorization, cancellationToken);
                if (result.IsPending) continue;
                var credential = result.Credential ?? throw new InvalidOperationException("GitHub did not return a sign-in token.");
                await SaveGitHubCredentialAsync(credential, cancellationToken);
                _githubCredential = credential;
                _githubAccount = await _githubApiClient.GetAccountAsync(credential.AccessToken, cancellationToken);
                connection.Text = $"Connected as {_githubAccount.Login}";
                connect.Content = "Connected";
                connect.IsEnabled = false;
                disconnect.Visibility = Visibility.Visible;
                signInStatus.Text = "GitHub is connected. Choose a repository below to open it as a project.";
                try { await refreshRepositoriesAsync(credential.AccessToken); }
                catch (Exception exception) { signInStatus.Text = $"Connected, but the repository list could not be loaded: {exception.Message}"; }
                return;
            }
        }
        catch (OperationCanceledException)
        {
            signInStatus.Text = "GitHub sign-in canceled.";
        }
        catch (Exception exception)
        {
            connect.Content = "Connect GitHub";
            connect.IsEnabled = _githubCredentialStore.GetStatus().Available;
            signInStatus.Text = exception.Message;
        }
        finally
        {
            code.Text = string.Empty;
            code.Visibility = Visibility.Collapsed;
            verification.Visibility = Visibility.Collapsed;
            cancelSignIn.Visibility = Visibility.Collapsed;
            _githubAuthorizationCancellation?.Dispose();
            _githubAuthorizationCancellation = null;
            if (connect.Content?.ToString() != "Connected")
            {
                connect.IsEnabled = _githubCredentialStore.GetStatus().Available && !string.IsNullOrWhiteSpace(_settings.GitHubAppClientId);
                connect.Focus(FocusState.Programmatic);
            }
        }
    }

    private async Task<GitHubUserCredential?> LoadGitHubCredentialAsync(CancellationToken cancellationToken = default)
    {
        if (!_githubCredentialStore.GetStatus().Available) return null;
        var saved = await _githubCredentialStore.ReadAsync(cancellationToken);
        if (saved is null) { _githubCredential = null; return null; }
        GitHubUserCredential? credential;
        try { credential = JsonSerializer.Deserialize<GitHubUserCredential>(saved); }
        catch (JsonException exception) { throw new InvalidDataException("The saved GitHub sign-in is damaged. Remove it and sign in again.", exception); }
        if (credential is null || credential.AccessToken.Length is < 10 or > 16_384)
            throw new InvalidDataException("The saved GitHub sign-in is invalid. Remove it and sign in again.");
        if (credential.AccessTokenExpiresAt is { } expiresAt && expiresAt <= DateTimeOffset.UtcNow.AddMinutes(2))
        {
            if (string.IsNullOrWhiteSpace(credential.RefreshToken) || credential.RefreshTokenExpiresAt is { } refreshExpiry && refreshExpiry <= DateTimeOffset.UtcNow)
                throw new InvalidOperationException("The saved GitHub sign-in expired. Connect GitHub again.");
            credential = await _githubDeviceFlow.RefreshAsync(_settings.GitHubAppClientId, credential, cancellationToken);
            await SaveGitHubCredentialAsync(credential, cancellationToken);
        }
        _githubCredential = credential;
        return credential;
    }

    private async Task<GitHubUserCredential?> GetGitHubCredentialAsync(CancellationToken cancellationToken = default)
    {
        if (_githubCredential is { AccessTokenExpiresAt: null }) return _githubCredential;
        if (_githubCredential is { AccessTokenExpiresAt: { } expiry } && expiry > DateTimeOffset.UtcNow.AddMinutes(2)) return _githubCredential;
        return await LoadGitHubCredentialAsync(cancellationToken);
    }

    private async Task SaveGitHubCredentialAsync(GitHubUserCredential credential, CancellationToken cancellationToken = default) =>
        await _githubCredentialStore.WriteAsync(JsonSerializer.Serialize(credential), cancellationToken);

    private static void OpenExternalUri(Uri uri)
    {
        if (uri.Scheme != Uri.UriSchemeHttps || !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only GitHub’s verified HTTPS sign-in address can be opened.");
        Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }
}
