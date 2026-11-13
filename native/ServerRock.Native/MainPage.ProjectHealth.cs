// Offline project health scan and architecture map (added on main): runs
// LocalProjectHealthAnalyzer over the active project's folders without any model or network call,
// shows the compact Markdown report, and can add it to the composer.
// Depends on: LocalProjectHealthAnalyzer (SkaiDesktop.Core), ChooseProjectAsync (MainPage.Projects.cs).
// Called by: /health, /audit, /map (MainPage.SlashCommands.cs) and the sidebar More menu.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private async Task ShowLocalHealthAsync(bool includeMap)
    {
        var project = _projects.FirstOrDefault(item => item.Id == _activeProjectId);
        if (project is null || project.FolderPaths.Count == 0)
        {
            RuntimeStatus.Text = "Choose a project with at least one local folder before running an offline health scan.";
            await ChooseProjectAsync();
            project = _projects.FirstOrDefault(item => item.Id == _activeProjectId);
            if (project is null || project.FolderPaths.Count == 0) return;
        }
        if (XamlRoot is null)
        {
            RuntimeStatus.Text = "The project health panel is unavailable until the window is ready.";
            return;
        }

        RuntimeStatus.Text = "Running bounded offline health scan…";
        var report = await Task.Run(() => _localHealthAnalyzer.Analyze(project.FolderPaths));
        var markdown = report.ToCompactMarkdown(project.Name, includeMap);

        var preview = new TextBox
        {
            Text = markdown,
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 320,
            MaxHeight = 540,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas")
        };
        AutomationProperties.SetName(preview, includeMap ? "Local architecture map" : "Local project health report");
        var dialog = new ContentDialog
        {
            Title = includeMap ? "Local architecture map" : "Offline project health",
            Content = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    new TextBlock { Text = "This scan is local, bounded, and token-free. It uses file names and small text samples; no repository contents were sent to a model.", TextWrapping = TextWrapping.Wrap },
                    preview
                }
            },
            PrimaryButtonText = "Add compact report",
            CloseButtonText = "Done",
            XamlRoot = XamlRoot
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            PromptBox.Text = string.IsNullOrWhiteSpace(PromptBox.Text) ? markdown : PromptBox.Text.TrimEnd() + "\n\n" + markdown;
            PromptBox.Focus(FocusState.Programmatic);
            PromptBox.SelectionStart = PromptBox.Text.Length;
            RuntimeStatus.Text = "Compact local report added to the composer. Review it before sending.";
        }
        else RuntimeStatus.Text = $"Offline scan complete · {report.Files:N0} files checked without using model tokens.";
    }
}
