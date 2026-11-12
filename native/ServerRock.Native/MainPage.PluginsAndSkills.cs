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
using ServerRock.Core.Models;
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
        var installedList = new ListView { MinHeight = 80, MaxHeight = 180, SelectionMode = ListViewSelectionMode.Single, DisplayMemberPath = nameof(InstalledSkill.Name) };
        var details = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 12 };
        var run = new Button { Content = "Use skill", IsEnabled = false, MinWidth = 100 };
        var enable = new Button { Content = "Enable selected", IsEnabled = false, MinWidth = 140 };
        var install = new Button { Content = "Install local package", MinWidth = 170 };
        var rollback = new Button { Content = "Rollback selected", IsEnabled = false, MinWidth = 150 };
        var status = new TextBlock { FontSize = 11, TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 92, 111, 116)) };
        AutomationProperties.SetName(installedList, "Installed skills");
        AutomationProperties.SetName(install, "Install local skill package");
        AutomationProperties.SetHelpText(install, "Choose a local skill package containing a manifest and Markdown instructions. Scripts are rejected and never executed.");
        AutomationProperties.SetName(rollback, "Rollback selected installed skill");
        AutomationProperties.SetHelpText(rollback, "Restore the previous atomically activated version of the selected installed skill.");
        AutomationProperties.SetName(enable, "Enable or disable selected skill");
        AutomationProperties.SetHelpText(enable, "A skill must be explicitly enabled before its Markdown instructions can be loaded into the composer.");
        void RefreshInstalled() => installedList.ItemsSource = _skillPackageInstaller.ListInstalled();
        list.SelectionChanged += (_, _) =>
        {
            installedList.SelectedItem = null;
            rollback.IsEnabled = false;
            enable.IsEnabled = false;
            if (list.SelectedItem is not SkillDefinition skill) { run.IsEnabled = false; details.Text = string.Empty; return; }
            details.Text = $"{skill.Name}\n\n{skill.Description}\n\nPermissions: {string.Join(", ", skill.RequiredPermissions)}";
            run.IsEnabled = true;
        };
        installedList.SelectionChanged += (_, _) =>
        {
            list.SelectedItem = null;
            if (installedList.SelectedItem is not InstalledSkill skill) { rollback.IsEnabled = false; enable.IsEnabled = false; details.Text = string.Empty; return; }
            details.Text = $"{skill.Name} · v{skill.Version}\n\nPublisher: {skill.Publisher}\nSource: {skill.SourceRef}\nDigest: {skill.PackageSha256}\nPermissions: {string.Join(", ", skill.RequiredPermissions)}\nState: {(skill.Enabled ? "enabled" : "disabled; explicit enablement required")}\n\nInstalled package content is treated as instructions and is never executed automatically.";
            rollback.IsEnabled = true;
            enable.IsEnabled = true;
            enable.Content = skill.Enabled ? "Disable selected" : "Enable selected";
            run.IsEnabled = skill.Enabled;
        };
        enable.Click += (_, _) =>
        {
            if (installedList.SelectedItem is not InstalledSkill skill) return;
            try
            {
                var updated = _skillPackageInstaller.SetEnabled(skill.Id, !skill.Enabled);
                status.Text = updated.Enabled ? $"Enabled {updated.Name}. Review its permissions before use." : $"Disabled {updated.Name}. It cannot be loaded until enabled again.";
                RefreshInstalled();
                installedList.SelectedItem = updated;
            }
            catch (Exception ex) { status.Text = $"Skill permission state could not be changed: {ex.Message}"; }
        };
        run.Click += (_, _) =>
        {
            if (list.SelectedItem is SkillDefinition builtIn)
            {
                PromptBox.Text = builtIn.Prompt;
                PromptBox.Focus(FocusState.Programmatic);
                status.Text = $"Loaded {builtIn.Name}. Review the prompt, then send it when ready.";
                return;
            }
            if (installedList.SelectedItem is not InstalledSkill skill) return;
            try { PromptBox.Text = _skillPackageInstaller.ReadInstructions(skill.Id); }
            catch (Exception ex) { status.Text = $"Skill instructions could not be read: {ex.Message}"; return; }
            PromptBox.Focus(FocusState.Programmatic);
            status.Text = $"Loaded {skill.Name}. Review the prompt, then send it when ready.";
        };
        install.Click += async (_, _) =>
        {
            var folder = await PickWorkspaceFolderAsync();
            if (string.IsNullOrWhiteSpace(folder)) return;
            var confirmation = new ContentDialog
            {
                Title = "Install local skill package?",
                Content = "The package must contain a valid manifest and Markdown instructions. Executable files, links, and unsafe paths are rejected. The package will not be executed.",
                PrimaryButtonText = "Install",
                CloseButtonText = "Cancel",
                XamlRoot = XamlRoot
            };
            if (await confirmation.ShowAsync() != ContentDialogResult.Primary) return;
            try
            {
                var installed = _skillPackageInstaller.Install(folder, "local-folder:" + Path.GetFileName(folder));
                status.Text = $"Installed {installed.Name} v{installed.Version}. Review its permissions before using it.";
                RefreshInstalled();
            }
            catch (Exception ex) { status.Text = $"Skill package was not installed: {ex.Message}"; }
        };
        rollback.Click += async (_, _) =>
        {
            if (installedList.SelectedItem is not InstalledSkill skill) return;
            try
            {
                var restored = _skillPackageInstaller.Rollback(skill.Id);
                status.Text = $"Rolled back {restored.Name} to v{restored.Version}.";
                RefreshInstalled();
            }
            catch (Exception ex) { status.Text = $"Skill rollback failed: {ex.Message}"; }
            await Task.CompletedTask;
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
                    new TextBlock { Text = "INSTALLED LOCAL SKILLS", FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Opacity = 0.75 },
                    installedList,
                    details,
                    run,
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { install, enable, rollback } },
                    status
                }
            }
        };
        RefreshInstalled();
        await dialog.ShowAsync();
    }
}
