using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using ServerRock.Core.Services;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private async Task ReviewMemoryContextAsync()
    {
        IReadOnlyList<ServerRock.Core.Models.MemoryEntry> entries;
        try { entries = _memoryStore.Load(); }
        catch (Exception exception)
        {
            RuntimeStatus.Text = $"Saved memory could not be loaded: {exception.Message}";
            return;
        }
        var projectName = _projects.FirstOrDefault(item => item.Id == _activeProjectId)?.Name;
        var injection = new CheckBox { Content = "Allow approved memory injection", IsChecked = _settings.MemoryInjectionEnabled };
        AutomationProperties.SetName(injection, "Allow approved memory injection");
        AutomationProperties.SetHelpText(injection, "When enabled, only enabled global or active-project notes are added as untrusted reference context to the next model request.");
        var previewText = MemoryPromptContext.Build(entries, true, projectName)
            ?? "No enabled global or active-project notes are eligible for this chat.";
        var preview = new TextBox { Text = previewText, IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MaxHeight = 260, MinWidth = 460 };
        AutomationProperties.SetName(preview, "Approved memory context preview");
        var disclosure = new TextBlock
        {
            Text = "This preview is local and read-only. Notes are untrusted reference material, never instructions, and are sent only when you explicitly enable injection and send a prompt.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = UiTheme.FontSize("SmallFontSize"),
            Foreground = UiTheme.Brush("MutedInkBrush")
        };
        var dialog = new ContentDialog
        {
            Title = "Review approved memory context",
            Content = new StackPanel { Spacing = 10, Children = { disclosure, injection, preview } },
            PrimaryButtonText = "Save memory setting",
            CloseButtonText = "Cancel",
            XamlRoot = XamlRoot
        };
        dialog.Opened += (_, _) => injection.Focus(FocusState.Programmatic);
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        _settings.MemoryInjectionEnabled = injection.IsChecked == true;
        SaveSettings();
        RuntimeStatus.Text = _settings.MemoryInjectionEnabled
            ? "Approved memory injection enabled. Review the local context before sending your next prompt."
            : "Approved memory injection disabled; saved notes will stay out of model requests.";
    }
}
