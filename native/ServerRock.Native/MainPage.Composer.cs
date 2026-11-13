// Composer controls under the prompt box: the Tools toggle (second toolbar row), web search,
// temporary chat, project files in chat, smart rewrite, the "editing a message" banner, and
// saved-chat search.
// Depends on: SaveSettings, SaveCurrentSession, and toolbar elements in MainPage.xaml.
// Called by: composer buttons in MainPage.xaml, slash commands (/tools, /web, /temporary, /search),
// and layout/mode code that refreshes the button states.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using ServerRock.Core.Models;
using ServerRock.Core.Infrastructure;
using ServerRock.Core.Services;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private void WebSearchClick(object sender, RoutedEventArgs e)
    {
        if (_mode != WorkspaceModes.Chat) return;
        if (_documentsOnlyMode)
        {
            RuntimeStatus.Text = "Documents-only mode keeps web search off until you turn documents-only mode off.";
            return;
        }
        _settings.WebSearchEnabled = !_settings.WebSearchEnabled;
        SaveSettings();
        UpdateWebSearchButton();
        RuntimeStatus.Text = _settings.WebSearchEnabled
            ? $"Web search on · query goes to DuckDuckGo; results{(_includeProjectFilesInChat ? ", prompt, and selected project excerpts" : " and prompt")} go to the selected model."
            : $"Web search off · prompt{(_includeProjectFilesInChat ? " and selected project excerpts" : "")} go to the selected model server.";
    }

    private void DocumentsOnlyClick(object sender, RoutedEventArgs e)
    {
        if (_mode != WorkspaceModes.Chat) return;
        _documentsOnlyMode = !_documentsOnlyMode;
        if (_documentsOnlyMode && _settings.WebSearchEnabled)
        {
            _settings.WebSearchEnabled = false;
            SaveSettings();
        }
        UpdateDocumentsOnlyControls();
        UpdateWebSearchButton();
        RuntimeStatus.Text = _documentsOnlyMode
            ? "Documents-only mode on · attach at least one document; web search is disabled and answers must stay grounded in those documents."
            : "Documents-only mode off · normal Chat routing is available again.";
    }

    private async void TemporaryClick(object sender, RoutedEventArgs e)
    {
        if (_temporaryTransitionInProgress) return;
        _temporaryTransitionInProgress = true;
        TemporaryButton.IsEnabled = false;
        try { await RunUiActionAsync(() => SetTemporaryModeAsync(!_isTemporaryChat)); }
        finally
        {
            _temporaryTransitionInProgress = false;
            TemporaryButton.IsEnabled = true;
        }
    }

    private async Task SetTemporaryModeAsync(bool enabled)
    {
        if (_requestCancellation is not null)
        {
            RuntimeStatus.Text = "Wait for the current response to finish before changing temporary chat mode.";
            return;
        }
        if (enabled == _isTemporaryChat) return;
        if (enabled)
        {
            if (_messages.Count > 0 && !_historyAvailable)
            {
                RuntimeStatus.Text = "Conversation history is unavailable. Save or copy this chat before starting a temporary one.";
                return;
            }
            if (_messages.Count > 0)
            {
                if (XamlRoot is null)
                {
                    RuntimeStatus.Text = "Temporary chat is unavailable until the window is ready. Please try again in a moment.";
                    return;
                }
                var dialog = new ContentDialog
                {
                    Title = "Start a temporary chat?",
                    Content = "The current conversation will be saved, then a blank temporary chat will start. New messages will stay in memory and will not be written to Recents or local history.",
                    PrimaryButtonText = "Start temporary chat",
                    CloseButtonText = "Cancel",
                    XamlRoot = XamlRoot
                };
                if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
            }
            SaveCurrentSession();
            if (!_historyAvailable) return;
            _activeSessionId = null;
            _activeChatFolder = null;
            _editingMessageIndex = null;
            _includeProjectFilesInChat = false;
            _messages.Clear();
            ConversationMessages.Children.Clear();
            WelcomePanel.Visibility = Visibility.Visible;
            ConversationScroll.Visibility = Visibility.Collapsed;
            PageTitle.Text = _activeProjectName ?? "New chat";
            _isTemporaryChat = true;
            RuntimeStatus.Text = "Temporary chat started · messages will not be saved.";
        }
        else
        {
            if (!await ConfirmTemporaryDiscardAsync("turn off Temporary")) return;
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
            _isTemporaryChat = false;
            RuntimeStatus.Text = "Temporary chat cleared and ended.";
        }
        UpdateTemporaryControls();
        UpdateProjectFilesControls();
        UpdateWebSearchButton();
        RenderRecent();
    }

    private async Task<bool> ConfirmTemporaryDiscardAsync(string action)
    {
        if (!_isTemporaryChat || _messages.Count == 0 && _pendingAttachments.Count == 0) return true;
        if (XamlRoot is null)
        {
            RuntimeStatus.Text = "Temporary chat is unavailable until the window is ready. Please try again in a moment.";
            return false;
        }
        var dialog = new ContentDialog
        {
            Title = "Discard temporary chat?",
            Content = $"To {action}, the temporary messages and selected attachments will be cleared and never saved to Recents or local history.",
            PrimaryButtonText = "Discard and continue",
            CloseButtonText = "Stay here",
            XamlRoot = XamlRoot
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    private void UpdateTemporaryControls()
    {
        // Tools toggles: plain "<name> on/off" labels (no mixed glyphs) plus a visible on-state.
        TemporaryButton.Content = _compactComposerToolbar
            ? _isTemporaryChat ? "Temp on" : "Temp off"
            : _isTemporaryChat ? "Temporary on" : "Temporary off";
        UiTheme.ApplyToggleState(TemporaryButton, _isTemporaryChat);
        TemporaryBadge.Visibility = _isTemporaryChat ? Visibility.Visible : Visibility.Collapsed;
        UpdateEditingControls();
        ChatFolderBadge.Visibility = string.IsNullOrWhiteSpace(_activeChatFolder) || _isTemporaryChat ? Visibility.Collapsed : Visibility.Visible;
        ChatFolderBadgeText.Text = _activeChatFolder is null ? string.Empty : $"▱　{_activeChatFolder}";
        UpdateBranchControls();
    }

    private void UpdateEditingControls()
    {
        CancelEditButton.Content = _compactComposerToolbar ? "Cancel" : "Cancel edit";
        CancelEditButton.Visibility = _editingMessageIndex is null ? Visibility.Collapsed : Visibility.Visible;
    }

    private void CancelEditClick(object sender, RoutedEventArgs e)
    {
        if (_editingMessageIndex is null) return;
        _editingMessageIndex = null;
        PromptBox.Text = string.Empty;
        ClearPendingAttachments();
        UpdateEditingControls();
        RuntimeStatus.Text = "Message edit canceled. Conversation history is unchanged.";
    }

    private void ProjectFilesClick(object sender, RoutedEventArgs e)
    {
        if (_requestCancellation is not null)
        {
            RuntimeStatus.Text = "Wait for the response to finish before changing Chat project files.";
            return;
        }
        if (ProjectFilesButton.Visibility != Visibility.Visible) return;
        _includeProjectFilesInChat = !_includeProjectFilesInChat;
        SaveCurrentSession();
        UpdateProjectFilesControls();
        UpdateWebSearchButton();
        RuntimeStatus.Text = _includeProjectFilesInChat
            ? "Project files on · bounded excerpts from selected folders will be included with Chat messages."
            : "Project files off · Chat will use project instructions, not folder contents.";
    }

    private void UpdateProjectFilesControls()
    {
        var project = _projects.FirstOrDefault(item => item.Id == _activeProjectId);
        var available = _mode == WorkspaceModes.Chat && project is not null && project.FolderPaths.Count > 0;
        ProjectFilesButton.Visibility = available ? Visibility.Visible : Visibility.Collapsed;
        ProjectFilesButton.Content = _compactComposerToolbar
            ? _includeProjectFilesInChat ? "Files on" : "Files off"
            : _includeProjectFilesInChat ? "Project files on" : "Project files off";
        UiTheme.ApplyToggleState(ProjectFilesButton, _includeProjectFilesInChat);
        ProjectBadgeText.Text = _mode == WorkspaceModes.CodingAgent
            ? project?.FolderPaths.Count > 0 ? "Selected project files in context" : "Project instructions on"
            : _includeProjectFilesInChat ? "Project files on" : "Project instructions on";
        ProcessingLabel.Text = DescribeDataRoute();
        UpdateDocumentsOnlyControls();
    }

    private async void SearchClick(object sender, RoutedEventArgs e) { CloseCompactSidebar(); await RunUiActionAsync(SearchClickAsync); }

    private async Task SearchClickAsync()
    {
        var query = new TextBox { PlaceholderText = "Search chats in both workspaces", MinWidth = 360 };
        var results = new ListView { DisplayMemberPath = nameof(ConversationSearchHit.Display), MinHeight = 240, SelectionMode = ListViewSelectionMode.Single };
        void Refresh()
        {
            try { results.ItemsSource = ConversationSearchService.Search(_sessions, query.Text); }
            catch (ArgumentException) { results.ItemsSource = Array.Empty<ConversationSearchHit>(); }
        }
        query.TextChanged += (_, _) => Refresh();
        Refresh();
        var dialog = new ContentDialog { Title = "Search your workspace", Content = new StackPanel { Spacing = 10, Children = { query, results } }, PrimaryButtonText = "Open chat", CloseButtonText = "Close", XamlRoot = XamlRoot };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary && results.SelectedItem is ConversationSearchHit hit)
            RecentClick(new Button { Tag = hit.Session.Id }, new RoutedEventArgs());
    }

    private async Task SearchCurrentConversationAsync()
    {
        if (_messages.Count == 0)
        {
            RuntimeStatus.Text = "There are no messages in the current chat to search.";
            return;
        }
        var query = new TextBox { Header = "Find in this chat", PlaceholderText = "Search message text", MinWidth = 420 };
        var results = new ListView { MinHeight = 220, MaxHeight = 420, SelectionMode = ListViewSelectionMode.Single, DisplayMemberPath = nameof(ConversationMessageSearchHit.Preview) };
        AutomationProperties.SetName(query, "Search current conversation messages");
        AutomationProperties.SetHelpText(query, "Search only the messages already loaded in this chat. Nothing is sent to a provider.");
        AutomationProperties.SetName(results, "Current conversation search results");
        void RefreshResults()
        {
            try { results.ItemsSource = ConversationMessageSearchService.Search(_messages, query.Text); }
            catch (ArgumentException) { results.ItemsSource = Array.Empty<ConversationMessageSearchHit>(); }
        }
        query.TextChanged += (_, _) => RefreshResults();
        RefreshResults();
        var dialog = new ContentDialog
        {
            Title = "Find in current chat",
            Content = new StackPanel { Spacing = 10, Children = { query, results } },
            PrimaryButtonText = "Jump to match",
            CloseButtonText = "Close",
            XamlRoot = XamlRoot
        };
        dialog.Opened += (_, _) => query.Focus(FocusState.Programmatic);
        if (await dialog.ShowAsync() == ContentDialogResult.Primary && results.SelectedItem is ConversationMessageSearchHit hit)
        {
            JumpToConversationMessage(hit.MessageIndex);
            RuntimeStatus.Text = $"Jumped to message {hit.MessageIndex + 1} of {_messages.Count}.";
        }
    }

    private void JumpToConversationMessage(int messageIndex)
    {
        if (messageIndex < 0 || messageIndex >= ConversationMessages.Children.Count) return;
        if (ConversationMessages.Children[messageIndex] is not FrameworkElement message) return;
        try
        {
            var point = message.TransformToVisual(ConversationScroll).TransformPoint(new Windows.Foundation.Point(0, 0));
            ConversationScroll.ChangeView(null, Math.Max(0, ConversationScroll.VerticalOffset + point.Y - 36), null);
            AutomationProperties.SetName(message, $"Conversation message {messageIndex + 1}");
        }
        catch { RuntimeStatus.Text = "The selected message is no longer visible."; }
    }

    private void UpdateWebSearchButton()
    {
        WebSearchButton.IsEnabled = !_documentsOnlyMode;
        WebSearchButton.Content = _compactComposerToolbar
            ? _settings.WebSearchEnabled ? "Web on" : "Web off"
            : _settings.WebSearchEnabled ? "Web search on" : "Web search off";
        UiTheme.ApplyToggleState(WebSearchButton, _settings.WebSearchEnabled);
        AutomationProperties.SetHelpText(WebSearchButton, _documentsOnlyMode
            ? "Web search is locked off while documents-only mode is active."
            : "Turn optional DuckDuckGo web search on or off. Search result excerpts and your prompt are sent to the selected model server.");
        var local = LocalEndpoint.IsLocal(_settings.Endpoint);
        var projectFiles = _mode == WorkspaceModes.Chat && _includeProjectFilesInChat;
        if (_documentsOnlyMode)
        {
            PrivacyFootnote.Text = "Documents-only mode is on. Only attached documents are grounding context; web search, project files, memory, and outside context are excluded.";
            return;
        }
        if (_isTemporaryChat)
        {
            PrivacyFootnote.Text = _mode == WorkspaceModes.CodingAgent
                ? local ? "Temporary chat is not saved. Selected project files still go to this on-device model."
                    : "Temporary chat is not saved. Selected project files still go to the configured remote model."
                : _settings.WebSearchEnabled
                    ? $"Temporary chat is not saved. Search queries go to DuckDuckGo. Results and your prompt{(projectFiles ? ", plus selected project files," : "")} go to the selected model."
                    : local ? $"Temporary chat is not saved. Prompts, attached files{(projectFiles ? " and selected project files" : "")} go to the on-device model."
                        : $"Temporary chat is not saved. Prompts, attached files{(projectFiles ? " and selected project files" : "")} go to the configured remote model.";
            return;
        }
        PrivacyFootnote.Text = _mode == WorkspaceModes.CodingAgent
            ? local ? "Only files in your selected folders are sent to this on-device model." : "Files from your selected folders are sent to the configured remote model server."
            : _settings.WebSearchEnabled
                ? $"Search queries go to DuckDuckGo. Results and your prompt{(projectFiles ? ", plus selected project files," : "")} go to the selected model server."
                : local ? $"Your prompt{(projectFiles ? " and selected project files go" : " goes")} to the on-device model. Web search is off."
                    : $"Your prompt{(projectFiles ? " and selected project files go" : " goes")} to the configured remote model server. Web search is off.";
    }

    private void UpdateDocumentsOnlyControls()
    {
        AutomationProperties.SetName(DocumentsOnlyButton, "Toggle documents-only mode");
        DocumentsOnlyButton.Visibility = _mode == WorkspaceModes.Chat ? Visibility.Visible : Visibility.Collapsed;
        DocumentsOnlyButton.Content = _compactComposerToolbar
            ? _documentsOnlyMode ? "Docs only on" : "Docs only off"
            : _documentsOnlyMode ? "Documents only on" : "Documents only off";
        UiTheme.ApplyToggleState(DocumentsOnlyButton, _documentsOnlyMode);
        AutomationProperties.SetHelpText(DocumentsOnlyButton, _documentsOnlyMode
            ? "Ground the next Chat response only in attached documents. Web search is disabled and at least one attachment is required."
            : "Ground the next Chat response only in attached documents. Turning this on disables web search and requires an attachment.");
    }

    private void UpdateSmartRewriteButton()
    {
        SmartRewriteButton.Content = _compactComposerToolbar
            ? _settings.SmartPromptRewritingEnabled ? "Rewrite on" : "Rewrite off"
            : _settings.SmartPromptRewritingEnabled ? "Smart rewrite on" : "Smart rewrite off";
        UiTheme.ApplyToggleState(SmartRewriteButton, _settings.SmartPromptRewritingEnabled);
        AutomationProperties.SetHelpText(SmartRewriteButton,
            _settings.SmartPromptRewritingEnabled
                ? "Your draft and current conversation are sent to the selected model for a rewrite preview before sending."
                : "Ask the selected model to rewrite your draft using the conversation context before sending. You review the rewrite before it is sent.");
        UpdateWebSearchButton();
    }

    private void SmartRewriteClick(object sender, RoutedEventArgs e)
    {
        _settings.SmartPromptRewritingEnabled = !_settings.SmartPromptRewritingEnabled;
        SaveSettings();
        UpdateSmartRewriteButton();
        RuntimeStatus.Text = _settings.SmartPromptRewritingEnabled
            ? "Smart rewriting on · drafts are previewed before they reach the agent."
            : "Smart rewriting off · drafts are sent as written.";
    }

    // The composer card is the focus target users see: accent border while the prompt has
    // focus, normal line border otherwise (the inner TextBox focus visual is turned off in XAML).
    private void ComposerGotFocus(object sender, RoutedEventArgs e) => ComposerBorder.BorderBrush = UiTheme.Brush("AccentBrush");
    private void ComposerLostFocus(object sender, RoutedEventArgs e) => ComposerBorder.BorderBrush = UiTheme.Brush("LineBrush");

    private void ComposerToolsClick(object sender, RoutedEventArgs e)
    {
        var isOpen = SecondaryComposerToolbar.Visibility == Visibility.Visible;
        SecondaryComposerToolbar.Visibility = isOpen ? Visibility.Collapsed : Visibility.Visible;
        ComposerToolsButton.Content = isOpen ? "Tools　⌄" : "Tools　⌃";
        AutomationProperties.SetHelpText(ComposerToolsButton, isOpen
            ? "Show optional composer controls such as web search, temporary chat, project files, and smart rewriting."
            : "Hide optional composer controls.");
    }
}
