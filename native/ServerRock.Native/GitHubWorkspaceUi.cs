using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using ServerRock.Core.Models;
using ServerRock.Core.Services;
using ServerRock.Core.Infrastructure;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private readonly GitHubDeviceFlow _githubDeviceFlow = new();
    private readonly GitHubApiClient _githubApiClient = new();
    private readonly GitHubCredentialStore _githubCredentialStore = new();
    private readonly GitHubWorkspaceService _githubWorkspaceService = new();
    private readonly List<GitHubRepository> _githubRepositories = [];
    private GitHubUserCredential? _githubCredential;
    private GitHubAccount? _githubAccount;
    private CancellationTokenSource? _githubAuthorizationCancellation;

    private async void GitHubClick(object sender, RoutedEventArgs e) => await RunUiActionAsync(GitHubWorkspaceDialogAsync);

    private async Task GitHubWorkspaceDialogAsync()
    {
        var clientId = new TextBox
        {
            Header = "GitHub App Client ID",
            Text = _settings.GitHubAppClientId,
            PlaceholderText = "Iv1…",
            MaxLength = 80
        };
        var storage = _githubCredentialStore.GetStatus();
        var storageMessage = new TextBlock { Text = storage.Message, FontSize = 11, TextWrapping = TextWrapping.Wrap, Foreground = Brush("#718187") };
        var connection = new TextBlock { Text = "Not connected", FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Foreground = Brush("#35494d") };
        var code = new TextBox { IsReadOnly = true, FontSize = 24, TextAlignment = TextAlignment.Center, FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"), Visibility = Visibility.Collapsed };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(code, "GitHub device sign-in code");
        var verification = new Button { Content = "Open GitHub device sign-in", Visibility = Visibility.Collapsed };
        var signInStatus = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 11, Foreground = Brush("#718187") };
        var connect = new Button { Content = "Connect GitHub", IsEnabled = storage.Available, HorizontalAlignment = HorizontalAlignment.Left };
        var cancelSignIn = new Button { Content = "Cancel sign-in", Visibility = Visibility.Collapsed, HorizontalAlignment = HorizontalAlignment.Left };
        var disconnect = new Button { Content = "Remove saved connection", Visibility = Visibility.Collapsed, HorizontalAlignment = HorizontalAlignment.Left };
        var repositoryPicker = new ComboBox { PlaceholderText = "Choose a repository", HorizontalAlignment = HorizontalAlignment.Stretch, IsTextSearchEnabled = true };
        var repositoryReference = new TextBox { Header = "Repository", PlaceholderText = "owner/name or https://github.com/owner/name" };
        var refreshRepositories = new Button { Content = "Refresh repositories", HorizontalAlignment = HorizontalAlignment.Left };
        var parentPath = new TextBox
        {
            Header = "Clone into",
            Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Server Rock Projects"),
            PlaceholderText = "Choose a local parent folder"
        };
        var chooseParent = new Button { Content = "Choose folder…", HorizontalAlignment = HorizontalAlignment.Left };
        var cloneStatus = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 11, Foreground = Brush("#718187") };
        var clone = new Button { Content = "Clone and open project", IsEnabled = false, HorizontalAlignment = HorizontalAlignment.Left };
        var cancelClone = new Button { Content = "Cancel clone", Visibility = Visibility.Collapsed, HorizontalAlignment = HorizontalAlignment.Left };
        var signInPanel = new StackPanel { Spacing = 7, Children = { clientId, storageMessage, connection, connect, cancelSignIn, disconnect, code, verification, signInStatus } };
        var repoPanel = new StackPanel { Spacing = 8, Children = { new TextBlock { Text = "Repositories", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = Brush("#35494d") }, repositoryPicker, repositoryReference, refreshRepositories, parentPath, chooseParent, cloneStatus, clone, cancelClone } };
        var content = new StackPanel { Spacing = 16, Children =
        {
            new TextBlock { Text = "Sign in to browse repositories you can access. Choose one to clone it into a local project. Git credentials stay in your OS keyring.", TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = Brush("#718187") },
            signInPanel,
            new Border { Height = 1, Background = Brush("#e5ece9"), Margin = new Thickness(0, 0, 0, 1) },
            repoPanel,
            new TextBlock { Text = "The app’s GitHub App must have Device Flow enabled and Contents read/write permission for private clone, commit, and push.", TextWrapping = TextWrapping.Wrap, FontSize = 10, Foreground = Brush("#829093") }
        } };
        var dialog = new ContentDialog
        {
            Title = "GitHub workspace",
            Content = new ScrollViewer { Content = content, MaxHeight = 620, VerticalScrollBarVisibility = ScrollBarVisibility.Auto },
            PrimaryButtonText = "Done",
            XamlRoot = XamlRoot
        };

        CancellationTokenSource? cloneCancellation = null;
        dialog.Closed += (_, _) =>
        {
            _githubAuthorizationCancellation?.Cancel();
            cloneCancellation?.Cancel();
        };
        verification.Click += (_, _) =>
        {
            try { OpenExternalUri(new Uri("https://github.com/login/device")); }
            catch (Exception exception) { signInStatus.Text = $"Could not open the browser. Visit https://github.com/login/device manually. {exception.Message}"; }
        };
        cancelSignIn.Click += (_, _) => _githubAuthorizationCancellation?.Cancel();
        clientId.TextChanged += (_, _) => connect.IsEnabled = storage.Available && !string.IsNullOrWhiteSpace(clientId.Text);

        async Task RefreshRepositoryListAsync(string token)
        {
            refreshRepositories.IsEnabled = false;
            try
            {
                var repositories = await _githubApiClient.ListRepositoriesAsync(token);
                _githubRepositories.Clear();
                _githubRepositories.AddRange(repositories);
                repositoryPicker.Items.Clear();
                foreach (var repository in repositories) repositoryPicker.Items.Add(repository.FullName);
                signInStatus.Text = repositories.Count == 0
                    ? "No repositories are available to this GitHub App. Check its installation and repository permissions."
                    : $"Loaded {repositories.Count} repository/repositories.";
            }
            catch (Exception exception)
            {
                signInStatus.Text = $"Repository list could not be loaded: {exception.Message} You can still enter owner/name manually if the app has access.";
            }
            finally { refreshRepositories.IsEnabled = true; }
        }

        async Task ConnectAccountAsync(GitHubUserCredential credential)
        {
            _githubCredential = credential;
            _githubAccount = await _githubApiClient.GetAccountAsync(credential.AccessToken);
            connection.Text = $"Connected as {_githubAccount.Login}";
            disconnect.Visibility = Visibility.Visible;
            connect.Content = "Connected";
            connect.IsEnabled = false;
            await RefreshRepositoryListAsync(credential.AccessToken);
        }

        try
        {
            var saved = await LoadGitHubCredentialAsync();
            if (saved is not null)
            {
                try { await ConnectAccountAsync(saved); }
                catch (Exception exception) { connection.Text = $"Saved sign-in needs attention: {exception.Message}"; }
            }
            else
            {
                var available = _githubCredentialStore.GetStatus().Available;
                connection.Text = available ? "Not connected" : "Secure credential storage unavailable";
            }
        }
        catch (Exception exception) { connection.Text = $"Could not read the saved sign-in: {exception.Message}"; }

        connect.Click += async (_, _) =>
        {
            var configuredClientId = clientId.Text.Trim();
            if (configuredClientId.Length == 0) { signInStatus.Text = "Enter the GitHub App Client ID first."; return; }
            _settings.GitHubAppClientId = configuredClientId;
            SaveSettings();
            connect.IsEnabled = false;
            try
            {
                var authorization = await _githubDeviceFlow.BeginAsync(configuredClientId);
                code.Text = authorization.UserCode;
                code.Visibility = Visibility.Visible;
                verification.Visibility = Visibility.Visible;
                cancelSignIn.Visibility = Visibility.Visible;
                signInStatus.Text = "Enter the code in GitHub. This app will check for approval automatically.";
                _githubAuthorizationCancellation = new CancellationTokenSource();
                try { OpenExternalUri(authorization.VerificationUri); }
                catch (Exception exception) { signInStatus.Text = $"Enter the code at github.com/login/device. Browser could not open: {exception.Message}"; }
                _ = PollGitHubAuthorizationAsync(authorization, connection, signInStatus, connect, disconnect,
                    RefreshRepositoryListAsync, cancellationToken: _githubAuthorizationCancellation.Token);
            }
            catch (Exception exception)
            {
                connect.IsEnabled = _githubCredential is null && storage.Available;
                signInStatus.Text = exception.Message;
            }
        };

        disconnect.Click += async (_, _) =>
        {
            try
            {
                await _githubCredentialStore.DeleteAsync();
                _githubCredential = null;
                _githubAccount = null;
                _githubRepositories.Clear();
                repositoryPicker.Items.Clear();
                connection.Text = "Saved sign-in removed from this device";
                signInStatus.Text = "To revoke this app’s GitHub access, also remove it from your GitHub Applications settings.";
                disconnect.Visibility = Visibility.Collapsed;
                connect.Content = "Connect GitHub";
                connect.IsEnabled = storage.Available && !string.IsNullOrWhiteSpace(clientId.Text);
                clone.IsEnabled = false;
            }
            catch (Exception exception) { signInStatus.Text = $"Could not remove the saved connection: {exception.Message}"; }
        };

        repositoryPicker.SelectionChanged += (_, _) =>
        {
            if (repositoryPicker.SelectedItem is string selectedRepository) repositoryReference.Text = selectedRepository;
        };
        refreshRepositories.Click += async (_, _) =>
        {
            try
            {
                var credential = await GetGitHubCredentialAsync();
                if (credential is null) { signInStatus.Text = "Connect GitHub to load your repositories."; return; }
                await RefreshRepositoryListAsync(credential.AccessToken);
            }
            catch (Exception exception) { signInStatus.Text = exception.Message; }
        };
        chooseParent.Click += async (_, _) =>
        {
            var folder = await PickWorkspaceFolderAsync();
            if (folder is not null) parentPath.Text = folder;
        };
        cancelClone.Click += (_, _) => cloneCancellation?.Cancel();
        clone.Click += async (_, _) =>
        {
            var credential = await GetGitHubCredentialAsync();
            if (credential is null) { cloneStatus.Text = "Connect GitHub before cloning a repository."; return; }
            var reference = repositoryReference.Text.Trim();
            if (reference.Length == 0 && repositoryPicker.SelectedItem is string selectedRepository) reference = selectedRepository;
            if (reference.Length == 0) { cloneStatus.Text = "Choose a repository or enter owner/name."; return; }
            if (string.IsNullOrWhiteSpace(parentPath.Text)) { cloneStatus.Text = "Choose a local parent folder for the clone."; return; }

            clone.IsEnabled = false;
            chooseParent.IsEnabled = false;
            cancelClone.Visibility = Visibility.Visible;
            cloneCancellation = new CancellationTokenSource();
            try
            {
                GitHubRepository repository;
                var normalizedReference = GitHubApiClient.ParseRepositoryReference(reference);
                repository = _githubRepositories.FirstOrDefault(item => item.FullName.Equals(normalizedReference, StringComparison.OrdinalIgnoreCase))
                    ?? await _githubApiClient.ResolveRepositoryAsync(credential.AccessToken, reference, cloneCancellation.Token);
                var destination = Path.Combine(Path.GetFullPath(parentPath.Text.Trim()), repository.Name);
                cloneStatus.Text = $"Cloning {repository.FullName}…";
                var clonedPath = await _githubWorkspaceService.CloneAsync(repository, destination, credential.AccessToken, cloneCancellation.Token);
                var project = new LocalProject
                {
                    Name = repository.FullName,
                    GitHubRepository = repository.FullName,
                    FolderPaths = [clonedPath],
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                _projects.Add(project);
                if (!SaveProjects())
                {
                    _projects.Remove(project);
                    return;
                }
                RenderProjects();
                cloneStatus.Text = $"Cloned {repository.FullName}. Opening it as a Coding Agent project…";
                dialog.Hide();
                await Task.Yield();
                ProjectClick(new Button { Tag = project.Id }, new RoutedEventArgs());
            }
            catch (OperationCanceledException) { cloneStatus.Text = "Clone canceled. Any existing files in the destination were left untouched."; }
            catch (Exception exception) { cloneStatus.Text = exception.Message; }
            finally
            {
                cloneCancellation?.Dispose();
                cloneCancellation = null;
                cancelClone.Visibility = Visibility.Collapsed;
                chooseParent.IsEnabled = true;
                clone.IsEnabled = _githubCredential is not null;
            }
        };

        await dialog.ShowAsync();
        _githubAuthorizationCancellation?.Cancel();
    }

    private async Task PollGitHubAuthorizationAsync(GitHubDeviceAuthorization authorization,
        TextBlock connection, TextBlock signInStatus, Button connect, Button disconnect,
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
            _githubAuthorizationCancellation?.Dispose();
            _githubAuthorizationCancellation = null;
            if (connect.Content?.ToString() != "Connected") connect.IsEnabled = _githubCredentialStore.GetStatus().Available && !string.IsNullOrWhiteSpace(_settings.GitHubAppClientId);
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

    private async void GitHubActionsClick(object sender, RoutedEventArgs e)
    {
        var project = _projects.FirstOrDefault(item => item.Id == _activeProjectId);
        if (project is null || string.IsNullOrWhiteSpace(project.GitHubRepository) || project.FolderPaths.Count == 0) return;
        try
        {
            var credential = await GetGitHubCredentialAsync();
            if (credential is null)
            {
                RuntimeStatus.Text = "Connect GitHub to commit and push this project.";
                await GitHubWorkspaceDialogAsync();
                return;
            }
            var repositoryPath = project.FolderPaths[0];
            var status = await _githubWorkspaceService.GetStatusAsync(repositoryPath);
            await GitHubChangesDialogAsync(project, repositoryPath, status, credential);
        }
        catch (Exception exception) { RuntimeStatus.Text = $"GitHub workspace is unavailable: {exception.Message}"; }
    }

    private async Task GitHubChangesDialogAsync(LocalProject project, string repositoryPath, GitWorkspaceStatus initialStatus, GitHubUserCredential credential)
    {
        var statusLine = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = Brush("#526a70") };
        var changedFiles = new TextBox { IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.NoWrap, FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"), MinHeight = 120, MaxHeight = 250 };
        var commitMessage = new TextBox { Header = "Commit message", PlaceholderText = "Describe these changes", MaxLength = 256 };
        var actionStatus = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 11, Foreground = Brush("#718187") };
        var action = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        var content = new StackPanel { Spacing = 10, Children = { statusLine, changedFiles, commitMessage, actionStatus, action } };
        var dialog = new ContentDialog { Title = $"GitHub · {project.GitHubRepository}", Content = content, CloseButtonText = "Close", XamlRoot = XamlRoot };

        void Render(GitWorkspaceStatus status)
        {
            statusLine.Text = $"Branch {status.Branch} · {status.Changes.Count} changed file(s) · {status.CommitsAhead} commit(s) to push";
            changedFiles.Text = status.Changes.Count == 0
                ? "No uncommitted file changes."
                : string.Join(Environment.NewLine, status.Changes.Select(change =>
                    $"{change.Status,-3} {change.Path}{(change.OriginalPath is null ? string.Empty : $" (from {change.OriginalPath})")}"));
            changedFiles.Visibility = status.Changes.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            commitMessage.Visibility = status.Changes.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            action.Content = status.Changes.Count > 0 ? "Commit all listed files and push" : $"Push {status.CommitsAhead} local commit(s)";
            action.IsEnabled = status.Changes.Count > 0 || status.CommitsAhead > 0;
            if (status.CommitsBehind > 0) actionStatus.Text = $"GitHub has {status.CommitsBehind} newer commit(s). Fetch and review remote changes before pushing.";
            else actionStatus.Text = "The action stages every listed change, creates a local commit, then pushes it to GitHub. A failed push keeps the local commit for retry.";
            if (status.CommitsBehind > 0) action.IsEnabled = false;
            if (status.Changes.Any(change =>
                    change.Path.Split('/').Any(segment => segment is ".github") ||
                    change.OriginalPath?.Split('/').Any(segment => segment is ".github") == true) && status.Changes.Count > 0)
                actionStatus.Text += " Changes under .github may require the GitHub App’s Workflows permission.";
        }
        Render(initialStatus);

        action.Click += async (_, _) =>
        {
            action.IsEnabled = false;
            try
            {
                var currentCredential = await GetGitHubCredentialAsync() ?? throw new InvalidOperationException("Connect GitHub again to continue.");
                if (initialStatus.Changes.Count > 0)
                {
                    if (string.IsNullOrWhiteSpace(commitMessage.Text)) throw new InvalidOperationException("Enter a commit message first.");
                    actionStatus.Text = "Creating local commit…";
                    _ = await _githubWorkspaceService.CommitAllAsync(repositoryPath, commitMessage.Text, currentCredential.AccessToken);
                }
                var currentStatus = await _githubWorkspaceService.GetStatusAsync(repositoryPath);
                Render(currentStatus);
                if (currentStatus.CommitsBehind > 0) throw new InvalidOperationException("The local commit is safe, but GitHub has newer commits. Fetch and review them before pushing.");
                if (currentStatus.CommitsAhead > 0)
                {
                    actionStatus.Text = "Pushing to GitHub…";
                    await _githubWorkspaceService.PushAsync(repositoryPath, currentCredential.AccessToken);
                }
                RuntimeStatus.Text = $"GitHub project {project.GitHubRepository} is up to date.";
                dialog.Hide();
            }
            catch (Exception exception)
            {
                actionStatus.Text = exception.Message;
                if (initialStatus.Changes.Count > 0)
                {
                    try { Render(await _githubWorkspaceService.GetStatusAsync(repositoryPath)); }
                    catch { }
                }
                action.IsEnabled = initialStatus.CommitsBehind == 0;
            }
        };

        await dialog.ShowAsync();
    }

    private static Microsoft.UI.Xaml.Media.SolidColorBrush Brush(string color)
    {
        var parsed = Microsoft.UI.ColorHelper.FromArgb(
            255,
            Convert.ToByte(color.Substring(1, 2), 16),
            Convert.ToByte(color.Substring(3, 2), 16),
            Convert.ToByte(color.Substring(5, 2), 16));
        return new Microsoft.UI.Xaml.Media.SolidColorBrush(parsed);
    }

    private static void OpenExternalUri(Uri uri)
    {
        if (uri.Scheme != Uri.UriSchemeHttps || !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only GitHub’s verified HTTPS sign-in address can be opened.");
        Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }
}
