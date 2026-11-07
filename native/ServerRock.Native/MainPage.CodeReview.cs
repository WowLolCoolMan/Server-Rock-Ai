// Code Review Agent intake (added on main): asks for a folder, ZIP, or GitHub URL and a model,
// switches to Coding Agent, and pre-fills a review prompt for the user to check and send.
// It does not fetch or run anything itself.
// Depends on: SwitchModeAsync (MainPage.Navigation.cs). Called by: the sidebar More menu.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ServerRock.Core.Models;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private async Task OpenCodeReviewAgentAsync()
    {
        var source = new TextBox { PlaceholderText = "Folder path, ZIP path, or GitHub repository URL", TextWrapping = TextWrapping.Wrap };
        var model = new TextBox { Text = _settings.Model, PlaceholderText = "Model ID", TextWrapping = TextWrapping.Wrap };
        var note = new TextBlock { Text = "The review agent will inspect the supplied source, critique architecture, correctness, security, tests, and release posture, and return findings for your approval. The current native slice prepares the review request; it does not fetch remote repositories automatically.", TextWrapping = TextWrapping.Wrap, FontSize = 12, Opacity = 0.82 };
        var panel = new StackPanel { Spacing = 10, Children = { note, new TextBlock { Text = "Review source", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold }, source, new TextBlock { Text = "Use model", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold }, model } };
        var dialog = new ContentDialog { Title = "Code Review Agent", Content = panel, PrimaryButtonText = "Prepare review", CloseButtonText = "Cancel", XamlRoot = XamlRoot };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(source.Text)) return;
        await SwitchModeAsync(WorkspaceModes.CodingAgent);
        PromptBox.Text = $"Act as a specialty code review agent. Review this source completely: {source.Text.Trim()}\n\nUse model preference: {model.Text.Trim()}\n\nCritique everything relevant: requirements/spec alignment, correctness, architecture, readability, security, performance, tests, packaging, and release readiness. Separate concrete blockers from optional improvements. Do not modify files without explicit approval.";
        PromptBox.Focus(FocusState.Programmatic);
        RuntimeStatus.Text = "Code Review Agent request prepared. Review it before sending.";
    }
}
