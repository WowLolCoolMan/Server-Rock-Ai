using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using ServerRock.Core.Models;

namespace ServerRock.Native;

public sealed partial class SplitConversationView : UserControl
{
    private bool _updatingPicker;
    private bool _updatingRatio;

    public event EventHandler<string?>? ComparisonChanged;
    public event EventHandler<double>? RatioChanged;

    public SplitConversationView()
    {
        InitializeComponent();
    }

    public double Ratio => SplitRatioSlider.Value;

    public void SetRatio(double ratio)
    {
        _updatingRatio = true;
        SplitRatioSlider.Value = Math.Clamp(ratio, SplitRatioSlider.Minimum, SplitRatioSlider.Maximum);
        _updatingRatio = false;
        ApplyRatio();
    }

    public void Render(
        string currentTitle,
        IReadOnlyList<ChatMessage> currentMessages,
        IReadOnlyList<ChatSession> comparisonSessions,
        string? selectedComparisonId)
    {
        CurrentTitle.Text = string.IsNullOrWhiteSpace(currentTitle) ? "Current chat" : currentTitle;
        RenderMessages(CurrentMessages, currentMessages, "Current chat is empty.");

        _updatingPicker = true;
        ComparisonPicker.Items.Clear();
        foreach (var session in comparisonSessions)
        {
            var item = new ComboBoxItem { Content = session.Title, Tag = session.Id };
            AutomationProperties.SetName(item, $"Compare chat: {session.Title}");
            ComparisonPicker.Items.Add(item);
        }

        var selectedIndex = comparisonSessions
            .Select((session, index) => (session.Id, index))
            .FirstOrDefault(item => string.Equals(item.Id, selectedComparisonId, StringComparison.Ordinal))
            .index;
        if (comparisonSessions.Count > 0 && (selectedComparisonId is null || selectedIndex == 0 && !string.Equals(comparisonSessions[0].Id, selectedComparisonId, StringComparison.Ordinal)))
            selectedIndex = 0;
        ComparisonPicker.SelectedIndex = comparisonSessions.Count == 0 ? -1 : selectedIndex;
        ComparisonPicker.IsEnabled = comparisonSessions.Count > 0;
        _updatingPicker = false;

        var selected = comparisonSessions.FirstOrDefault(session => session.Id == (ComparisonPicker.SelectedItem as ComboBoxItem)?.Tag as string);
        ComparisonEmpty.Visibility = selected is null ? Visibility.Visible : Visibility.Collapsed;
        ComparisonScroll.Visibility = selected is null ? Visibility.Collapsed : Visibility.Visible;
        RenderMessages(ComparisonMessages, selected?.Messages ?? [], "Select a saved chat to compare.");
        ApplyRatio();
    }

    private void SplitRatioChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        ApplyRatio();
        if (!_updatingRatio) RatioChanged?.Invoke(this, SplitRatioSlider.Value);
    }

    private void ApplyRatio()
    {
        if (Root.ColumnDefinitions.Count < 3) return;
        var left = Math.Clamp(SplitRatioSlider.Value, 25, 75);
        Root.ColumnDefinitions[0].Width = new GridLength(left, GridUnitType.Star);
        Root.ColumnDefinitions[2].Width = new GridLength(100 - left, GridUnitType.Star);
    }

    private void ComparisonPickerChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingPicker) return;
        ComparisonChanged?.Invoke(this, (ComparisonPicker.SelectedItem as ComboBoxItem)?.Tag as string);
    }

    private static void RenderMessages(StackPanel target, IReadOnlyList<ChatMessage> messages, string emptyText)
    {
        target.Children.Clear();
        if (messages.Count == 0)
        {
            target.Children.Add(new TextBlock { Text = emptyText, TextWrapping = TextWrapping.Wrap, Foreground = UiTheme.Brush("MutedInkBrush") });
            return;
        }

        foreach (var message in messages)
        {
            var user = string.Equals(message.Role, "user", StringComparison.OrdinalIgnoreCase);
            var content = new StackPanel { Spacing = 5 };
            content.Children.Add(new TextBlock
            {
                Text = user ? "You" : ProductIdentity.ShortName,
                FontSize = UiTheme.FontSize("CaptionFontSize"),
                Foreground = UiTheme.Brush("AccentInkBrush")
            });
            if (message.Attachments is { Count: > 0 })
                content.Children.Add(new TextBlock
                {
                    Text = string.Join(" · ", message.Attachments.Select(item => (item.IsImage ? "▧ " : "▤ ") + item.FileName)),
                    FontSize = UiTheme.FontSize("CaptionFontSize"),
                    Foreground = UiTheme.Brush("AccentInkBrush"),
                    TextWrapping = TextWrapping.Wrap
                });
            if (!user && !string.IsNullOrWhiteSpace(message.Content))
                content.Children.Add(MarkdownUiRenderer.Render(message.Content));
            else
                content.Children.Add(new TextBlock
                {
                    Text = message.Content,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = UiTheme.FontSize("ReadingFontSize"),
                    Foreground = UiTheme.Brush("InkBrush"),
                    IsTextSelectionEnabled = true
                });

            var bubble = new Border
            {
                HorizontalAlignment = user ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                MaxWidth = 560,
                Background = UiTheme.Brush(user ? "AccentSoftBrush" : "SurfaceAltBrush"),
                BorderBrush = UiTheme.Brush("LineSoftBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 10, 12, 10),
                Child = content
            };
            target.Children.Add(bubble);
        }
    }
}
