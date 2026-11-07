// The Plugins dialog (catalog of integrations, built in vs not available yet) and the Skills
// dialog (built-in read-only workflows such as reverse-engineer).
// Depends on: PluginCatalogService, SkillCatalogService (ServerRock.Core).
// Called by: the sidebar Plugins button in MainPage.xaml and the /plugins and /skills slash commands.

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Media;
using ServerRock.Core.Services;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private async void PluginStoreClick(object sender, RoutedEventArgs e)
    {
        CloseCompactSidebar();
        await RunUiActionAsync(PluginStoreClickAsync);
    }

    // Plugin store, reworked so it is honest: entries that already work are marked "Built in"
    // and say where to find them; the others read "Not available yet". The old Install/Remove
    // button only saved a list nothing read, while reporting "Installed Gmail." It also had two
    // buttons (Done and Close) that did the same thing; it now has one.
    private async Task PluginStoreClickAsync()
    {
        var search = new TextBox { PlaceholderText = "Search plugins, extensions, or skills", MinWidth = 300 };
        var category = new ComboBox { MinWidth = 180 };
        category.Items.Add("All categories");
        foreach (var item in PluginCatalogService.Categories())
            category.Items.Add(item);
        category.SelectedIndex = 0;
        var list = UiTheme.WrapListText(new ListView { MinHeight = 220, MaxHeight = 320, SelectionMode = ListViewSelectionMode.Single, DisplayMemberPath = nameof(PluginDefinition.ListLabel) });
        AutomationProperties.SetName(list, "Plugins");
        var details = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = UiTheme.FontSize("SmallFontSize"), Foreground = UiTheme.Brush("InkBrush"), IsTextSelectionEnabled = true };
        AutomationProperties.SetLiveSetting(details, AutomationLiveSetting.Polite);
        var status = new TextBlock { FontSize = UiTheme.FontSize("CaptionFontSize"), Foreground = UiTheme.Brush("MutedInkBrush"), TextWrapping = TextWrapping.Wrap };
        var dialog = new ContentDialog
        {
            Title = "Plugins",
            CloseButtonText = "Done",
            XamlRoot = XamlRoot,
            Content = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    new TextBlock { Text = "Integrations marked Built in already work in this app. The others are not connected in this version; each entry shows what data it would access.", TextWrapping = TextWrapping.Wrap, Foreground = UiTheme.Brush("MutedInkBrush") },
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { search, category } },
                    list,
                    details,
                    status
                }
            }
        };
        UiTheme.Standardize(dialog);

        void Refresh()
        {
            var filtered = PluginCatalogService.Search(search.Text.Trim(), category.SelectedItem as string);
            list.ItemsSource = filtered;
            var builtIn = filtered.Count(item => item.IsBuiltIn);
            status.Text = $"{filtered.Count} listed · {builtIn} built in · {filtered.Count - builtIn} not available yet";
            details.Text = "Select an entry to see what it does and where to find it.";
        }

        search.TextChanged += (_, _) => Refresh();
        category.SelectionChanged += (_, _) => Refresh();
        list.SelectionChanged += (_, _) =>
        {
            if (list.SelectedItem is not PluginDefinition item) return;
            var where = item.IsBuiltIn
                ? $"Built in. {item.WhereToFind}"
                : "Not available yet. This integration is not connected in this version of the app.";
            details.Text = $"{item.Name} · {item.Description}\nAccess: {item.Access}\n{where}";
        };
        Refresh();
        await dialog.ShowAsync();
    }

    private async Task SkillsDialogAsync()
    {
        var list = new ListView { MinHeight = 260, MaxHeight = 420, SelectionMode = ListViewSelectionMode.Single, DisplayMemberPath = nameof(SkillDefinition.Name) };
        list.ItemsSource = SkillCatalogService.BuiltIns;
        var details = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 12 };
        var run = new Button { Content = "Use skill", IsEnabled = false, MinWidth = 100 };
        var status = new TextBlock { FontSize = 11, TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 92, 111, 116)) };
        list.SelectionChanged += (_, _) =>
        {
            if (list.SelectedItem is not SkillDefinition skill) { run.IsEnabled = false; details.Text = string.Empty; return; }
            details.Text = $"{skill.Name}\n\n{skill.Description}\n\nPermissions: {string.Join(", ", skill.RequiredPermissions)}";
            run.IsEnabled = true;
        };
        run.Click += (_, _) =>
        {
            if (list.SelectedItem is not SkillDefinition skill) return;
            PromptBox.Text = skill.Prompt;
            PromptBox.Focus(FocusState.Programmatic);
            status.Text = $"Loaded {skill.Name}. Review the prompt, then send it when ready.";
        };
        var dialog = new ContentDialog
        {
            Title = "Skills",
            PrimaryButtonText = "Done",
            CloseButtonText = "Close",
            XamlRoot = XamlRoot,
            Content = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    new TextBlock { Text = "Built-in skills are read-only workflow templates. Review the requested permissions before using one.", TextWrapping = TextWrapping.Wrap },
                    list,
                    details,
                    run,
                    status
                }
            }
        };
        await dialog.ShowAsync();
    }
}
