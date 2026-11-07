// Saved conversations in the sidebar ("Recents"): starting a new chat, rendering the list
// (grouped by folder, with an Archived section), opening a saved chat, the per-chat actions menu
// (rename, archive/restore, move to folder, fork, delete), and saving the current conversation.
// SaveCurrentSession is the single place a live chat is written into _sessions/conversations.json;
// temporary chats are never saved.
// Depends on: ConversationStore (ServerRock.Core), page state in MainPage.xaml.cs (_sessions,
// _messages, _activeSessionId, …), chat-folder helpers in MainPage.ChatFolders.cs, and the
// branch helpers.
// Called by: New chat button and Recents rows in MainPage.xaml, slash commands (/new, /rename,
// /delete), mode switching, sending a message, and anything that changes saved history.

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using ServerRock.Core.Models;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private void NewChatClick(object sender, RoutedEventArgs e)
    {
        CloseCompactSidebar();
        if (_requestCancellation is not null)
        {
            CancelRequest();
            RuntimeStatus.Text = "Stopping response… start a new chat when it finishes.";
            return;
        }
        SaveCurrentSession();
        ClearPendingAttachments();
        _activeSessionId = null;
        _activeChatFolder = null;
        _editingMessageIndex = null;
        _includeProjectFilesInChat = false;
        _messages.Clear();
        ConversationMessages.Children.Clear();
        WelcomePanel.Visibility = Visibility.Visible;
        ConversationScroll.Visibility = Visibility.Collapsed;
        PageTitle.Text = _activeProjectName ?? "New chat";
        ProjectBadge.Visibility = _activeProjectName is null ? Visibility.Collapsed : Visibility.Visible;
        UpdateWorkspaceBar();
        UpdateProjectFilesControls();
        UpdateWebSearchButton();
        UpdateTemporaryControls();
    }

    private void RenderRecent()
    {
        RecentList.Children.Clear();
        _recentActionButtons.Clear();
        _chatFolderActionButtons.Clear();
        _chatFolderToggleButtons.Clear();
        _archivedChatsToggleButton = null;
        _settings.ChatFolders ??= [];
        var modeSessions = _sessions.Where(item => item.Mode == _mode).OrderByDescending(item => item.UpdatedAt).ToArray();
        var visibleSessions = modeSessions.Where(item => !item.Archived).ToArray();
        var archivedSessions = modeSessions.Where(item => item.Archived).ToArray();
        ChatFolderList.Children.Clear();
        _settings.ChatFolders = _settings.ChatFolders
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        foreach (var folder in _settings.ChatFolders)
        {
            var folderSessions = visibleSessions.Where(item => string.Equals(item.FolderName, folder, StringComparison.OrdinalIgnoreCase)).ToArray();
            var collapsed = _collapsedChatFolders.Contains(folder);
            var open = new Button
            {
                Content = $"{(collapsed ? "▸" : "▾")}　▱　{folder}　{folderSessions.Length}",
                Tag = folder,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = new SolidColorBrush(ColorHelper.FromArgb(255, 231, 245, 247)),
                Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 45, 78, 86)),
                Padding = new Thickness(8, 7, 8, 7),
                CornerRadius = new CornerRadius(8)
            };
            AutomationProperties.SetName(open, $"{(collapsed ? "Expand" : "Collapse")} chat folder {folder}, {folderSessions.Length} chats");
            _chatFolderToggleButtons[folder] = open;
            open.Click += (_, _) =>
            {
                if (!_collapsedChatFolders.Add(folder)) _collapsedChatFolders.Remove(folder);
                RenderRecent();
                DispatcherQueue.TryEnqueue(() => FocusChatFolderToggleOrFallback(folder));
            };
            var more = new Button
            {
                Content = "⋯", Tag = folder, Width = 30, Height = 32, Padding = new Thickness(0),
                Background = new SolidColorBrush(ColorHelper.FromArgb(255, 231, 245, 247)),
                Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 82, 111, 119)),
                CornerRadius = new CornerRadius(8)
            };
            AutomationProperties.SetName(more, $"Actions for chat folder {folder}");
            more.Click += ChatFolderActionsClick;
            _chatFolderActionButtons[folder] = more;
            var header = new Grid { ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition { Width = GridLength.Auto } } };
            header.Children.Add(open);
            Grid.SetColumn(more, 1);
            header.Children.Add(more);
            ChatFolderList.Children.Add(header);
            if (!collapsed)
            {
                var contents = new StackPanel { Margin = new Thickness(12, 2, 0, 5), Spacing = 3 };
                foreach (var session in folderSessions) contents.Children.Add(CreateRecentRow(session));
                if (folderSessions.Length == 0)
                    contents.Children.Add(new TextBlock { Text = "No chats in this folder yet.", FontSize = 10, Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 113, 133, 140)), Margin = new Thickness(8, 4, 8, 4) });
                ChatFolderList.Children.Add(contents);
            }
        }
        if (_settings.ChatFolders.Count == 0)
            ChatFolderList.Children.Add(new TextBlock { Text = "No folders yet. Use the folder button above to create one.", TextWrapping = TextWrapping.Wrap, FontSize = UiTheme.FontSize("CaptionFontSize"), Foreground = UiTheme.Brush("MutedInkBrush"), Margin = new Thickness(8, 2, 8, 5) });
        var unfiled = visibleSessions.Where(item => string.IsNullOrWhiteSpace(item.FolderName) || !_settings.ChatFolders.Contains(item.FolderName, StringComparer.OrdinalIgnoreCase)).ToArray();
        foreach (var session in unfiled) RecentList.Children.Add(CreateRecentRow(session));
        if (unfiled.Length == 0)
            RecentList.Children.Add(new TextBlock
            {
                Text = !_historyAvailable ? "Conversation history is unavailable." : visibleSessions.Length == 0
                    ? _mode == WorkspaceModes.CodingAgent ? "Coding tasks will appear here." : "Your chats will appear here."
                    : "All chats are in folders.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 113, 133, 140)),
                FontSize = 11,
                Margin = new Thickness(8, 4, 8, 4)
            });

        if (archivedSessions.Length > 0)
        {
            var archivedHeader = new Button
            {
                Content = $"{(_archivedChatsExpanded ? "▾" : "▸")}　Archived　{archivedSessions.Length}",
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = new SolidColorBrush(ColorHelper.FromArgb(255, 231, 245, 247)),
                Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 82, 111, 119)),
                Padding = new Thickness(8, 7, 8, 7),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 10, 0, 0)
            };
            AutomationProperties.SetName(archivedHeader, $"{(_archivedChatsExpanded ? "Collapse" : "Expand")} archived chats, {archivedSessions.Length} chats");
            _archivedChatsToggleButton = archivedHeader;
            archivedHeader.Click += (_, _) =>
            {
                _archivedChatsExpanded = !_archivedChatsExpanded;
                RenderRecent();
                DispatcherQueue.TryEnqueue(FocusArchivedChatsToggleOrFallback);
            };
            RecentList.Children.Add(archivedHeader);
            if (_archivedChatsExpanded)
            {
                var archivedList = new StackPanel { Margin = new Thickness(8, 3, 0, 4), Spacing = 3 };
                foreach (var session in archivedSessions) archivedList.Children.Add(CreateRecentRow(session));
                RecentList.Children.Add(archivedList);
            }
        }
    }

    private Grid CreateRecentRow(ChatSession session)
    {
        var title = new TextBlock { Text = session.Title, TextTrimming = TextTrimming.CharacterEllipsis, Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 54, 74, 81)), FontSize = 12 };
        var updated = new TextBlock
        {
            Text = string.Join(" · ", new[] { session.ProjectName, session.UpdatedAt.ToLocalTime().ToString("g") }.Where(value => !string.IsNullOrWhiteSpace(value))),
            Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 113, 133, 140)), FontSize = 10
        };
        var button = new Button
        {
            Content = new StackPanel { Spacing = 3, Children = { title, updated } }, Tag = session.Id,
            HorizontalContentAlignment = HorizontalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = new SolidColorBrush(ColorHelper.FromArgb(255, 238, 248, 249)), Padding = new Thickness(9, 7, 9, 7), CornerRadius = new CornerRadius(8)
        };
        AutomationProperties.SetName(button, $"Open conversation: {session.Title}");
        button.Click += RecentClick;
        var actions = new Button
        {
            Content = "⋯", Tag = session.Id, Width = 32, Height = 34, Margin = new Thickness(3, 0, 0, 0), Padding = new Thickness(0),
            Background = new SolidColorBrush(ColorHelper.FromArgb(255, 238, 248, 249)), Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 82, 111, 119)), CornerRadius = new CornerRadius(8)
        };
        AutomationProperties.SetName(actions, $"More actions for {session.Title}");
        actions.Click += RecentActionsClick;
        _recentActionButtons[session.Id] = actions;
        var row = new Grid { ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition { Width = GridLength.Auto } } };
        row.Children.Add(button);
        Grid.SetColumn(actions, 1);
        row.Children.Add(actions);
        row.RightTapped += (_, args) =>
        {
            args.Handled = true;
            ShowRecentActionsMenu(session, actions, row, args.GetPosition(row));
        };
        return row;
    }

    private async void RecentClick(object sender, RoutedEventArgs e) { CloseCompactSidebar(); await RunUiActionAsync(() => RecentClickAsync(sender)); }

    private async Task RecentClickAsync(object sender)
    {
        if (sender is not Button { Tag: string sessionId }) return;
        if (_requestCancellation is not null)
        {
            RuntimeStatus.Text = "Wait for the current response to finish before opening another chat.";
            return;
        }
        var session = _sessions.FirstOrDefault(item => item.Id == sessionId);
        if (session is null) return;
        if (!await ConfirmTemporaryDiscardAsync("open a saved chat")) return;
        SaveCurrentSession();
        ClearPendingAttachments();
        _isTemporaryChat = false;
        _mode = session.Mode;
        UpdateModeControls();
        UpdateTemporaryControls();
        PromptBox.PlaceholderText = _mode == WorkspaceModes.CodingAgent ? "Describe what you want to change in this project…" : "Ask anything… use / for commands";
        _activeSessionId = session.Id;
        _activeChatFolder = session.FolderName;
        _editingMessageIndex = null;
        UpdateTemporaryControls();
        _messages.Clear();
        _messages.AddRange(SelectedBranchMessages(session));
        var project = _projects.FirstOrDefault(item => item.Id == session.ProjectId) ??
            _projects.FirstOrDefault(item => item.Name.Equals(session.ProjectName, StringComparison.CurrentCultureIgnoreCase));
        _activeProjectId = project?.Id ?? session.ProjectId;
        _activeProjectName = project?.Name ?? session.ProjectName;
        _includeProjectFilesInChat = _mode == WorkspaceModes.Chat && session.IncludeProjectFilesInChat;
        _settings.ProjectInstructions = session.ProjectInstructions;
        SaveSettings();
        PageTitle.Text = _activeProjectName ?? session.Title;
        ProjectBadge.Visibility = _activeProjectName is null ? Visibility.Collapsed : Visibility.Visible;
        UpdateWorkspaceBar();
        UpdateProjectFilesControls();
        UpdateWebSearchButton();
        WelcomePanel.Visibility = _messages.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ConversationScroll.Visibility = _messages.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        ConversationMessages.Children.Clear();
        for (var index = 0; index < _messages.Count; index++)
        {
            var message = _messages[index];
            AddMessage(message.Role == "user" ? "You" : _mode == WorkspaceModes.CodingAgent ? "Coding Agent" : "Server Rock", message.Content, message.Role == "user", message.Attachments, message.Sources, index, showActions: true);
        }
        RenderProjects();
        RenderRecent();
        UpdateBranchControls();
        PromptBox.Focus(FocusState.Programmatic);
    }

    private void RecentActionsClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string sessionId }) return;
        var session = _sessions.FirstOrDefault(item => item.Id == sessionId);
        if (session is null) return;
        ShowRecentActionsMenu(session, (Button)sender);
    }

    private void ShowRecentActionsMenu(ChatSession session, Button focusTrigger, FrameworkElement? contextAnchor = null, Point? contextPosition = null)
    {
        var sessionId = session.Id;
        if (_requestCancellation is not null)
        {
            RuntimeStatus.Text = "Wait for the current response to finish before organizing chats.";
            return;
        }
        var menu = new MenuFlyout();
        var actionSelected = false;

        void RestoreFocus()
        {
            DispatcherQueue.TryEnqueue(() => FocusRecentActionOrFallback(sessionId));
        }

        void Run(Action action)
        {
            actionSelected = true;
            try { action(); }
            finally { RestoreFocus(); }
        }

        async Task RunAsync(Func<Task> action)
        {
            actionSelected = true;
            try { await action(); }
            finally { RestoreFocus(); }
        }

        var rename = new MenuFlyoutItem { Text = "Rename chat", Tag = sessionId };
        rename.Click += async (_, _) => await RunAsync(() => RenameSessionAsync(sessionId));
        var archive = new MenuFlyoutItem { Text = session.Archived ? "Restore from archive" : "Archive chat", Tag = sessionId };
        archive.Click += (_, _) => Run(() => ToggleSessionArchive(sessionId));
        var fork = new MenuFlyoutItem { Text = "Fork from here", Tag = sessionId };
        fork.Click += (_, _) => Run(() => ForkSession(sessionId));
        var delete = new MenuFlyoutItem { Text = "Delete chat", Tag = sessionId };
        delete.Click += async (_, _) => await RunAsync(() => DeleteSessionAsync(sessionId));
        menu.Items.Add(rename);
        menu.Items.Add(archive);
        menu.Items.Add(fork);
        if (!session.Archived && _settings.ChatFolders.Count > 0)
        {
            menu.Items.Add(new MenuFlyoutSeparator());
            foreach (var folder in _settings.ChatFolders)
            {
                var move = new MenuFlyoutItem { Text = $"Move to {folder}", Tag = (sessionId, folder) };
                move.Click += (_, _) => Run(() =>
                {
                    if (move.Tag is ValueTuple<string, string> selection) MoveChatToFolder(selection.Item1, selection.Item2);
                });
                menu.Items.Add(move);
            }
            var unfiled = new MenuFlyoutItem { Text = "Move to Unfiled", Tag = sessionId };
            unfiled.Click += (_, _) => Run(() => MoveChatToFolder(sessionId, null));
            menu.Items.Add(unfiled);
        }
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(delete);
        menu.Closed += (_, _) =>
        {
            if (!actionSelected) RestoreFocus();
        };
        if (contextAnchor is not null && contextPosition is Point position) menu.ShowAt(contextAnchor, position);
        else menu.ShowAt(focusTrigger);
    }

    private void FocusRecentActionOrFallback(string sessionId)
    {
        if (_recentActionButtons.TryGetValue(sessionId, out var trigger) && trigger.Visibility == Visibility.Visible && trigger.IsEnabled && trigger.Focus(FocusState.Programmatic))
            return;

        var fallback = _recentActionButtons.Values.FirstOrDefault(button => button.Visibility == Visibility.Visible && button.IsEnabled);
        if (fallback?.Focus(FocusState.Programmatic) == true) return;
        NewConversationButton.Focus(FocusState.Programmatic);
    }

    private void ToggleSessionArchive(string sessionId)
    {
        if (_requestCancellation is not null)
        {
            RuntimeStatus.Text = "Wait for the current response to finish before archiving or restoring a chat.";
            return;
        }
        var session = _sessions.FirstOrDefault(item => item.Id == sessionId);
        if (session is null) return;
        if (_activeSessionId == sessionId) SaveCurrentSession();
        var wasArchived = session.Archived;
        session.Archived = !wasArchived;
        if (!PersistConversationEdits())
        {
            session.Archived = wasArchived;
            RenderRecent();
            return;
        }
        _archivedChatsExpanded = session.Archived;
        RenderRecent();
        RuntimeStatus.Text = session.Archived ? "Chat moved to Archived." : "Chat restored to Recents.";
    }

    private async Task RenameSessionAsync(string sessionId)
    {
        if (_requestCancellation is not null)
        {
            RuntimeStatus.Text = "Wait for the current response to finish before renaming a chat.";
            return;
        }
        var session = _sessions.FirstOrDefault(item => item.Id == sessionId);
        if (session is null) return;
        var title = new TextBox { Text = session.Title, MaxLength = 80, PlaceholderText = "Chat name" };
        var dialog = new ContentDialog
        {
            Title = "Rename chat",
            Content = title,
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            XamlRoot = XamlRoot
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        var updatedTitle = title.Text.Trim();
        if (updatedTitle.Length == 0)
        {
            RuntimeStatus.Text = "A chat name cannot be empty.";
            return;
        }
        var previousTitle = session.Title;
        session.Title = updatedTitle;
        if (!PersistConversationEdits())
        {
            session.Title = previousTitle;
            RenderRecent();
            return;
        }
        if (_activeSessionId == sessionId && _activeProjectName is null) PageTitle.Text = updatedTitle;
        RuntimeStatus.Text = "Chat renamed.";
    }

    private async Task DeleteSessionAsync(string sessionId)
    {
        if (_requestCancellation is not null)
        {
            RuntimeStatus.Text = "Wait for the current response to finish before deleting a chat.";
            return;
        }
        var session = _sessions.FirstOrDefault(item => item.Id == sessionId);
        if (session is null) return;
        var dialog = new ContentDialog
        {
            Title = "Delete chat?",
            Content = $"“{session.Title}” and its saved messages will be removed from this device.",
            PrimaryButtonText = "Delete chat",
            CloseButtonText = "Cancel",
            XamlRoot = XamlRoot
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        if (_activeSessionId == sessionId) SaveCurrentSession();
        var wasActive = _activeSessionId == sessionId;
        _sessions.Remove(session);
        if (!PersistConversationEdits())
        {
            _sessions.Add(session);
            RenderRecent();
            return;
        }
        if (wasActive)
        {
            ClearPendingAttachments();
            _activeSessionId = null;
            _activeChatFolder = null;
            _editingMessageIndex = null;
            UpdateEditingControls();
            _messages.Clear();
            ConversationMessages.Children.Clear();
            WelcomePanel.Visibility = Visibility.Visible;
            ConversationScroll.Visibility = Visibility.Collapsed;
            PageTitle.Text = _activeProjectName ?? "New chat";
        }
        RuntimeStatus.Text = "Chat deleted from this device.";
    }

    private bool PersistConversationEdits()
    {
        if (!_historyAvailable)
        {
            RuntimeStatus.Text = "Conversation history is unavailable, so chat changes cannot be saved.";
            return false;
        }
        try
        {
            _conversationStore.Save(_sessions);
            RenderRecent();
            return true;
        }
        catch (Exception ex)
        {
            _historyAvailable = false;
            RuntimeStatus.Text = $"Chat changes could not be saved: {ex.Message}";
            return false;
        }
    }

    private void SaveCurrentSession()
    {
        if (_isTemporaryChat || !_historyAvailable || _messages.Count == 0) return;
        var session = _sessions.FirstOrDefault(item => item.Id == _activeSessionId);
        if (session is null)
        {
            var firstPrompt = _messages.FirstOrDefault(item => item.Role == "user")?.Content ?? "New chat";
            session = new ChatSession
            {
                Id = Guid.NewGuid().ToString("N"),
                Title = firstPrompt.Length > 48 ? firstPrompt[..48].TrimEnd() + "…" : firstPrompt,
                ProjectId = _activeProjectId,
                ProjectName = _activeProjectName,
                ProjectInstructions = _settings.ProjectInstructions,
                Mode = _mode,
                IncludeProjectFilesInChat = _includeProjectFilesInChat,
                FolderName = _activeChatFolder
            };
            _sessions.Add(session);
            _activeSessionId = session.Id;
            // The first save names the chat after its first prompt; show that name in the
            // header right away (it used to stay "New chat" until the chat was reopened).
            if (_activeProjectName is null) PageTitle.Text = session.Title;
        }
        if (session.BranchIndex > 0 && session.BranchIndex <= session.Branches.Count)
            session.Branches[session.BranchIndex - 1] = [.. _messages];
        else
            session.Messages = [.. _messages];
        session.ProjectId = _activeProjectId;
        session.ProjectName = _activeProjectName;
        session.ProjectInstructions = _settings.ProjectInstructions;
        session.Mode = _mode;
        session.IncludeProjectFilesInChat = _includeProjectFilesInChat;
        session.FolderName = _activeChatFolder;
        session.UpdatedAt = DateTimeOffset.UtcNow;
        try
        {
            _conversationStore.Save(_sessions);
            RenderRecent();
        }
        catch (Exception ex)
        {
            _historyAvailable = false;
            RuntimeStatus.Text = $"Conversation could not be saved locally: {ex.Message}";
            RenderRecent();
        }
    }
}
