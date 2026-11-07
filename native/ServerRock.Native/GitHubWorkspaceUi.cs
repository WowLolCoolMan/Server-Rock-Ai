// GitHub workspace dialog (a MainPage partial, despite the file name): GitHub App
// client ID, device-flow sign-in, repository list or manual owner/name entry, and cloning
// a repository into a new local project. Also holds the GitHub fields and the small
// Brush/ThemeBrush color helpers used by both GitHub dialogs.
// Related files: GitHubWorkspaceUi.SignIn.cs (polling + credential storage) and
// GitHubWorkspaceUi.Changes.cs (commit/push review).
// Depends on: GitHubDeviceFlow, GitHubApiClient, GitHubCredentialStore,
// GitHubWorkspaceService (ServerRock.Core).
// Called by: the sidebar GitHub button, Settings, and GitHubActionsClickAsync when not signed in.

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
            _githubAuthorizationCancellation?.Dispose();
            var authorizationCancellation = new CancellationTokenSource();
            _githubAuthorizationCancellation = authorizationCancellation;
            cancelSignIn.Visibility = Visibility.Visible;
            try
            {
                var authorization = await _githubDeviceFlow.BeginAsync(configuredClientId, authorizationCancellation.Token);
                code.Text = authorization.UserCode;
                code.Visibility = Visibility.Visible;
                code.Focus(FocusState.Programmatic);
                verification.Visibility = Visibility.Visible;
                signInStatus.Text = "Enter the code in GitHub. This app will check for approval automatically.";
                try { OpenExternalUri(authorization.VerificationUri); }
                catch (Exception exception) { signInStatus.Text = $"Enter the code at github.com/login/device. Browser could not open: {exception.Message}"; }
                _ = PollGitHubAuthorizationAsync(authorization, connection, signInStatus, connect, disconnect,
                    code, verification, cancelSignIn, RefreshRepositoryListAsync, cancellationToken: authorizationCancellation.Token);
            }
            catch (Exception exception)
            {
                var wasCanceled = exception is OperationCanceledException || authorizationCancellation.IsCancellationRequested;
                authorizationCancellation.Dispose();
                if (ReferenceEquals(_githubAuthorizationCancellation, authorizationCancellation)) _githubAuthorizationCancellation = null;
                code.Text = string.Empty;
                code.Visibility = Visibility.Collapsed;
                verification.Visibility = Visibility.Collapsed;
                cancelSignIn.Visibility = Visibility.Collapsed;
                connect.Content = "Connect GitHub";
                connect.IsEnabled = storage.Available && !string.IsNullOrWhiteSpace(clientId.Text);
                signInStatus.Text = wasCanceled ? "GitHub sign-in canceled." : exception.Message;
                if (workspaceDialogOpen) connect.Focus(FocusState.Programmatic);
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

    // Parses "#RRGGBB" into an opaque brush. ThemeBrush prefers an app/theme resource and
    // falls back to that color when the resource is missing.
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
}
