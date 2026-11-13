using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SkaiDesktop.Core.Models;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private void SplitChatClick(object sender, RoutedEventArgs e)
    {
        if (_splitChatVisible)
        {
            CloseSplitChat();
            return;
        }

        _splitChatVisible = true;
        SplitChatButton.Content = "Close split";
        ToolTipService.SetToolTip(SplitChatButton, "Close split chat view");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(SplitChatButton, "Close split chat view");
        ConversationSurfaceControl.Visibility = Visibility.Collapsed;
        SplitConversationView.Visibility = Visibility.Visible;
        SplitConversationView.SetRatio(GetSavedSplitRatio());
        RefreshSplitChat();
    }

    private void CloseSplitChat()
    {
        _splitChatVisible = false;
        SplitConversationView.Visibility = Visibility.Collapsed;
        ConversationSurfaceControl.Visibility = Visibility.Visible;
        SplitChatButton.Content = "Split";
        ToolTipService.SetToolTip(SplitChatButton, "Open split chat view");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(SplitChatButton, "Open split chat view");
    }

    private void RefreshSplitChat()
    {
        if (!_splitChatVisible) return;
        var comparisons = _sessions
            .Where(session => session.Mode == _mode && session.Id != _activeSessionId && !session.Archived)
            .OrderByDescending(session => session.Pinned)
            .ThenByDescending(session => session.UpdatedAt)
            .ToArray();
        if (_splitComparisonSessionId is null || comparisons.All(session => session.Id != _splitComparisonSessionId))
            _splitComparisonSessionId = comparisons.FirstOrDefault()?.Id;
        SplitConversationView.Render(PageTitle.Text, _messages, comparisons, _splitComparisonSessionId);
    }

    private void SplitComparisonChanged(object? sender, string? sessionId)
    {
        _splitComparisonSessionId = sessionId;
        RefreshSplitChat();
    }

    private double GetSavedSplitRatio()
    {
        if (_settings.SplitChatRatios is not null && _settings.SplitChatRatios.TryGetValue(SplitLayoutKey, out var ratio))
            return Math.Clamp(ratio, 25, 75);
        return 50;
    }

    private void SplitRatioChanged(object? sender, double ratio)
    {
        _settings.SplitChatRatios ??= new(StringComparer.OrdinalIgnoreCase);
        _settings.SplitChatRatios[SplitLayoutKey] = Math.Clamp(ratio, 25, 75);
        SaveSettings();
    }
}
