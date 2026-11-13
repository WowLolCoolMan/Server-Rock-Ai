using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using SkaiDesktop.Core.Models;
using SkaiDesktop.Core.Services;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private sealed record CommandPaletteItem(CommandPaletteAction Action, Func<Task> Execute)
    {
        public string Title => Action.Title;
    }

    private async Task OpenCommandPaletteAsync()
    {
        var commands = CommandPaletteCatalog.Actions
            .Select(action => new CommandPaletteItem(action, ResolveCommandPaletteAction(action.Id)))
            .ToArray();

        var query = new TextBox { PlaceholderText = "Search actions", MinWidth = 420 };
        var results = new ListView { MaxHeight = 360, SelectionMode = ListViewSelectionMode.Single };
        AutomationProperties.SetName(query, "Search command palette actions");
        AutomationProperties.SetName(results, "Command palette results");
        ContentDialog? dialog = null;
        CommandPaletteItem? selected = null;

        void RefreshResults()
        {
            var text = query.Text.Trim();
            results.Items.Clear();
            foreach (var command in commands.Where(item => CommandPaletteCatalog.Search(text).Any(action => action.Id == item.Action.Id)))
            {
                var item = new ListViewItem { Content = command.Title, Tag = command };
                AutomationProperties.SetName(item, command.Title);
                results.Items.Add(item);
            }
            if (results.Items.Count > 0) results.SelectedIndex = 0;
        }

        query.TextChanged += (_, _) => RefreshResults();
        results.SelectionChanged += (_, _) => selected = (results.SelectedItem as ListViewItem)?.Tag as CommandPaletteItem;
        RefreshResults();

        dialog = new ContentDialog
        {
            Title = "Command palette",
            Content = new StackPanel { Spacing = 10, Children = { query, results } },
            PrimaryButtonText = "Run action",
            CloseButtonText = "Close",
            XamlRoot = XamlRoot
        };
        dialog.Opened += (_, _) => query.Focus(FocusState.Programmatic);
        dialog.Closed += (_, _) => PromptBox.Focus(FocusState.Programmatic);
        if (await dialog.ShowAsync() == ContentDialogResult.Primary && selected is not null)
            await selected.Execute();
    }

    private Func<Task> ResolveCommandPaletteAction(string id) => id switch
    {
        "new-chat" => () => { NewChatClick(this, new RoutedEventArgs()); return Task.CompletedTask; },
        "search-conversations" => () => { SearchClick(this, new RoutedEventArgs()); return Task.CompletedTask; },
        "search-current-chat" => SearchCurrentConversationAsync,
        "review-memory" => ReviewMemoryContextAsync,
        "activity" => () => { ScheduledTasksClick(this, new RoutedEventArgs()); return Task.CompletedTask; },
        "models" => () => RunUiActionAsync(ModelClickAsync),
        "projects" => () => { NewProjectClick(this, new RoutedEventArgs()); return Task.CompletedTask; },
        "chat-workspace" => () => SwitchModeFromSidebarAsync(WorkspaceModes.Chat),
        "coding-workspace" => () => SwitchModeFromSidebarAsync(WorkspaceModes.CodingAgent),
        "quick-open" => OpenQuickFileAsync,
        "documents-only" => () => { DocumentsOnlyClick(this, new RoutedEventArgs()); return Task.CompletedTask; },
        "github" => () => { GitHubClick(this, new RoutedEventArgs()); return Task.CompletedTask; },
        "images" => () => { ImageStudioClick(this, new RoutedEventArgs()); return Task.CompletedTask; },
        "skills" => () => RunUiActionAsync(PluginStoreClickAsync),
        "settings" => () => { SettingsClick(this, new RoutedEventArgs()); return Task.CompletedTask; },
        "code-review" => OpenCodeReviewAgentAsync,
        "health" => () => ShowLocalHealthAsync(includeMap: false),
        "documentation" => ShowInAppDocumentationAsync,
        "snippets" => OpenSnippetsAsync,
        "personas" => OpenPersonasAsync,
        "presets" => OpenPresetsAsync,
        "translate" => OpenTranslatorAsync,
        "quicklinks" => OpenQuicklinksAsync,
        "developer-diagnostics" => OpenDeveloperDiagnosticsAsync,
        _ => () => Task.CompletedTask
    };
}
