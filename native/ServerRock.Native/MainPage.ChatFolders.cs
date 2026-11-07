// Sidebar chat folders: create, rename, delete (chats move to Unfiled), collapse/expand,
// move a chat into a folder, and "new chat in this folder". Folder names live in
// AppSettings.ChatFolders; each chat's folder is ChatSession.FolderName.
// The Focus*OrFallback helpers put keyboard focus back on the right control after the sidebar
// list is rebuilt (the old buttons no longer exist after RenderRecent).
// Depends on: SaveSettings, PersistConversationEdits/RenderRecent (MainPage.Recents.cs), and the
// _chatFolder* button maps in MainPage.xaml.cs.
// Called by: the New folder sidebar button, folder rows and menus built in RenderRecent, the
// recent-chat actions menu, and the /folder slash command.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private async void NewChatFolderClick(object sender, RoutedEventArgs e) { CloseCompactSidebar(); await RunUiActionAsync(CreateChatFolderAsync); }

    private async Task CreateChatFolderAsync()
    {
        var name = new TextBox { PlaceholderText = "For example, Research or Writing", MaxLength = 40 };
        var dialog = new ContentDialog { Title = "Create chat folder", Content = name, PrimaryButtonText = "Create folder", CloseButtonText = "Cancel", XamlRoot = XamlRoot };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        var value = name.Text.Trim();
        if (value.Length == 0 || _settings.ChatFolders.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            RuntimeStatus.Text = value.Length == 0 ? "Enter a folder name." : "A folder with that name already exists.";
            return;
        }
        _settings.ChatFolders.Add(value);
        if (!SaveSettings())
        {
            _settings.ChatFolders.Remove(value);
            return;
        }
        _collapsedChatFolders.Remove(value);
        RenderRecent();
        RuntimeStatus.Text = $"Chat folder “{value}” created.";
    }

    private void ChatFolderActionsClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string folder } trigger) return;
        if (_requestCancellation is not null)
        {
            RuntimeStatus.Text = "Wait for the current response to finish before organizing chats.";
            return;
        }
        var menu = new MenuFlyout();
        var actionSelected = false;

        void RestoreFolderFocus() => DispatcherQueue.TryEnqueue(() => FocusChatFolderActionOrFallback(folder));

        async Task RunFolderActionAsync(Func<Task> action)
        {
            actionSelected = true;
            try { await action(); }
            finally { RestoreFolderFocus(); }
        }

        var newChat = new MenuFlyoutItem { Text = "New chat in this folder", Tag = folder };
        newChat.Click += (_, _) =>
        {
            actionSelected = true;
            if (_requestCancellation is not null)
            {
                RuntimeStatus.Text = "Wait for the current response to finish before starting a chat in a folder.";
                RestoreFolderFocus();
                return;
            }
            NewChatClick(this, new RoutedEventArgs());
            _activeChatFolder = folder;
            UpdateTemporaryControls();
            RuntimeStatus.Text = $"New chat will be filed under “{folder}”.";
            PromptBox.Focus(FocusState.Programmatic);
        };
        var rename = new MenuFlyoutItem { Text = "Rename folder", Tag = folder };
        rename.Click += async (_, _) => await RunFolderActionAsync(() => RenameChatFolderAsync(folder));
        var delete = new MenuFlyoutItem { Text = "Delete folder", Tag = folder };
        delete.Click += async (_, _) => await RunFolderActionAsync(() => DeleteChatFolderAsync(folder));
        menu.Items.Add(newChat);
        menu.Items.Add(rename);
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(delete);
        menu.Closed += (_, _) =>
        {
            if (!actionSelected) RestoreFolderFocus();
        };
        menu.ShowAt(trigger);
    }

    private void FocusChatFolderActionOrFallback(string folder)
    {
        var survivingFolder = _settings.ChatFolders.FirstOrDefault(name => name.Equals(folder, StringComparison.OrdinalIgnoreCase));
        if (survivingFolder is not null && _chatFolderActionButtons.TryGetValue(survivingFolder, out var trigger) &&
            trigger.Visibility == Visibility.Visible && trigger.IsEnabled && trigger.Focus(FocusState.Programmatic))
            return;

        var folderFallback = _chatFolderActionButtons.Values.FirstOrDefault(button => button.Visibility == Visibility.Visible && button.IsEnabled);
        if (folderFallback?.Focus(FocusState.Programmatic) == true) return;
        var chatFallback = _recentActionButtons.Values.FirstOrDefault(button => button.Visibility == Visibility.Visible && button.IsEnabled);
        if (chatFallback?.Focus(FocusState.Programmatic) == true) return;
        NewConversationButton.Focus(FocusState.Programmatic);
    }

    private void FocusChatFolderToggleOrFallback(string folder)
    {
        if (_chatFolderToggleButtons.TryGetValue(folder, out var toggle) &&
            toggle.Visibility == Visibility.Visible && toggle.IsEnabled && toggle.Focus(FocusState.Programmatic))
            return;
        if (_archivedChatsToggleButton?.Visibility == Visibility.Visible && _archivedChatsToggleButton.IsEnabled &&
            _archivedChatsToggleButton.Focus(FocusState.Programmatic))
            return;
        FocusChatFolderActionOrFallback(folder);
    }

    private void FocusArchivedChatsToggleOrFallback()
    {
        if (_archivedChatsToggleButton?.Visibility == Visibility.Visible && _archivedChatsToggleButton.IsEnabled &&
            _archivedChatsToggleButton.Focus(FocusState.Programmatic))
            return;
        var folderToggle = _chatFolderToggleButtons.Values.FirstOrDefault(button => button.Visibility == Visibility.Visible && button.IsEnabled);
        if (folderToggle?.Focus(FocusState.Programmatic) == true) return;
        var folderAction = _chatFolderActionButtons.Values.FirstOrDefault(button => button.Visibility == Visibility.Visible && button.IsEnabled);
        if (folderAction?.Focus(FocusState.Programmatic) == true) return;
        var chatAction = _recentActionButtons.Values.FirstOrDefault(button => button.Visibility == Visibility.Visible && button.IsEnabled);
        if (chatAction?.Focus(FocusState.Programmatic) == true) return;
        NewConversationButton.Focus(FocusState.Programmatic);
    }

    private async Task RenameChatFolderAsync(string oldName)
    {
        var name = new TextBox { Text = oldName, MaxLength = 40 };
        var dialog = new ContentDialog { Title = "Rename chat folder", Content = name, PrimaryButtonText = "Save", CloseButtonText = "Cancel", XamlRoot = XamlRoot };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        var newName = name.Text.Trim();
        if (newName.Length == 0 || !newName.Equals(oldName, StringComparison.OrdinalIgnoreCase) && _settings.ChatFolders.Contains(newName, StringComparer.OrdinalIgnoreCase))
        {
            RuntimeStatus.Text = newName.Length == 0 ? "A folder name cannot be empty." : "A folder with that name already exists.";
            return;
        }
        var folderIndex = _settings.ChatFolders.FindIndex(value => value.Equals(oldName, StringComparison.OrdinalIgnoreCase));
        if (folderIndex < 0) return;
        var previous = _sessions.Where(session => session.FolderName?.Equals(oldName, StringComparison.OrdinalIgnoreCase) == true).ToDictionary(session => session, session => session.FolderName);
        _settings.ChatFolders[folderIndex] = newName;
        foreach (var session in previous.Keys) session.FolderName = newName;
        if (!SaveSettings())
        {
            _settings.ChatFolders[folderIndex] = oldName;
            foreach (var pair in previous) pair.Key.FolderName = pair.Value;
            return;
        }
        if (!PersistConversationEdits())
        {
            _settings.ChatFolders[folderIndex] = oldName;
            foreach (var pair in previous) pair.Key.FolderName = pair.Value;
            SaveSettings();
            RenderRecent();
            return;
        }
        if (_activeChatFolder?.Equals(oldName, StringComparison.OrdinalIgnoreCase) == true) _activeChatFolder = newName;
        UpdateTemporaryControls();
        _collapsedChatFolders.Remove(oldName);
        RenderRecent();
        RuntimeStatus.Text = $"Folder renamed to “{newName}”.";
    }

    private async Task DeleteChatFolderAsync(string folder)
    {
        var count = _sessions.Count(session => session.FolderName?.Equals(folder, StringComparison.OrdinalIgnoreCase) == true);
        var dialog = new ContentDialog
        {
            Title = "Delete chat folder?",
            Content = $"The folder will be removed. Its {count} chat(s), if any, will be kept under Unfiled.",
            PrimaryButtonText = "Delete folder",
            CloseButtonText = "Cancel",
            XamlRoot = XamlRoot
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        var previousNames = _sessions.Where(session => session.FolderName?.Equals(folder, StringComparison.OrdinalIgnoreCase) == true).ToDictionary(session => session, session => session.FolderName);
        _settings.ChatFolders.RemoveAll(value => value.Equals(folder, StringComparison.OrdinalIgnoreCase));
        foreach (var session in previousNames.Keys) session.FolderName = null;
        if (!SaveSettings())
        {
            _settings.ChatFolders.Add(folder);
            foreach (var pair in previousNames) pair.Key.FolderName = pair.Value;
            return;
        }
        if (!PersistConversationEdits())
        {
            _settings.ChatFolders.Add(folder);
            foreach (var pair in previousNames) pair.Key.FolderName = pair.Value;
            SaveSettings();
            RenderRecent();
            return;
        }
        if (_activeChatFolder?.Equals(folder, StringComparison.OrdinalIgnoreCase) == true) _activeChatFolder = null;
        UpdateTemporaryControls();
        _collapsedChatFolders.Remove(folder);
        RenderRecent();
        RuntimeStatus.Text = $"Folder “{folder}” deleted. Its chats remain in Unfiled.";
    }

    private void MoveChatToFolder(string sessionId, string? folder)
    {
        if (_requestCancellation is not null)
        {
            RuntimeStatus.Text = "Wait for the current response to finish before organizing chats.";
            return;
        }
        var session = _sessions.FirstOrDefault(item => item.Id == sessionId);
        if (session is null) return;
        if (folder is not null && !_settings.ChatFolders.Contains(folder, StringComparer.OrdinalIgnoreCase)) return;
        var previous = session.FolderName;
        session.FolderName = folder;
        if (_activeSessionId == sessionId) _activeChatFolder = folder;
        if (_activeSessionId == sessionId) UpdateTemporaryControls();
        if (!PersistConversationEdits())
        {
            session.FolderName = previous;
            if (_activeSessionId == sessionId)
            {
                _activeChatFolder = previous;
                UpdateTemporaryControls();
            }
            RenderRecent();
            return;
        }
        RuntimeStatus.Text = folder is null ? "Chat moved to Unfiled." : $"Chat moved to “{folder}”.";
    }

    private async Task ChooseCurrentChatFolderAsync()
    {
        if (_requestCancellation is not null)
        {
            RuntimeStatus.Text = "Wait for the current response to finish before organizing chats.";
            return;
        }
        if (_isTemporaryChat)
        {
            RuntimeStatus.Text = "Temporary chats are not filed or saved.";
            return;
        }
        var options = new[] { "Unfiled" }.Concat(_settings.ChatFolders).ToArray();
        var picker = new ListView { ItemsSource = options, SelectionMode = ListViewSelectionMode.Single, MinWidth = 300 };
        var current = string.IsNullOrWhiteSpace(_activeChatFolder) ? "Unfiled" : _activeChatFolder;
        picker.SelectedItem = options.FirstOrDefault(value => value.Equals(current, StringComparison.OrdinalIgnoreCase)) ?? "Unfiled";
        var dialog = new ContentDialog { Title = "Choose a chat folder", Content = picker, PrimaryButtonText = "Save", CloseButtonText = "Cancel", XamlRoot = XamlRoot };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary || picker.SelectedItem is not string selected) return;
        var folder = selected == "Unfiled" ? null : selected;
        var previous = _activeChatFolder;
        _activeChatFolder = folder;
        UpdateTemporaryControls();
        if (_activeSessionId is not null)
        {
            var session = _sessions.FirstOrDefault(item => item.Id == _activeSessionId);
            if (session is not null) session.FolderName = folder;
            if (!PersistConversationEdits())
            {
                _activeChatFolder = previous;
                if (session is not null) session.FolderName = previous;
                UpdateTemporaryControls();
                RenderRecent();
                return;
            }
        }
        RuntimeStatus.Text = folder is null ? "This chat is unfiled." : $"This chat is filed under “{folder}”.";
    }
}
