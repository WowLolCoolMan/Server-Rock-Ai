// GitHub commit/push review for the active project (a MainPage partial).
// GitHubActionsClick opens a dialog that lists changed files, previews the diff, and lets
// the user commit selected files and push. Secret-looking files can't be selected.
// Depends on: GitHubWorkspaceService (git status/diff/commit/push in ServerRock.Core),
// GetGitHubCredentialAsync (GitHubWorkspaceUi.SignIn.cs).
// Called by: the GitHub button in the Coding Agent workspace bar (MainPage.xaml).

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using ServerRock.Core.Models;
using ServerRock.Core.Services;

namespace ServerRock.Native;

public sealed partial class MainPage
{
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
        var statusLine = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = UiTheme.Brush("MutedInkBrush") };
        var protectedNotice = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 11, Foreground = UiTheme.Brush("WarningInkBrush"), Visibility = Visibility.Collapsed };
        var changedFiles = new StackPanel { Spacing = 4, MaxHeight = 250, HorizontalAlignment = HorizontalAlignment.Stretch };
        var previewDiff = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            FontFamily = UiTheme.CodeFont,
            MinHeight = 160,
            MaxHeight = 360,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Visibility = Visibility.Collapsed,
            PlaceholderText = "Select files, then preview their bounded unified diff."
        };
        ScrollViewer.SetHorizontalScrollBarVisibility(previewDiff, ScrollBarVisibility.Auto);
        ScrollViewer.SetVerticalScrollBarVisibility(previewDiff, ScrollBarVisibility.Auto);
        var commitMessage = new TextBox { Header = "Commit message", PlaceholderText = "Describe these changes", MaxLength = 256 };
        var actionStatus = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 11, Foreground = UiTheme.Brush("MutedInkBrush") };
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
                changedFiles.Children.Add(new TextBlock { Text = "No uncommitted file changes.", Foreground = UiTheme.Brush("MutedInkBrush") });
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
                    FontFamily = UiTheme.CodeFont,
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
}
