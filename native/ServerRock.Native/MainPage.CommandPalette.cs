using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using ServerRock.Core.Models;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private sealed record CommandPaletteItem(string Title, string Keywords, Func<Task> Execute);

    private async Task OpenCommandPaletteAsync()
    {
        var commands = new[]
        {
            new CommandPaletteItem("New chat", "new conversation", () => { NewChatClick(this, new RoutedEventArgs()); return Task.CompletedTask; }),
            new CommandPaletteItem("Search conversations", "find chats coding sessions", () => { SearchClick(this, new RoutedEventArgs()); return Task.CompletedTask; }),
            new CommandPaletteItem("Find in current chat", "search this conversation message", SearchCurrentConversationAsync),
            new CommandPaletteItem("Review memory context", "approved local notes injection", ReviewMemoryContextAsync),
            new CommandPaletteItem("Activity & tasks", "operations approvals history", () => { ScheduledTasksClick(this, new RoutedEventArgs()); return Task.CompletedTask; }),
            new CommandPaletteItem("Model store", "models downloads benchmark", () => RunUiActionAsync(ModelClickAsync)),
            new CommandPaletteItem("Projects & folders", "workspace project files", () => { NewProjectClick(this, new RoutedEventArgs()); return Task.CompletedTask; }),
            new CommandPaletteItem("Chat workspace", "switch mode conversation", () => SwitchModeFromSidebarAsync(WorkspaceModes.Chat)),
            new CommandPaletteItem("Coding Agent workspace", "switch mode code project", () => SwitchModeFromSidebarAsync(WorkspaceModes.CodingAgent)),
            new CommandPaletteItem("GitHub & source control", "repository git push pull", () => { GitHubClick(this, new RoutedEventArgs()); return Task.CompletedTask; }),
            new CommandPaletteItem("Images", "image studio generation", () => { ImageStudioClick(this, new RoutedEventArgs()); return Task.CompletedTask; }),
            new CommandPaletteItem("Extensions & skills", "plugins skills marketplace", () => RunUiActionAsync(PluginStoreClickAsync)),
            new CommandPaletteItem("Connection & settings", "preferences endpoint voice", () => { SettingsClick(this, new RoutedEventArgs()); return Task.CompletedTask; }),
            new CommandPaletteItem("Code Review Agent", "review zip repository", OpenCodeReviewAgentAsync),
            new CommandPaletteItem("Health scan", "architecture map diagnostics", () => ShowLocalHealthAsync(includeMap: false)),
            new CommandPaletteItem("In-app documentation", "help guide privacy safety local workflow", ShowInAppDocumentationAsync),
            new CommandPaletteItem("Prompt snippets", "saved prompts templates expand composer", OpenSnippetsAsync),
            new CommandPaletteItem("Translate text", "language translation review composer", OpenTranslatorAsync),
            new CommandPaletteItem("Quicklinks", "bookmarks frequent web links", OpenQuicklinksAsync),
        };

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
            foreach (var command in commands.Where(item => string.IsNullOrWhiteSpace(text)
                || item.Title.Contains(text, StringComparison.OrdinalIgnoreCase)
                || item.Keywords.Contains(text, StringComparison.OrdinalIgnoreCase)))
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
}
