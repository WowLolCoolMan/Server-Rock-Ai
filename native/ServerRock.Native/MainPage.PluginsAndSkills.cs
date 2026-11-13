// The Plugins dialog (catalog of integrations, built in vs not available yet) and the Skills
// dialog (built-in read-only workflows such as reverse-engineer).
// Depends on: PluginCatalogService, SkillCatalogService (ServerRock.Core).
// Called by: the sidebar Plugins button in MainPage.xaml and the /plugins and /skills slash commands.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using ServerRock.Core.Infrastructure;
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
        var reviewOpenApi = new Button { Content = "Review local OpenAPI schema", MinWidth = 220 };
        AutomationProperties.SetName(reviewOpenApi, "Review local OpenAPI schema");
        AutomationProperties.SetHelpText(reviewOpenApi, "Choose a local JSON OpenAPI 3 document. The app only discovers and displays operations; it does not fetch, execute, or authorize them.");
        var localOpenApiPath = new TextBox { Header = "Local JSON path", PlaceholderText = "C:\\path\\openapi.json" };
        AutomationProperties.SetName(localOpenApiPath, "Local OpenAPI JSON path");
        var reviewOpenApiPath = new Button { Content = "Use local path", MinWidth = 140 };
        AutomationProperties.SetName(reviewOpenApiPath, "Review OpenAPI JSON from local path");
        AutomationProperties.SetHelpText(reviewOpenApiPath, "Read and review a bounded local JSON schema when the native file picker is unavailable.");
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
                    new TextBlock { Text = "OPENAPI DISCOVERY", Style = UiTheme.Style("SectionLabelStyle"), Margin = new Thickness(0, 8, 0, 0) },
                    new TextBlock { Text = "Review a local OpenAPI 3 JSON schema without connecting to its server. Discovered operations remain untrusted until a future explicit permission and execution flow exists.", TextWrapping = TextWrapping.Wrap, Foreground = UiTheme.Brush("MutedInkBrush") },
                    reviewOpenApi,
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { localOpenApiPath, reviewOpenApiPath } },
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
        async Task ShowOpenApiReviewAsync(IReadOnlyList<OpenApiDiscoveredOperation> operations, string fileName)
        {
            var results = UiTheme.WrapListText(new ListView { MinHeight = 180, MaxHeight = 300, SelectionMode = ListViewSelectionMode.Single, DisplayMemberPath = nameof(OpenApiDiscoveredOperation.Name) });
            var operationDetails = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = UiTheme.FontSize("SmallFontSize"), Foreground = UiTheme.Brush("InkBrush"), IsTextSelectionEnabled = true };
            AutomationProperties.SetName(results, "OpenAPI operation results");
            AutomationProperties.SetName(operationDetails, "OpenAPI operation details");
            results.ItemsSource = operations;
            results.SelectionChanged += (_, _) =>
            {
                if (results.SelectedItem is not OpenApiDiscoveredOperation operation) return;
                operationDetails.Text = $"{operation.Method} {operation.Path}\n{operation.Description}\nCapability: {string.Join(", ", operation.RequiredCapabilities)}\nMutating: {(operation.IsMutating ? "yes" : "no")}\nInput schema: {operation.InputSchemaJson}";
            };
            var reviewDialog = new ContentDialog
            {
                Title = $"OpenAPI operations · {fileName}",
                Content = new StackPanel
                {
                    Spacing = 10,
                    Children =
                    {
                        new TextBlock { Text = $"Discovered {operations.Count} operation(s). This is an untrusted local schema review; nothing was fetched, executed, or authorized.", TextWrapping = TextWrapping.Wrap, Foreground = UiTheme.Brush("MutedInkBrush") },
                        results,
                        operationDetails
                    }
                },
                CloseButtonText = "Done",
                XamlRoot = XamlRoot
            };
            UiTheme.Standardize(reviewDialog);
            await reviewDialog.ShowAsync();
        }
        async Task ReviewOpenApiStreamAsync(Stream stream, string fileName, long size)
        {
            if (size > (long)OpenApiToolDiscovery.MaxDocumentCharacters * 4)
            {
                status.Text = $"OpenAPI schema {fileName} exceeds the bounded input limit.";
                return;
            }
            var json = await BoundedFileReader.ReadTextAsync(stream, OpenApiToolDiscovery.MaxDocumentCharacters, "OpenAPI schema");
            var operations = OpenApiToolDiscovery.Parse(json, Path.GetFileName(fileName));
            status.Text = $"Discovered {operations.Count} OpenAPI operation(s) from {Path.GetFileName(fileName)}. Nothing was fetched, executed, or authorized.";
            await ShowOpenApiReviewAsync(operations, Path.GetFileName(fileName));
        }
        reviewOpenApiPath.Click += async (_, _) =>
        {
            try
            {
                var rawPath = localOpenApiPath.Text.Trim();
                if (rawPath.Length is 0 or > 1_024 || rawPath.Any(char.IsControl)) throw new InvalidOperationException("Enter a bounded local path.");
                if (!Path.IsPathRooted(rawPath)) throw new InvalidOperationException("Enter a fully qualified local path.");
                var path = Path.GetFullPath(rawPath);
                if (!Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("The local OpenAPI file must use the .json extension.");
                if (!File.Exists(path)) throw new FileNotFoundException("The local OpenAPI file does not exist.");
                var attributes = File.GetAttributes(path);
                if (attributes.HasFlag(System.IO.FileAttributes.ReparsePoint)) throw new InvalidOperationException("Reparse-point files are not reviewed.");
                await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
                await ReviewOpenApiStreamAsync(stream, Path.GetFileName(path), stream.Length);
            }
            catch (Exception ex) { status.Text = $"OpenAPI schema could not be reviewed: {ex.Message}"; }
        };
        reviewOpenApi.Click += async (_, _) =>
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker
                {
                    SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
                    ViewMode = Windows.Storage.Pickers.PickerViewMode.List
                };
                picker.FileTypeFilter.Add(".json");
#if WINDOWS || SRAI_DESKTOP
                var window = (Application.Current as App)?.MainWindow;
                if (window is null) throw new InvalidOperationException("The app window is unavailable.");
                WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(window));
#endif
                await Task.Delay(50);
                var file = await picker.PickSingleFileAsync();
                if (file is null) return;
                var properties = await file.GetBasicPropertiesAsync();
                await using var stream = await file.OpenStreamForReadAsync();
                await ReviewOpenApiStreamAsync(stream, file.Name, (long)properties.Size);
            }
            catch (Exception ex) { status.Text = $"OpenAPI schema could not be reviewed: {ex.Message}"; }
        };
        Refresh();
        await dialog.ShowAsync();
    }

    private async Task SkillsDialogAsync()
    {
        var list = new ListView { MinHeight = 260, MaxHeight = 420, SelectionMode = ListViewSelectionMode.Single, DisplayMemberPath = nameof(SkillDefinition.Name) };
        list.ItemsSource = SkillCatalogService.BuiltIns;
        var installedList = new ListView { MinHeight = 80, MaxHeight = 180, SelectionMode = ListViewSelectionMode.Single, DisplayMemberPath = nameof(InstalledSkill.Name) };
        var details = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = UiTheme.FontSize("SmallFontSize"), Foreground = UiTheme.Brush("InkBrush") };
        var run = new Button { Content = "Use skill", IsEnabled = false, MinWidth = 100 };
        var enable = new Button { Content = "Enable selected", IsEnabled = false, MinWidth = 140 };
        var install = new Button { Content = "Install local package", MinWidth = 170 };
        var rollback = new Button { Content = "Rollback selected", IsEnabled = false, MinWidth = 150 };
        var status = new TextBlock { FontSize = UiTheme.FontSize("CaptionFontSize"), TextWrapping = TextWrapping.Wrap, Foreground = UiTheme.Brush("MutedInkBrush") };
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
                PromptBox.Text = PromptVariableExpander.Expand(builtIn.Prompt, DateTimeOffset.Now, _settings.PromptUserName);
                PromptBox.Focus(FocusState.Programmatic);
                status.Text = $"Loaded {builtIn.Name}. Review the prompt, then send it when ready.";
                return;
            }
            if (installedList.SelectedItem is not InstalledSkill skill) return;
            try { PromptBox.Text = PromptVariableExpander.Expand(_skillPackageInstaller.ReadInstructions(skill.Id), DateTimeOffset.Now, _settings.PromptUserName); }
            catch (Exception ex) { status.Text = $"Skill instructions could not be read: {ex.Message}"; return; }
            PromptBox.Focus(FocusState.Programmatic);
            status.Text = $"Loaded {skill.Name}. Review the prompt, then send it when ready.";
        };
        install.Click += async (_, _) =>
        {
            var folder = await PickWorkspaceFolderAsync();
            if (string.IsNullOrWhiteSpace(folder)) return;
            SkillPackagePreview preview;
            try { preview = _skillPackageInstaller.Preview(folder); }
            catch (Exception ex) { status.Text = $"Skill package preview failed: {ex.Message}"; return; }
            var confirmation = new ContentDialog
            {
                Title = preview.IsUpdate ? $"Review skill update · {preview.Name}" : $"Review skill package · {preview.Name}",
                Content = $"Version: {preview.Version}{(preview.IsUpdate ? $" (replaces {preview.ExistingVersion})" : string.Empty)}\nPublisher: {preview.Publisher}\nDigest: {preview.PackageSha256}\nPermissions: {string.Join(", ", preview.RequiredPermissions)}\n\nThe package contains Markdown instructions only. Executable files, links, and unsafe paths are rejected. Nothing is executed automatically. Review this preview before installing; the package will remain disabled until explicitly enabled.",
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
                    new TextBlock { Text = "INSTALLED LOCAL SKILLS", FontSize = UiTheme.FontSize("SectionLabelFontSize"), FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = UiTheme.Brush("MutedInkBrush") },
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
