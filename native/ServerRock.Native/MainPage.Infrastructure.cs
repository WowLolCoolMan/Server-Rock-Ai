// MainPage infrastructure shared by feature partials: status classification,
// guarded UI actions, settings persistence, and the actionable-status history dialog.
// Keeping these cross-cutting seams out of MainPage.xaml.cs makes the page state/lifecycle
// file easier to inspect without changing event names, persistence behavior, or feature APIs.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private void DismissRuntimeStatusClick(object sender, RoutedEventArgs e) => RuntimeStatus.Text = string.Empty;

    private async void StatusHistoryClick(object sender, RoutedEventArgs e)
    {
        var results = new StackPanel { Spacing = 8 };
        AutomationProperties.SetName(results, "Status history results");
        foreach (var message in _statusHistory.Snapshot())
            results.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap });
        var historyScroll = new ScrollViewer { Content = results, MaxHeight = 420, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, IsTabStop = true };
        AutomationProperties.SetName(historyScroll, "Status history results scroll region");
        var dialog = new ContentDialog
        {
            Title = "Status history",
            Content = historyScroll,
            CloseButtonText = "Done",
            XamlRoot = XamlRoot
        };
        AutomationProperties.SetName(dialog, "Status history");
        dialog.Opened += (_, _) => DispatcherQueue.TryEnqueue(() => historyScroll.Focus(FocusState.Programmatic));
        await dialog.ShowAsync();
    }

    private static bool IsActionableRuntimeStatus(string message) =>
        new[] { "could not", "cannot", "failed", "error", "unknown", "unavailable", "wait for", "timed out", "timeout", "expired", "no readable", "no results", "not available", "not saved", "refused", "rejected", "requires", "enter ", "choose ", "add a project", "not connected", "check settings", "not enabled", "stopped", "preserve newer", "protected" }
            .Any(marker => message.Contains(marker, StringComparison.OrdinalIgnoreCase));

    private async Task RunUiActionAsync(Func<Task> action)
    {
        try { await action(); }
        catch (Exception ex)
        {
            App.Log.Error("UI", ex, "UI action failed");
            RuntimeStatus.Text = $"Action failed: {ex.Message}";
        }
    }

    private bool SaveSettings()
    {
        try
        {
            _settingsStore.Save(_settings);
            return true;
        }
        catch (Exception ex)
        {
            RuntimeStatus.Text = $"Could not save settings: {ex.Message}";
            return false;
        }
    }

    private void PersistWorkspaceDrafts()
    {
        if (_settings is null) return;
        _settings.WorkspaceDrafts = _workspaceDrafts.Snapshot();
        SaveSettings();
    }

    private void ScheduleWorkspaceDraftPersistence()
    {
        _draftPersistenceTimer.Stop();
        _draftPersistenceTimer.Start();
    }
}
