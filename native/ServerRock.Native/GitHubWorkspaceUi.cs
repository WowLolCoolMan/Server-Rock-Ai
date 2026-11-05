using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation.Peers;
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

    private async void GitHubClick(object sender, RoutedEventArgs e) { CloseCompactSidebar(); await RunUiActionAsync(GitHubWorkspaceDialogAsync); }

    private async Task GitHubWorkspaceDialogAsync()
    {
        var clientId = new TextBox
        {
            Header = "GitHub App Client ID",
            Text = _settings.GitHubAppClientId,
            PlaceholderText = "Iv1…",
            MaxLength = 80
        };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(clientId, "GitHub App client ID");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetHelpText(clientId, "Enter the public client ID for the GitHub App configured for device sign-in.");
        var storage = _githubCredentialStore.GetStatus();
        var storageMessage = new TextBlock { Text = storage.Message, FontSize = 11, TextWrapping = TextWrapping.Wrap, Foreground = Brush("#718187") };
        var connection = new TextBlock { Text = "Not connected", FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Foreground = Brush("#35494d") };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(connection, "GitHub connection state");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetLiveSetting(connection, AutomationLiveSetting.Polite);
        var code = new TextBox { IsReadOnly = true, FontSize = 24, TextAlignment = TextAlignment.Center, FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"), Visibility = Visibility.Collapsed };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(code, "GitHub device sign-in code");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetHelpText(code, "One-time code to enter on GitHub. It expires after a short time.");
        var verification = new Button { Content = "Open GitHub device sign-in", Visibility = Visibility.Collapsed };
        var signInStatus = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 11, Foreground = Brush("#718187") };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(signInStatus, "GitHub connection and repository status");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetLiveSetting(signInStatus, AutomationLiveSetting.Polite);
        var connect = new Button { Content = "Connect GitHub", IsEnabled = storage.Available, HorizontalAlignment = HorizontalAlignment.Left };
        var cancelSignIn = new Button { Content = "Cancel sign-in", Visibility = Visibility.Collapsed, HorizontalAlignment = HorizontalAlignment.Left };
        var disconnect = new Button { Content = "Remove saved connection", Visibility = Visibility.Collapsed, HorizontalAlignment = HorizontalAlignment.Left };
        var repositoryPicker = new ComboBox { PlaceholderText = "Choose a repository", HorizontalAlignment = HorizontalAlignment.Stretch, IsTextSearchEnabled = true };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(repositoryPicker, "GitHub repositories");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetHelpText(repositoryPicker, "Choose a repository available to this GitHub App. You can also enter its owner and name below.");
        var repositoryReference = new TextBox { Header = "Repository", PlaceholderText = "owner/name or https://github.com/owner/name" };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(repositoryReference, "Repository to clone");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetHelpText(repositoryReference, "Enter owner/name or an HTTPS GitHub repository URL.");
        var repositoryValidation = new TextBlock { Visibility = Visibility.Collapsed, TextWrapping = TextWrapping.Wrap, FontSize = 11, Foreground = Brush("#a64242") };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(repositoryValidation, "Repository format guidance");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetLiveSetting(repositoryValidation, AutomationLiveSetting.Polite);
        var refreshRepositories = new Button { Content = "Refresh repositories", HorizontalAlignment = HorizontalAlignment.Left };
        var parentPath = new TextBox
        {
            Header = "Clone into",
            Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Server Rock Projects"),
            PlaceholderText = "Choose a local parent folder"
        };
        var chooseParent = new Button { Content = "Choose folder…", HorizontalAlignment = HorizontalAlignment.Left };
        var cloneStatus = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 11, Foreground = Brush("#718187") };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(cloneStatus, "GitHub clone status");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetLiveSetting(cloneStatus, AutomationLiveSetting.Polite);
        var clone = new Button { Content = "Clone and open project", IsEnabled = false, HorizontalAlignment = HorizontalAlignment.Left };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(clone, "Clone repository and open as project");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetHelpText(clone, "Clone the selected or entered repository into the chosen local folder, then open it in Coding Agent.");
        var cancelClone = new Button { Content = "Cancel clone", Visibility = Visibility.Collapsed, HorizontalAlignment = HorizontalAlignment.Left };
        var signInPanel = new StackPanel { Spacing = 7, Children = { clientId, storageMessage, connection, connect, cancelSignIn, disconnect, code, verification, signInStatus } };
        var repositoryAccessHint = new TextBlock
        {
            Text = "Connect GitHub above to browse repositories or enter a repository to clone.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12,
            Foreground = Brush("#718187")
        };
        var repoPanel = new StackPanel
        {
            Spacing = 8,
            Visibility = Visibility.Collapsed,
            Children = { new TextBlock { Text = "Repositories", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = Brush("#35494d") }, repositoryPicker, repositoryReference, repositoryValidation, refreshRepositories, parentPath, chooseParent, cloneStatus, clone, cancelClone }
        };
        var content = new StackPanel { Spacing = 16, Children =
        {
            new TextBlock { Text = "Sign in to browse repositories you can access. Choose one to clone it into a local project. Git credentials stay in your OS keyring.", TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = Brush("#718187") },
            signInPanel,
            new Border { Height = 1, Background = Brush("#e5ece9"), Margin = new Thickness(0, 0, 0, 1) },
            repositoryAccessHint,
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

        var workspaceDialogOpen = false;
        var cloneInProgress = false;
        var destinationErrorVisible = false;
        CancellationTokenSource? cloneCancellation = null;
        void UpdateCloneAvailability(bool revealRepositoryError = false)
        {
            var candidate = repositoryReference.Text.Trim();
            if (candidate.Length == 0 && repositoryPicker.SelectedItem is string selectedRepository)
                candidate = selectedRepository;
            var validationWasVisible = repositoryValidation.Visibility == Visibility.Visible;
            var hasValidRepository = false;
            string? repositoryFullName = null;
            string? validationError = null;
            if (candidate.Length > 0)
            {
                try
                {
                    repositoryFullName = GitHubApiClient.ParseRepositoryReference(candidate);
                    hasValidRepository = true;
                }
                catch (ArgumentException exception) { validationError = exception.Message; }
            }
            if (validationError is not null && (revealRepositoryError || validationWasVisible))
            {
                repositoryValidation.Visibility = Visibility.Visible;
                repositoryValidation.Text = validationError;
            }
            else if (validationError is null)
            {
                repositoryValidation.Text = string.Empty;
                repositoryValidation.Visibility = Visibility.Collapsed;
            }

            var destinationReady = true;
            if (hasValidRepository && !string.IsNullOrWhiteSpace(parentPath.Text))
            {
                try
                {
                    var repositoryName = repositoryFullName!.Split('/')[1];
                    var destination = Path.Combine(Path.GetFullPath(parentPath.Text.Trim()), repositoryName);
                    var destinationIsNonEmpty = Directory.Exists(destination) && Directory.EnumerateFileSystemEntries(destination).Any();
                    if (File.Exists(destination) || destinationIsNonEmpty)
                    {
                        destinationReady = false;
                        destinationErrorVisible = true;
                        cloneStatus.Text = $"A file or non-empty folder already exists at {destination}. Choose another parent folder to retry.";
                    }
                    else if (destinationErrorVisible)
                    {
                        destinationErrorVisible = false;
                        cloneStatus.Text = string.Empty;
                    }
                }
                catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException or NotSupportedException)
                {
                    destinationReady = false;
                    destinationErrorVisible = true;
                    cloneStatus.Text = $"Cannot use this clone destination: {exception.Message}";
                }
            }
            else if (destinationErrorVisible)
            {
                destinationErrorVisible = false;
                cloneStatus.Text = string.Empty;
            }
            clone.IsEnabled = _githubCredential is not null && hasValidRepository && destinationReady && !string.IsNullOrWhiteSpace(parentPath.Text) && !cloneInProgress;
        }

        void FocusRepositoryEntry()
        {
            if (repositoryPicker.Items.Count > 0 && repositoryPicker.Focus(FocusState.Programmatic)) return;
            repositoryReference.Focus(FocusState.Programmatic);
        }

        dialog.Opened += (_, _) =>
        {
            workspaceDialogOpen = true;
            if (_githubCredential is not null && repoPanel.Visibility == Visibility.Visible) FocusRepositoryEntry();
            else clientId.Focus(FocusState.Programmatic);
        };
        dialog.Closed += (_, _) =>
        {
            workspaceDialogOpen = false;
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
            repositoryAccessHint.Visibility = Visibility.Collapsed;
            repoPanel.Visibility = Visibility.Visible;
            refreshRepositories.IsEnabled = false;
            signInStatus.Text = "Loading repositories from GitHub…";
            var repositoryRefreshFailed = false;
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
                repositoryRefreshFailed = true;
                _githubRepositories.Clear();
                repositoryPicker.Items.Clear();
                signInStatus.Text = $"Repository list could not be loaded: {exception.Message} You can still enter owner/name manually if the app has access.";
            }
            finally
            {
                refreshRepositories.IsEnabled = true;
                UpdateCloneAvailability();
                if (workspaceDialogOpen)
                {
                    if (repositoryRefreshFailed) repositoryReference.Focus(FocusState.Programmatic);
                    else FocusRepositoryEntry();
                }
            }
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
            signInStatus.Text = "Starting GitHub device sign-in…";
            try
            {
                var authorization = await _githubDeviceFlow.BeginAsync(configuredClientId);
                code.Text = authorization.UserCode;
                code.Visibility = Visibility.Visible;
                code.Focus(FocusState.Programmatic);
                verification.Visibility = Visibility.Visible;
                cancelSignIn.Visibility = Visibility.Visible;
                signInStatus.Text = "Enter the code in GitHub. This app will check for approval automatically.";
                _githubAuthorizationCancellation = new CancellationTokenSource();
                try { OpenExternalUri(authorization.VerificationUri); }
                catch (Exception exception) { signInStatus.Text = $"Enter the code at github.com/login/device. Browser could not open: {exception.Message}"; }
                _ = PollGitHubAuthorizationAsync(authorization, connection, signInStatus, connect, disconnect,
                    code, verification, cancelSignIn, RefreshRepositoryListAsync, cancellationToken: _githubAuthorizationCancellation.Token);
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
                repoPanel.Visibility = Visibility.Collapsed;
                repositoryAccessHint.Visibility = Visibility.Visible;
                connection.Text = "Saved sign-in removed from this device";
                signInStatus.Text = "To revoke this app’s GitHub access, also remove it from your GitHub Applications settings.";
                disconnect.Visibility = Visibility.Collapsed;
                connect.Content = "Connect GitHub";
                connect.IsEnabled = storage.Available && !string.IsNullOrWhiteSpace(clientId.Text);
                UpdateCloneAvailability();
            }
            catch (Exception exception) { signInStatus.Text = $"Could not remove the saved connection: {exception.Message}"; }
        };

        repositoryPicker.SelectionChanged += (_, _) =>
        {
            if (repositoryPicker.SelectedItem is string selectedRepository) repositoryReference.Text = selectedRepository;
            UpdateCloneAvailability();
        };
        repositoryReference.TextChanged += (_, _) => UpdateCloneAvailability(repositoryValidation.Visibility == Visibility.Visible);
        repositoryReference.LostFocus += (_, _) => UpdateCloneAvailability(revealRepositoryError: true);
        parentPath.TextChanged += (_, _) => UpdateCloneAvailability();
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
            cloneInProgress = true;
            UpdateCloneAvailability();
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
                cloneInProgress = false;
                cancelClone.Visibility = Visibility.Collapsed;
                chooseParent.IsEnabled = true;
                UpdateCloneAvailability();
                if (workspaceDialogOpen)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        if (clone.IsEnabled && clone.Focus(FocusState.Programmatic)) return;
                        FocusRepositoryEntry();
                    });
                }
            }
        };

        await dialog.ShowAsync();
        _githubAuthorizationCancellation?.Cancel();
    }

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

    private async void GitHubActionsClick(object sender, RoutedEventArgs e) => await GitHubActionsClickAsync(sender);

    private async Task GitHubActionsClickAsync(object sender)
    {
        try
        {
            var project = _projects.FirstOrDefault(item => item.Id == _activeProjectId);
            if (project is null || string.IsNullOrWhiteSpace(project.GitHubRepository) || project.FolderPaths.Count == 0) return;
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
        finally { RestoreNavigationDialogFocus(false, sender as Control); }
    }

    private async Task GitHubChangesDialogAsync(LocalProject project, string repositoryPath, GitWorkspaceStatus initialStatus, GitHubUserCredential credential)
    {
        var statusLine = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = ThemeBrush("TextFillColorSecondaryBrush", "#526a70") };
        var protectedNotice = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 11, Foreground = ThemeBrush("TextFillColorCautionBrush", "#8a5a22"), Visibility = Visibility.Collapsed };
        var changedFiles = new StackPanel { Spacing = 4, MaxHeight = 250, HorizontalAlignment = HorizontalAlignment.Stretch };
        var previewDiff = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
            MinHeight = 160,
            MaxHeight = 360,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Visibility = Visibility.Collapsed,
            PlaceholderText = "Select files, then preview their bounded unified diff."
        };
        ScrollViewer.SetHorizontalScrollBarVisibility(previewDiff, ScrollBarVisibility.Auto);
        ScrollViewer.SetVerticalScrollBarVisibility(previewDiff, ScrollBarVisibility.Auto);
        var commitMessage = new TextBox { Header = "Commit message", PlaceholderText = "Describe these changes", MaxLength = 256 };
        var actionStatus = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 11, Foreground = ThemeBrush("TextFillColorSecondaryBrush", "#718187") };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(actionStatus, "GitHub changes status");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetLiveSetting(actionStatus, AutomationLiveSetting.Polite);
        var preview = new Button { Content = "Preview selected diff", HorizontalAlignment = HorizontalAlignment.Left };
        var action = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        var cancelOperation = new Button { Content = "Cancel current operation", IsEnabled = false, HorizontalAlignment = HorizontalAlignment.Left };
        var changeChecks = new List<CheckBox>();
        var operationCancellation = new CancellationTokenSource();
        var dialogClosed = false;
        var latestStatus = initialStatus;
        var content = new StackPanel { Spacing = 10, Children = { statusLine, protectedNotice, changedFiles, preview, previewDiff, commitMessage, actionStatus, cancelOperation, action } };
        var dialog = new ContentDialog { Title = $"GitHub · {project.GitHubRepository}", Content = content, CloseButtonText = "Close", XamlRoot = XamlRoot };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(dialog, "GitHub changes review");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(protectedNotice, "Protected file notice");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetHelpText(protectedNotice, "Protected paths are hidden from diff previews and excluded from staging.");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(preview, "Preview selected Git diff");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetHelpText(preview, "Shows a bounded unified diff for the selected safe files.");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(cancelOperation, "Cancel current Git operation");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetHelpText(cancelOperation, "Stops the current preview, commit, status, or push operation. Local work is preserved.");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(action, "Commit selected files and push");
        dialog.Closing += (_, _) =>
        {
            dialogClosed = true;
            operationCancellation.Cancel();
        };

        IReadOnlyList<GitWorkspaceFileChange> SelectedChanges() => changeChecks
            .Where(check => check.IsEnabled && check.IsChecked == true && check.Tag is GitWorkspaceFileChange)
            .Select(check => (GitWorkspaceFileChange)check.Tag!)
            .ToArray();

        void FocusRetryTarget(bool preferAction)
        {
            try
            {
                if (preferAction && action.IsEnabled) action.Focus(FocusState.Programmatic);
                else if (preview.IsEnabled) preview.Focus(FocusState.Programmatic);
                else if (action.IsEnabled) action.Focus(FocusState.Programmatic);
            }
            catch (InvalidOperationException) { }
        }

        void FocusInitialReviewTarget()
        {
            var firstSelectableChange = changeChecks.FirstOrDefault(check => check.IsEnabled);
            if (firstSelectableChange?.Focus(FocusState.Programmatic) == true) return;
            FocusRetryTarget(preferAction: false);
        }

        void RestoreIdleState()
        {
            var hasSelectedSafeChanges = SelectedChanges().Count > 0;
            preview.IsEnabled = hasSelectedSafeChanges;
            action.IsEnabled = latestStatus.CommitsBehind == 0 && (hasSelectedSafeChanges || latestStatus.CommitsAhead > 0);
            cancelOperation.IsEnabled = false;
        }

        void Render(GitWorkspaceStatus status)
        {
            latestStatus = status;
            statusLine.Text = $"Branch {status.Branch} · {status.Changes.Count} changed file(s) · {status.CommitsAhead} commit(s) to push";
            changedFiles.Children.Clear();
            changeChecks.Clear();
            if (status.Changes.Count == 0)
                changedFiles.Children.Add(new TextBlock { Text = "No uncommitted file changes.", Foreground = ThemeBrush("TextFillColorSecondaryBrush", "#718187") });
            foreach (var change in status.Changes)
            {
                var protectedPath = GitHubWorkspaceService.FindUnsafeChangePaths([change]).Count > 0;
                var label = protectedPath
                    ? $"{change.Status,-3} [protected path redacted; not selectable]"
                    : $"{change.Status,-3} {change.Path}{(change.OriginalPath is null ? string.Empty : $" (from {change.OriginalPath})")}";
                var changeText = new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap };
                var check = new CheckBox
                {
                    Content = changeText,
                    IsChecked = !protectedPath,
                    IsEnabled = !protectedPath,
                    Tag = change,
                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                    HorizontalContentAlignment = HorizontalAlignment.Stretch
                };
                Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(check, label);
                Microsoft.UI.Xaml.Automation.AutomationProperties.SetHelpText(check,
                    protectedPath ? "Protected path hidden and excluded from staging." : "Safe path selected for review; uncheck to leave it uncommitted.");
                changeChecks.Add(check);
                changedFiles.Children.Add(check);
            }
            var protectedCount = status.Changes.Count(change => GitHubWorkspaceService.FindUnsafeChangePaths([change]).Count > 0);
            protectedNotice.Visibility = protectedCount > 0 ? Visibility.Visible : Visibility.Collapsed;
            protectedNotice.Text = protectedCount > 0
                ? $"{protectedCount} protected path(s) are hidden from previews and excluded from staging. Review them locally before committing."
                : string.Empty;
            commitMessage.Visibility = status.Changes.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            var safeChanges = status.Changes.Count(change => GitHubWorkspaceService.FindUnsafeChangePaths([change]).Count == 0);
            action.Content = safeChanges > 0 ? "Commit selected files and push" : $"Push {status.CommitsAhead} local commit(s)";
            action.IsEnabled = safeChanges > 0 || status.CommitsAhead > 0;
            preview.IsEnabled = safeChanges > 0;
            if (status.CommitsBehind > 0) actionStatus.Text = $"GitHub has {status.CommitsBehind} newer commit(s). Fetch and review remote changes before pushing.";
            else actionStatus.Text = safeChanges > 0
                ? "Selected safe files are staged intentionally, then committed locally and pushed. Protected paths remain uncommitted and their contents are redacted."
                : "Only protected paths remain uncommitted, or there are no changes. Review them locally before staging.";
            if (status.CommitsBehind > 0) action.IsEnabled = false;
            if (status.Changes.Any(change =>
                    change.Path.Split('/').Any(segment => segment is ".github") ||
                    change.OriginalPath?.Split('/').Any(segment => segment is ".github") == true) && status.Changes.Count > 0)
                actionStatus.Text += " Changes under .github may require the GitHub App’s Workflows permission.";
        }
        Render(initialStatus);
        dialog.Opened += (_, _) => FocusInitialReviewTarget();

        cancelOperation.Click += (_, _) =>
        {
            if (operationCancellation.IsCancellationRequested) return;
            actionStatus.Text = "Cancelling the Git operation…";
            cancelOperation.IsEnabled = false;
            operationCancellation.Cancel();
        };

        preview.Click += async (_, _) =>
        {
            var selected = SelectedChanges();
            if (selected.Count == 0)
            {
                previewDiff.Text = "Select at least one safe file to preview.";
                previewDiff.Visibility = Visibility.Visible;
                return;
            }
            operationCancellation.Dispose();
            operationCancellation = new CancellationTokenSource();
            var operationToken = operationCancellation.Token;
            preview.IsEnabled = false;
            action.IsEnabled = false;
            cancelOperation.IsEnabled = true;
            actionStatus.Text = "Preparing a bounded diff preview…";
            try
            {
                var result = await _githubWorkspaceService.GetDiffPreviewAsync(repositoryPath, selected, operationToken);
                previewDiff.Text = result.Text;
                previewDiff.Visibility = Visibility.Visible;
                actionStatus.Text = result.RedactedFiles > 0
                    ? $"Previewed {result.IncludedFiles} file(s); {result.RedactedFiles} protected path(s) were redacted."
                    : $"Previewed {result.IncludedFiles} file(s){(result.IsTruncated ? "; output was bounded and truncated." : ".")}";
            }
            catch (OperationCanceledException) when (operationToken.IsCancellationRequested && !dialogClosed)
            {
                actionStatus.Text = "Diff preview canceled. You can retry it.";
            }
            catch (Exception exception) when (!dialogClosed) { actionStatus.Text = exception.Message; }
            finally
            {
                cancelOperation.IsEnabled = false;
                if (!dialogClosed)
                {
                    preview.IsEnabled = SelectedChanges().Count > 0;
                    RestoreIdleState();
                    FocusRetryTarget(preferAction: false);
                }
            }
        };

        action.Click += async (_, _) =>
        {
            operationCancellation.Dispose();
            operationCancellation = new CancellationTokenSource();
            var operationToken = operationCancellation.Token;
            action.IsEnabled = false;
            preview.IsEnabled = false;
            cancelOperation.IsEnabled = true;
            try
            {
                var currentCredential = await GetGitHubCredentialAsync(operationToken) ?? throw new InvalidOperationException("Connect GitHub again to continue.");
                var currentStatus = await _githubWorkspaceService.GetStatusAsync(repositoryPath, operationToken);
                if (currentStatus.Changes.Count > 0)
                {
                    var selected = SelectedChanges();
                    if (selected.Count == 0) throw new InvalidOperationException("Select at least one safe file to commit.");
                    if (string.IsNullOrWhiteSpace(commitMessage.Text)) throw new InvalidOperationException("Enter a commit message first.");
                    actionStatus.Text = "Creating local commit…";
                    _ = await _githubWorkspaceService.CommitSelectedAsync(repositoryPath, selected, commitMessage.Text, currentCredential.AccessToken, operationToken);
                }
                currentStatus = await _githubWorkspaceService.GetStatusAsync(repositoryPath, operationToken);
                Render(currentStatus);
                if (currentStatus.CommitsBehind > 0) throw new InvalidOperationException("The local commit is safe, but GitHub has newer commits. Fetch and review them before pushing.");
                if (currentStatus.CommitsAhead > 0)
                {
                    actionStatus.Text = "Pushing to GitHub…";
                    await _githubWorkspaceService.PushAsync(repositoryPath, currentCredential.AccessToken, operationToken);
                }
                RuntimeStatus.Text = $"GitHub project {project.GitHubRepository} is up to date.";
                dialog.Hide();
            }
            catch (OperationCanceledException) when (operationToken.IsCancellationRequested && !dialogClosed)
            {
                actionStatus.Text = "Git operation canceled. Your local changes and any existing local commit were preserved; you can retry.";
                RestoreIdleState();
                preview.IsEnabled = SelectedChanges().Count > 0;
                FocusRetryTarget(preferAction: true);
            }
            catch (Exception exception) when (!dialogClosed)
            {
                try { Render(await _githubWorkspaceService.GetStatusAsync(repositoryPath)); }
                catch { RestoreIdleState(); }
                actionStatus.Text = exception.Message;
                FocusRetryTarget(preferAction: true);
            }
            finally
            {
                cancelOperation.IsEnabled = false;
                if (!dialogClosed && !operationToken.IsCancellationRequested)
                {
                    RestoreIdleState();
                }
            }
        };

        try { await dialog.ShowAsync(); }
        finally
        {
            dialogClosed = true;
            operationCancellation.Cancel();
            operationCancellation.Dispose();
        }
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

    private static Microsoft.UI.Xaml.Media.Brush ThemeBrush(string resourceKey, string fallback)
    {
        try
        {
            if (Application.Current?.Resources[resourceKey] is Microsoft.UI.Xaml.Media.Brush brush) return brush;
        }
        catch (KeyNotFoundException) { }
        return Brush(fallback);
    }

    private static void OpenExternalUri(Uri uri)
    {
        if (uri.Scheme != Uri.UriSchemeHttps || !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only GitHub’s verified HTTPS sign-in address can be opened.");
        Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }
}
