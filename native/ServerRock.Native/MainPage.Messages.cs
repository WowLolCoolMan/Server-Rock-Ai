// Rendering the conversation: message bubbles (user text, assistant Markdown via
// MarkdownUiRenderer, attachment labels, web source cards) and the per-message actions
// (copy, edit, retry). RebuildConversationView redraws everything from _messages.
// Depends on: MarkdownUiRenderer, ChatMessage/ChatSource (ServerRock.Core), ConversationScroll /
// ConversationMessages in MainPage.xaml.
// Called by: SendClick and streaming (MainPage.Chat.cs), opening a saved chat, branch changes,
// and editing/retrying a message.

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media;
using ServerRock.Core.Models;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private TextBlock AddMessage(
        string author,
        string text,
        bool user,
        IReadOnlyList<ChatAttachment>? attachments = null,
        IReadOnlyList<ChatSource>? sources = null,
        int? messageIndex = null,
        bool showActions = false,
        Action<StackPanel>? addContent = null)
    {
        // Message bubbles and their actions use tokens: user turns sit on AccentSoft, replies on
        // SurfaceAlt, and Copy/Retry/Edit use the shared ContextButtonStyle.
        var bubble = new Border { MaxWidth = user ? 620 : 800, HorizontalAlignment = user ? HorizontalAlignment.Right : HorizontalAlignment.Left, Background = UiTheme.Brush(user ? "AccentSoftBrush" : "SurfaceAltBrush"), BorderBrush = UiTheme.Brush("LineSoftBrush"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(14), Padding = new Thickness(16, 13, 16, 13) };
        var content = new StackPanel { Spacing = 6 };
        content.Children.Add(new TextBlock { Text = author, FontSize = UiTheme.FontSize("CaptionFontSize"), Foreground = UiTheme.Brush("AccentInkBrush") });
        if (attachments is { Count: > 0 })
        {
            content.Children.Add(new TextBlock
            {
                Text = string.Join(" · ", attachments.Select(item => (item.IsImage ? "▧ " : "▤ ") + item.FileName)),
                FontSize = 11,
                Foreground = UiTheme.Brush("AccentInkBrush"),
                TextWrapping = TextWrapping.Wrap
            });
        }
        var body = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, FontSize = UiTheme.FontSize("ReadingFontSize"), Foreground = UiTheme.Brush("InkBrush"), IsTextSelectionEnabled = true };
        if (!user && !string.IsNullOrWhiteSpace(text)) content.Children.Add(MarkdownUiRenderer.Render(text));
        else content.Children.Add(body);
        if (!user && sources is { Count: > 0 }) content.Children.Add(BuildSourcesPanel(sources));
        if (showActions && messageIndex is int index) content.Children.Add(BuildMessageActions(index, user, text));
        addContent?.Invoke(content);
        bubble.Child = content;
        ConversationMessages.Children.Add(bubble);
        ConversationScroll.ChangeView(null, double.MaxValue, null);
        return body;
    }

    private void RenderAssistantMessage(TextBlock output, string markdown, IReadOnlyList<ChatSource>? sources = null)
    {
        if (output.Parent is not StackPanel messageContent) return;
        var index = messageContent.Children.IndexOf(output);
        if (index < 0) return;
        messageContent.Children.RemoveAt(index);
        messageContent.Children.Insert(index, MarkdownUiRenderer.Render(markdown));
        if (sources is { Count: > 0 }) messageContent.Children.Insert(index + 1, BuildSourcesPanel(sources));
        var assistantIndex = _messages.FindLastIndex(message => message.Role == "assistant");
        if (assistantIndex >= 0) messageContent.Children.Add(BuildMessageActions(assistantIndex, false, markdown));
        ConversationScroll.ChangeView(null, double.MaxValue, null);
    }

    private StackPanel BuildMessageActions(int messageIndex, bool user, string content)
    {
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 3, 0, 0) };
        if (user)
        {
            var edit = new Button
            {
                Content = "Edit & resend", Tag = messageIndex, Padding = new Thickness(8, 4, 8, 4),
                Style = UiTheme.Style("ContextButtonStyle")
            };
            AutomationProperties.SetName(edit, "Edit and resend this message");
            edit.Click += EditMessageClick;
            actions.Children.Add(edit);
            return actions;
        }
        if (!string.IsNullOrWhiteSpace(content))
        {
            var copy = new Button
            {
                Content = "Copy response", Tag = content, Padding = new Thickness(8, 4, 8, 4),
                Style = UiTheme.Style("ContextButtonStyle")
            };
            AutomationProperties.SetName(copy, "Copy full assistant response");
            copy.Click += CopyMessageClick;
            actions.Children.Add(copy);
        }
        if (messageIndex == _messages.Count - 1 && messageIndex > 0 && _messages.Take(messageIndex).Any(message => message.Role == "user"))
        {
            var retry = new Button
            {
                Content = "Retry", Tag = messageIndex, Padding = new Thickness(8, 4, 8, 4),
                Style = UiTheme.Style("ContextButtonStyle")
            };
            AutomationProperties.SetName(retry, "Retry the latest response");
            retry.Click += RetryMessageClick;
            actions.Children.Add(retry);
        }
        return actions;
    }

    private void CopyMessageClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string content }) return;
        try
        {
            var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
            package.SetText(content);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
            RuntimeStatus.Text = "Response copied.";
        }
        catch (Exception ex) { RuntimeStatus.Text = $"Could not copy response: {ex.Message}"; }
    }

    private void EditMessageClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: int messageIndex }) return;
        BeginEditingMessage(messageIndex, retryImmediately: false);
    }

    private void RetryMessageClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: int assistantIndex }) return;
        if (_requestCancellation is not null)
        {
            RuntimeStatus.Text = "Wait for the current response to finish before retrying.";
            return;
        }
        var userIndex = _messages.Take(assistantIndex).Select((message, index) => (message, index))
            .Where(item => item.message.Role == "user").Select(item => item.index).LastOrDefault(-1);
        if (userIndex < 0)
        {
            RuntimeStatus.Text = "There is no user prompt to retry.";
            return;
        }
        BeginEditingMessage(userIndex, retryImmediately: true);
    }

    private void BeginEditingMessage(int messageIndex, bool retryImmediately)
    {
        if (_requestCancellation is not null)
        {
            RuntimeStatus.Text = "Wait for the current response to finish before editing a message.";
            return;
        }
        if (_pendingAttachments.Count > 0 || !string.IsNullOrWhiteSpace(PromptBox.Text))
        {
            RuntimeStatus.Text = "Send or clear the draft and files waiting in the composer before editing a saved message.";
            return;
        }
        if (messageIndex < 0 || messageIndex >= _messages.Count || _messages[messageIndex].Role != "user") return;
        var message = _messages[messageIndex];
        _pendingAttachments.AddRange(message.Attachments ?? []);
        RenderPendingAttachments();
        PromptBox.Text = message.Content;
        _editingMessageIndex = messageIndex;
        UpdateEditingControls();
        RuntimeStatus.Text = retryImmediately
            ? "Retrying the latest answer…"
            : "Editing an earlier message. Sending it will replace this turn and later turns.";
        PromptBox.Focus(FocusState.Programmatic);
        if (retryImmediately) SendClick(this, new RoutedEventArgs());
    }

    private void RebuildConversationView()
    {
        ConversationMessages.Children.Clear();
        for (var index = 0; index < _messages.Count; index++)
        {
            var message = _messages[index];
            AddMessage(message.Role == "user" ? "You" : _mode == WorkspaceModes.CodingAgent ? "Coding Agent" : ProductIdentity.ShortName, message.Content, message.Role == "user", message.Attachments, message.Sources, index, showActions: true);
        }
        WelcomePanel.Visibility = _messages.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ConversationScroll.Visibility = _messages.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private StackPanel BuildSourcesPanel(IReadOnlyList<ChatSource> sources)
    {
        var validSources = sources.Where(source => source is not null
            && !string.IsNullOrWhiteSpace(source.Title)
            && Uri.TryCreate(source.Url, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https").Take(6).ToArray();
        var panel = new StackPanel { Spacing = 7 };
        panel.Children.Add(new TextBlock
        {
            Text = $"Web search results · {validSources.Length}",
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = UiTheme.Brush("MutedInkBrush")
        });
        foreach (var source in validSources)
        {
            if (!Uri.TryCreate(source.Url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) continue;
            var sourceContent = new StackPanel { Spacing = 3 };
            sourceContent.Children.Add(new TextBlock
            {
                Text = source.Title,
                FontSize = 13,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = UiTheme.Brush("AccentInkBrush"),
                TextWrapping = TextWrapping.Wrap
            });
            sourceContent.Children.Add(new TextBlock
            {
                Text = uri.Host,
                FontSize = 10,
                Foreground = UiTheme.Brush("MutedInkBrush"),
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            sourceContent.Children.Add(new TextBlock
            {
                Text = source.Snippet,
                FontSize = 12,
                Foreground = UiTheme.Brush("InkBrush"),
                TextWrapping = TextWrapping.Wrap
            });
            var button = new Button
            {
                Content = new Border
                {
                    Background = UiTheme.Brush("SurfaceAltBrush"),
                    BorderBrush = UiTheme.Brush("LineSoftBrush"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(9),
                    Padding = new Thickness(10, 8, 10, 8),
                    Child = sourceContent
                },
                Tag = uri,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(0),
                Background = UiTheme.Brush("SurfaceBrush"),
                CornerRadius = new CornerRadius(9)
            };
            AutomationProperties.SetName(button, $"Open web source: {source.Title} · {uri.Host}");
            button.Click += async (_, _) =>
            {
                if (button.Tag is not Uri target || target.Scheme is not ("http" or "https")) return;
                try { await Windows.System.Launcher.LaunchUriAsync(target); }
                catch { RuntimeStatus.Text = $"Could not open {target.Host}."; }
            };
            panel.Children.Add(button);
        }
        if (validSources.Length > 0)
            panel.Children.Add(new TextBlock
            {
                Text = "Search snippets can be incomplete. Open a source to check its full context.",
                FontSize = 10,
                Foreground = UiTheme.Brush("MutedInkBrush"),
                TextWrapping = TextWrapping.Wrap
            });
        return panel;
    }
}
