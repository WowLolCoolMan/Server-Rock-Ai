// Switching between the two workspaces (Chat = "ChatBot", Coding Agent = "Coding Agent")
// and the page-wide keyboard shortcuts (Ctrl+L focus composer, Ctrl+Shift+E / Esc for the
// compact sidebar, Esc to close slash suggestions).
// SwitchModeAsync saves the current chat, clears the conversation, and updates every
// mode-dependent control; UpdateModeControls/UpdateWorkspaceBar refresh labels and visibility.
// Depends on: page state in MainPage.xaml.cs (_mode, _messages, _projects, …), SaveCurrentSession,
// RenderRecent, RenderProjectFiles, and the project dialogs for Coding Agent without folders.
// Called by: the Chat/Coding buttons and KeyDown in MainPage.xaml, the /mode slash command,
// and MainPage code that calls UpdateModeControls()/UpdateWorkspaceBar() after state changes.

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    // Not wired to any element in MainPage.xaml at the moment (kept as-is; see the refactor notes).
    private void NavigateClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string destination }) return;
        PageTitle.Text = destination switch { "images" => "Images", "activity" => "Activity", _ => _project ?? "New chat" };
        RuntimeStatus.Text = destination switch { "images" => "Image generation is not connected yet", "activity" => "Recent conversations are being migrated", _ => $"⌖　{_settings.Endpoint}" };
    }

    private async void ChatModeClick(object sender, RoutedEventArgs e) => await SwitchModeFromSidebarAsync("ChatBot");
    private async void CodingModeClick(object sender, RoutedEventArgs e) => await SwitchModeFromSidebarAsync("Coding Agent");

    private async Task SwitchModeFromSidebarAsync(string mode)
    {
        var returnToComposer = _compactNavigation && Sidebar.Visibility == Visibility.Visible;
        CloseCompactSidebar();
        await RunUiActionAsync(() => SwitchModeAsync(mode));
        if (returnToComposer)
            DispatcherQueue.TryEnqueue(() => PromptBox.Focus(FocusState.Programmatic));
    }

    private void AppKeyDown(object sender, KeyRoutedEventArgs e)
    {
        var controlPressed = Microsoft.UI.Input.InputKeyboardSource
            .GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        var shiftPressed = Microsoft.UI.Input.InputKeyboardSource
            .GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

        if (controlPressed && shiftPressed && e.Key == Windows.System.VirtualKey.E && _compactNavigation)
        {
            e.Handled = true;
            if (Sidebar.Visibility == Visibility.Visible) CloseCompactSidebar();
            else ToggleSidebarClick(sender, e);
            return;
        }

        if (e.Key == Windows.System.VirtualKey.Escape && _compactNavigation && Sidebar.Visibility == Visibility.Visible)
        {
            e.Handled = true;
            CloseCompactSidebar();
            return;
        }

        if (controlPressed && e.Key == Windows.System.VirtualKey.L)
        {
            e.Handled = true;
            PromptBox.Focus(FocusState.Programmatic);
            return;
        }

        if (e.Key == Windows.System.VirtualKey.Escape && CommandHintPanel.Visibility == Visibility.Visible)
        {
            e.Handled = true;
            CommandHintPanel.Visibility = Visibility.Collapsed;
            _selectedSlashSuggestion = -1;
            PromptBox.Focus(FocusState.Programmatic);
        }
    }

    private async Task SwitchModeAsync(string selected)
    {
        if (_mode == selected) return;
        if (_requestCancellation is not null)
        {
            RuntimeStatus.Text = "Wait for the response to finish before switching workspaces.";
            return;
        }
        if (!await ConfirmTemporaryDiscardAsync($"switch to {selected}")) return;
        SaveCurrentSession();
        ClearPendingAttachments();
        _mode = selected;
        _activeSessionId = null;
        _activeChatFolder = null;
        _editingMessageIndex = null;
        _includeProjectFilesInChat = false;
        _messages.Clear();
        ConversationMessages.Children.Clear();
        WelcomePanel.Visibility = Visibility.Visible;
        ConversationScroll.Visibility = Visibility.Collapsed;
        PageTitle.Text = selected == "Coding Agent" ? "Coding Agent" : _project ?? "New chat";
        PromptBox.PlaceholderText = selected == "Coding Agent"
            ? "Describe what you want to change in this project…"
            : "Ask anything… use / for commands";
        UpdateModeControls();
        RenderProjectFiles();
        UpdateWebSearchButton();
        UpdateTemporaryControls();
        RenderRecent();
        if (_mode == "Coding Agent" && (_projects.FirstOrDefault(item => item.Id == _activeProjectId)?.FolderPaths.Count ?? 0) == 0)
        {
            if (_projects.FirstOrDefault(item => item.Id == _activeProjectId) is null) await NewProjectClickAsync();
            else await ProjectFoldersClickAsync();
        }
    }

    private void UpdateModeControls()
    {
        var chatMode = _mode == "ChatBot";
        // Segmented switch: the active workspace is a raised white segment with ink text;
        // the inactive one is flat muted text (it used to look like the raised one).
        UiTheme.ApplySegmentState(ChatModeButton, chatMode);
        UiTheme.ApplySegmentState(CodingModeButton, !chatMode);
        CodingWorkspaceBar.Visibility = chatMode ? Visibility.Collapsed : Visibility.Visible;
        CodingNavigationSection.Visibility = chatMode ? Visibility.Collapsed : Visibility.Visible;
        NewProjectButton.Visibility = chatMode ? Visibility.Collapsed : Visibility.Visible;
        GitHubButton.Visibility = Visibility.Visible;
        NewConversationLabel.Text = chatMode ? "New chat" : "New coding task";
        WebSearchButton.Visibility = chatMode ? Visibility.Visible : Visibility.Collapsed;
        WelcomeTitle.Text = chatMode ? "What can I help you with?" : "Work on a project together";
        WelcomeSubtitle.Text = chatMode
            ? "Chat with a model running on your computer, or turn on web search when you need current sources."
            : "Choose a project folder. The Coding Agent can inspect its files and propose changes for your review.";
        SuggestionOne.Content = chatMode ? "Help me write" : "Explain this codebase";
        SuggestionOne.Tag = chatMode ? "Help me write" : "Explain this codebase";
        SuggestionTwo.Content = chatMode ? "Plan a project" : "Find a bug";
        SuggestionTwo.Tag = chatMode ? "Help me plan a project" : "Find a bug in this project";
        SuggestionThree.Content = chatMode ? "Search the web" : "Add a feature";
        SuggestionThree.Tag = chatMode ? "Search the web for " : "Add a feature to this project";
        ProcessingLabel.Text = DescribeDataRoute();
        ConnectionSetupButton.Visibility = chatMode && !_providerConnected ? Visibility.Visible : Visibility.Collapsed;
        UpdateProjectFilesControls();
        UpdateWorkspaceBar();
        UpdateBranchControls();
    }

    private void UpdateWorkspaceBar()
    {
        var project = _projects.FirstOrDefault(item => item.Id == _activeProjectId);
        ProjectFoldersButton.Visibility = Visibility.Visible;
        GitHubActionsButton.Visibility = !string.IsNullOrWhiteSpace(project?.GitHubRepository) ? Visibility.Visible : Visibility.Collapsed;
        TerminalButton.Visibility = project?.FolderPaths.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        UndoHistoryButton.Content = !_undoHistoryAvailable ? "Undo history unavailable"
            : _workspaceUndoEntries.Count == 0 ? "Undo history" : $"Undo history · {_workspaceUndoEntries.Count}";
        ProjectFoldersButton.Content = project is null ? "Choose project" : "Manage folders";
        WorkspaceProjectName.Text = project?.Name ?? "No project selected";
        // The folder-count badge only appears with a project (it used to float "Coding workspace"
        // beside the buttons), and Undo history only when there is a project or a recorded change.
        WorkspaceFolderBadge.Visibility = project is null ? Visibility.Collapsed : Visibility.Visible;
        WorkspaceFolderCount.Text = project is null
            ? string.Empty
            : project.FolderPaths.Count == 1 ? "1 folder" : $"{project.FolderPaths.Count} folders";
        UndoHistoryButton.Visibility = project is not null || _workspaceUndoEntries.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        WorkspaceProjectPath.Text = project is null
            ? _projects.Count == 0 ? "Create a project to give the Coding Agent a local workspace." : "Choose a project from the sidebar or create a new one."
            : project.FolderPaths.Count == 0 ? "No local folders connected · add a folder before asking the agent to inspect files."
            : project.FolderPaths.Count == 1 ? project.FolderPaths[0] : $"{project.FolderPaths[0]}  + {project.FolderPaths.Count - 1} more folder(s)";
    }
}
