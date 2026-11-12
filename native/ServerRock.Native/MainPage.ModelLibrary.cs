// Model picker and Model Library.
// - The composer model button (ModelClick) and its quick "hotswap" menu of pinned models.
// - The Model Library dialog (ModelLibraryClickAsync): sources are Hugging Face GGUF, installed
//   local models, the Ollama Library, and ModelScope GGUF; search, sort, paging, pin/unpin,
//   download/pull, delete, and a speed benchmark for installed models.
// - SelectModelAsync switches the active model/endpoint; UpdateModelLabel and DescribeDataRoute
//   keep the composer labels honest about local vs remote.
// Depends on: ModelLibraryService, OpenAiCompatibleProvider (ServerRock.Core), SaveSettings,
// RefreshModelConnectionAsync (MainPage.Connection.cs).
// Called by: the model button in MainPage.xaml, Settings > Models & downloads, the /model
// command, and many places that refresh the composer labels.

using System.Diagnostics;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Media;
using ServerRock.Core.Models;
using ServerRock.Core.Services;
using ServerRock.Core.Infrastructure;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private sealed record InstalledModelChoice(string Provider, string Model, string Endpoint, string Display);
    private sealed record CatalogModelChoice(HubModel Model, string Display);
    private sealed record ModelScopeGgufPackageChoice(ModelScopeGgufPackage Package, string Display);

    private async void ModelClick(object sender, RoutedEventArgs e)
    {
        var returnToCompactSidebar = _compactNavigation && Sidebar.Visibility == Visibility.Visible;
        CloseCompactSidebar();
        await OpenModelLibraryAndRestoreFocusAsync(returnToCompactSidebar, sender as Control);
    }

    private async Task ModelClickAsync() => await OpenModelLibraryAndRestoreFocusAsync(false, PromptBox);

    private void ModelHotswapClick(object sender, RoutedEventArgs e)
    {
        var menu = new MenuFlyout();
        var pinned = (_settings.PinnedModelConnections ?? []).ToList();
        foreach (var model in _settings.PinnedModels ?? [])
            if (!pinned.Any(item => item.Model.Equals(model, StringComparison.OrdinalIgnoreCase) && item.Endpoint.Equals(_settings.Endpoint, StringComparison.OrdinalIgnoreCase)))
                pinned.Add(new PinnedModelConnection(_settings.ModelProvider, model, _settings.Endpoint));
        var quickSwitchModels = pinned.DistinctBy(item => item.Model + "\n" + item.Endpoint, StringComparer.OrdinalIgnoreCase).ToList();
        if (quickSwitchModels.Count == 0)
            menu.Items.Add(new MenuFlyoutItem { Text = "No pinned models yet", IsEnabled = false });
        var currentIsPinned = quickSwitchModels.Any(item => item.Model.Equals(_settings.Model, StringComparison.OrdinalIgnoreCase) && item.Endpoint.Equals(_settings.Endpoint, StringComparison.OrdinalIgnoreCase));
        if (!currentIsPinned)
        {
            var pinCurrent = new MenuFlyoutItem { Text = $"Pin current model · {_settings.Model}" };
            pinCurrent.Click += (_, _) => PinCurrentModel();
            menu.Items.Add(pinCurrent);
        }
        foreach (var model in quickSwitchModels)
        {
            var isCurrent = model.Model.Equals(_settings.Model, StringComparison.OrdinalIgnoreCase) && model.Endpoint.Equals(_settings.Endpoint, StringComparison.OrdinalIgnoreCase);
            var performance = (_settings.LocalModelPerformance ?? []).GetValueOrDefault(LocalModelPerformance.Key(model.Endpoint, model.Model));
            var rate = performance is null ? "Not benchmarked" : $"≈{performance.EstimatedTokensPerSecond:0.#} tok/s";
            var item = new MenuFlyoutItem { Text = $"{(isCurrent ? "✓  " : "")}{model.Provider} · {model.Model} · {rate}" };
            ToolTipService.SetToolTip(item, performance is null
                ? $"{model.Provider} · {model.Model}\nNo performance sample recorded on this device yet. Use Models → Benchmark selected to compare it."
                : $"{model.Provider} · {model.Model}\nEstimated {performance.EstimatedTokensPerSecond:0.#} tokens/second · {performance.TimeToFirstTokenMilliseconds} ms to first token · {performance.Samples} sample(s) on this device. Throughput is estimated from response text length.");
            item.Click += async (_, _) => await SelectModelAsync(model.Model, model.Endpoint, model.Provider);
            menu.Items.Add(item);
        }
        if (menu.Items.Count > 0) menu.Items.Add(new MenuFlyoutSeparator());
        var manage = new MenuFlyoutItem { Text = "Manage models…" };
        manage.Click += async (_, _) => await OpenModelLibraryAndRestoreFocusAsync(false, ModelButton);
        menu.Items.Add(manage);
        menu.ShowAt(ModelButton);
    }

    private async Task OpenModelLibraryAndRestoreFocusAsync(bool returnToCompactSidebar, Control? trigger, bool confirmModelSwitch = false)
    {
        try { await RunUiActionAsync(() => ModelLibraryClickAsync(confirmModelSwitch)); }
        finally { RestoreNavigationDialogFocus(returnToCompactSidebar, trigger); }
    }

    private void PinCurrentModel()
    {
        _settings.PinnedModelConnections ??= [];
        if (!_settings.PinnedModelConnections.Any(item => item.Model.Equals(_settings.Model, StringComparison.OrdinalIgnoreCase) && item.Endpoint.Equals(_settings.Endpoint, StringComparison.OrdinalIgnoreCase)))
            _settings.PinnedModelConnections.Add(new PinnedModelConnection(_settings.ModelProvider, _settings.Model, _settings.Endpoint));
        _settings.PinnedModels?.RemoveAll(model => model.Equals(_settings.Model, StringComparison.OrdinalIgnoreCase));
        SaveSettings();
        RuntimeStatus.Text = $"Pinned {_settings.Model} in the model quick switch above the typing box.";
    }

    private async Task SelectModelAsync(string model, string? endpoint = null, string? provider = null)
    {
        var targetEndpoint = endpoint ?? _settings.Endpoint;
        var targetProvider = provider ?? _settings.ModelProvider;
        try
        {
            if (targetProvider.Equals("LM Studio", StringComparison.OrdinalIgnoreCase))
                await _modelLibrary.EnsureLmStudioModelLoadedAsync(targetEndpoint, model, message => RuntimeStatus.Text = message);
        }
        catch (Exception ex)
        {
            RuntimeStatus.Text = $"Could not switch to {model}: {ex.Message}";
            return;
        }
        MigrateLegacyPinnedModels();
        _settings.Model = model;
        _settings.Endpoint = targetEndpoint;
        _settings.ModelProvider = targetProvider;
        SaveSettings();
        UpdateModelLabel();
        RuntimeStatus.Text = $"Switched to {model}";
        _ = RefreshModelConnectionAsync();
    }

    private void MigrateLegacyPinnedModels()
    {
        if (_settings.PinnedModels is not { Count: > 0 }) return;
        _settings.PinnedModelConnections ??= [];
        foreach (var model in _settings.PinnedModels)
            if (!_settings.PinnedModelConnections.Any(pinned => pinned.Model.Equals(model, StringComparison.OrdinalIgnoreCase) && pinned.Endpoint.Equals(_settings.Endpoint, StringComparison.OrdinalIgnoreCase)))
                _settings.PinnedModelConnections.Add(new PinnedModelConnection(_settings.ModelProvider, model, _settings.Endpoint));
        _settings.PinnedModels.Clear();
    }

    private async Task ModelLibraryClickAsync(bool confirmModelSwitch = false)
    {
        var source = new ComboBox { MinWidth = 220 };
        AutomationProperties.SetName(source, "Model source");
        AutomationProperties.SetHelpText(source, "Choose a model catalog, installed models, or a direct Ollama model ID.");
        source.Items.Add("Hugging Face · GGUF catalog");
        source.Items.Add("Installed models");
        source.Items.Add("Ollama Library");
        source.Items.Add("ModelScope · GGUF");
        // Owner decision (2026-11-07): open on Installed models so nothing is sent to Hugging
        // Face until the user picks the catalog source (matches the README privacy promise).
        source.SelectedIndex = 1;
        var catalogMode = new ComboBox { MinWidth = 220, Visibility = Visibility.Collapsed };
        AutomationProperties.SetName(catalogMode, "Model catalog category");
        AutomationProperties.SetHelpText(catalogMode, "Choose regular models or repositories explicitly labeled uncensored or abliterated.");
        catalogMode.Items.Add("Regular models");
        catalogMode.Items.Add("Uncensored / abliterated");
        catalogMode.SelectedIndex = 0;
        var query = new TextBox { PlaceholderText = "Search models by name or capability", MinWidth = 280 };
        AutomationProperties.SetName(query, "Search models");
        AutomationProperties.SetHelpText(query, "Search model repositories by name or capability, then activate Search or refresh.");
        var search = new Button { Content = "Search / refresh", MinWidth = 108 };
        AutomationProperties.SetName(search, "Search or refresh model catalog");
        AutomationProperties.SetHelpText(search, "Find models matching the search text or refresh installed-model results.");
        var pin = new Button { Content = "Pin / unpin", MinWidth = 88 };
        AutomationProperties.SetName(pin, "Pin selected installed model");
        AutomationProperties.SetHelpText(pin, "Add or remove this installed model from the composer hotswapper.");
        var benchmark = new Button { Content = "Benchmark selected", MinWidth = 130 };
        AutomationProperties.SetName(benchmark, "Benchmark selected model on this PC");
        AutomationProperties.SetHelpText(benchmark, "Measure response speed for the selected model using this device.");
        var stopBenchmark = new Button { Content = "Stop", MinWidth = 66, IsEnabled = false };
        var sort = new ComboBox { Header = "Sort", MinWidth = 150 };
        sort.Items.Add("Popular");
        sort.Items.Add("Name");
        sort.Items.Add("Downloads");
        sort.Items.Add("Size");
        sort.SelectedIndex = 0;
        var previousPage = new Button { Content = "‹", MinWidth = 42, IsEnabled = false };
        var nextPage = new Button { Content = "›", MinWidth = 42, IsEnabled = false };
        var pageStatus = new TextBlock { Text = "Page 1", VerticalAlignment = VerticalAlignment.Center, FontSize = 11 };
        var page = 0;
        var pageCount = 1;
        AutomationProperties.SetName(stopBenchmark, "Stop model benchmark");
        AutomationProperties.SetHelpText(stopBenchmark, "Cancel the model benchmark currently running on this device.");
        // Rows carry long repo names plus metadata; wrap them instead of cutting them off.
        var list = UiTheme.WrapListText(new ListView { MinHeight = 220, MaxHeight = 360, SelectionMode = ListViewSelectionMode.Single, DisplayMemberPath = "Id" });
        AutomationProperties.SetName(list, "Model search results");
        AutomationProperties.SetHelpText(list, "Select a model to use it, pin it in the composer hotswapper, or benchmark it on this PC.");
        CancellationTokenSource? catalogSearchCancellation = null;
        var status = new TextBlock { Text = $"Connected endpoint: {_settings.Endpoint}", TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 92, 111, 116)) };
        AutomationProperties.SetName(status, "Model library status");
        AutomationProperties.SetHelpText(status, "Reports model search, download, selection and benchmark progress or errors.");
        AutomationProperties.SetLiveSetting(status, AutomationLiveSetting.Polite);
        var actionRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { query, search } };
        var dialog = new ContentDialog
        {
            Title = "Model Library",
            PrimaryButtonText = "Use selected",
            CloseButtonText = "Done",
            XamlRoot = XamlRoot,
            Content = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    new TextBlock { Text = "Choose, pin, and benchmark the models installed on this PC. Switch the source to browse the Hugging Face, Ollama, or ModelScope catalogs and download compatible GGUF models through a local runtime.", TextWrapping = TextWrapping.Wrap },
                    source,
                    catalogMode,
                    actionRow,
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { sort, previousPage, pageStatus, nextPage } },
                    list,
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { pin, benchmark, stopBenchmark } },
                    status
                }
            }
        };

        async Task RefreshOrSearchAsync()
        {
            var searchCancellation = new CancellationTokenSource();
            var previousSearch = catalogSearchCancellation;
            catalogSearchCancellation = searchCancellation;
            previousSearch?.Cancel();
            var cancellationToken = searchCancellation.Token;
            dialog.IsPrimaryButtonEnabled = source.SelectedIndex == 1;
            pin.IsEnabled = source.SelectedIndex == 1;
            catalogMode.Visibility = source.SelectedIndex is 0 or 3 ? Visibility.Visible : Visibility.Collapsed;
            status.Text = source.SelectedIndex switch
            {
                0 => "Loading popular Hugging Face GGUF models for local runtimes…",
                1 => "Checking installed models on local runtimes…",
                2 => "Enter an Ollama model ID, then choose Download selected.",
                3 => "Searching ModelScope GGUF models…",
                _ => ""
            };
            list.ItemsSource = null;
            list.SelectedItem = null;
            list.DisplayMemberPath = "Id";
            try
            {
                var choice = source.SelectedIndex;
                if (choice == 1)
                {
                    var models = await _modelLibrary.DiscoverLocalModelsAsync(_settings.Endpoint, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    var allChoices = models.Select(model =>
                    {
                        var key = LocalModelPerformance.Key(model.Endpoint, model.Model);
                        var performance = (_settings.LocalModelPerformance ?? []).GetValueOrDefault(key);
                        var readiness = model.IsLoaded == false ? "Downloaded · not loaded" : model.IsLoaded == true ? "Loaded" : null;
                        var size = model.SizeBytes is > 0 ? $"{model.SizeBytes.Value / 1073741824d:0.#} GB" : null;
                        var details = string.Join(" · ", new[] { model.DisplayName, size, readiness }.Where(value => !string.IsNullOrWhiteSpace(value)));
                        var measurement = performance is null
                            ? "Not yet measured on this device"
                            : $"≈{performance.EstimatedTokensPerSecond:0.#} tok/s · {performance.TimeToFirstTokenMilliseconds} ms first token · {performance.Samples} sample{(performance.Samples == 1 ? "" : "s")}";
                        var display = $"{model.Model}   ·   {model.Provider}   ·   {measurement}" + (details.Length == 0 ? "" : $"   ·   {details}");
                        return new InstalledModelChoice(model.Provider, model.Model, model.Endpoint, display);
                    }).OrderByDescending(choice => (_settings.LocalModelPerformance ?? []).GetValueOrDefault(LocalModelPerformance.Key(choice.Endpoint, choice.Model))?.EstimatedTokensPerSecond ?? -1).ToArray();
                    var choices = ApplyModelPage(allChoices, sort.SelectedIndex, ref pageCount, ref page, out var shownCount);
                    list.DisplayMemberPath = nameof(InstalledModelChoice.Display);
                    list.ItemsSource = choices;
                    pageStatus.Text = $"Page {page + 1} of {pageCount} · {shownCount:N0} shown";
                    var hostCount = choices.Select(choice => choice.Endpoint).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                    status.Text = $"{choices.Length} installed model(s) found across {hostCount} local runtime(s). Performance is measured from completed responses on this device; tokens/second is estimated from response length.";
                }
                else if (choice == 0)
                {
                    var searchText = string.IsNullOrWhiteSpace(query.Text) ? "instruct" : query.Text.Trim();
                    var uncensored = catalogMode.SelectedIndex == 1;
                    var models = await _modelLibrary.SearchHuggingFaceAsync(searchText, uncensored, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    var allChoices = models.Select(model => new CatalogModelChoice(model,
                        $"{model.Id}   ·   {model.Downloads:N0} downloads" + (model.IsUncensored ? "   ·   Explicit uncensored / abliterated label" : ""))).ToArray();
                    var choices = ApplyModelPage(allChoices, sort.SelectedIndex, ref pageCount, ref page, out var shownCount);
                    list.DisplayMemberPath = nameof(CatalogModelChoice.Display);
                    list.ItemsSource = choices;
                    pageStatus.Text = $"Page {page + 1} of {pageCount} · {shownCount:N0} shown";
                    status.Text = uncensored
                        ? $"Found {models.Count} GGUF repositories with explicit uncensored/abliterated labels. Labels come from repository IDs and tags and do not guarantee model behavior."
                        : string.IsNullOrWhiteSpace(query.Text)
                            ? $"Showing {models.Count} popular chat-ready GGUF repositories. Search by name or capability to refine the catalog. Downloads use the configured LM Studio server."
                            : $"Found {models.Count} regular GGUF repositories; explicitly labeled uncensored/abliterated results are excluded. Hugging Face download uses the configured LM Studio server.";
                }
                else if (choice == 3)
                {
                    if (string.IsNullOrWhiteSpace(query.Text)) { status.Text = "Search ModelScope by model name or capability to find chat-ready GGUF files."; return; }
                    var uncensored = catalogMode.SelectedIndex == 1;
                    var packages = await _modelLibrary.SearchModelScopeGgufAsync(query.Text, uncensored, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    var allChoices = packages.Select(package => new ModelScopeGgufPackageChoice(package,
                        $"{package.Repository}   ·   {package.Name}   ·   {(package.SizeBytes > 0 ? $"{package.SizeBytes / 1073741824d:0.#} GB" : "size unknown")}" +
                        (package.Files.Count > 1 ? $"   ·   {package.Files.Count} split files" : "") +
                        $"   ·   {package.Downloads:N0} downloads" + (package.IsUncensored ? "   ·   Explicit uncensored / abliterated label" : ""))).ToArray();
                    var choices = ApplyModelPage(allChoices, sort.SelectedIndex, ref pageCount, ref page, out var shownCount);
                    list.DisplayMemberPath = nameof(ModelScopeGgufPackageChoice.Display);
                    list.ItemsSource = choices;
                    pageStatus.Text = $"Page {page + 1} of {pageCount} · {shownCount:N0} shown";
                    status.Text = uncensored
                        ? $"Found {packages.Count} ModelScope GGUF package(s) with explicit uncensored/abliterated labels. Files are SHA-256 checked and imported into the local Ollama runtime."
                        : $"Found {packages.Count} regular ModelScope GGUF package(s); explicitly labeled uncensored/abliterated repositories are excluded. Downloads are SHA-256 checked and imported into the local Ollama runtime.";
                }
                else
                {
                    list.DisplayMemberPath = null;
                    list.ItemsSource = string.IsNullOrWhiteSpace(query.Text) ? Array.Empty<string>() : new[] { query.Text.Trim() };
                    status.Text = "Ollama Library names are entered directly, for example qwen3:8b. Pull downloads the model into the connected local Ollama runtime.";
                }
                previousPage.IsEnabled = page > 0;
                nextPage.IsEnabled = page + 1 < pageCount;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested) status.Text = $"Could not load models: {ex.Message}";
            }
            finally
            {
                if (ReferenceEquals(catalogSearchCancellation, searchCancellation)) catalogSearchCancellation = null;
                searchCancellation.Dispose();
            }
        }
        search.Click += async (_, _) => await RefreshOrSearchAsync();
        sort.SelectionChanged += async (_, _) => { page = 0; await RefreshOrSearchAsync(); };
        previousPage.Click += async (_, _) => { if (page > 0) { page--; await RefreshOrSearchAsync(); } };
        nextPage.Click += async (_, _) => { if (page + 1 < pageCount) { page++; await RefreshOrSearchAsync(); } };
        source.SelectionChanged += async (_, _) => { page = 0; await RefreshOrSearchAsync(); };
        catalogMode.SelectionChanged += async (_, _) => { if (source.SelectedIndex is 0 or 3) { page = 0; await RefreshOrSearchAsync(); } };
        pin.Click += (_, _) =>
        {
            if (list.SelectedItem is not InstalledModelChoice selected) { status.Text = "Select an installed model first."; return; }
            _settings.PinnedModelConnections ??= [];
            _settings.PinnedModels ??= [];
            var existing = _settings.PinnedModelConnections.FirstOrDefault(model => model.Model.Equals(selected.Model, StringComparison.OrdinalIgnoreCase) && model.Endpoint.Equals(selected.Endpoint, StringComparison.OrdinalIgnoreCase));
            var legacyPin = selected.Endpoint.Equals(_settings.Endpoint, StringComparison.OrdinalIgnoreCase) && _settings.PinnedModels.Contains(selected.Model, StringComparer.OrdinalIgnoreCase);
            if (existing is null && legacyPin)
            {
                _settings.PinnedModels.RemoveAll(model => model.Equals(selected.Model, StringComparison.OrdinalIgnoreCase));
                status.Text = $"Unpinned {selected.Model} from the hotswapper.";
            }
            else if (existing is null)
            {
                _settings.PinnedModelConnections.Add(new PinnedModelConnection(selected.Provider, selected.Model, selected.Endpoint));
                status.Text = $"Pinned {selected.Model} from {selected.Provider} in the hotswapper.";
            }
            else
            {
                _settings.PinnedModelConnections.Remove(existing);
                if (selected.Endpoint.Equals(_settings.Endpoint, StringComparison.OrdinalIgnoreCase))
                    _settings.PinnedModels.RemoveAll(model => model.Equals(selected.Model, StringComparison.OrdinalIgnoreCase));
                status.Text = $"Unpinned {selected.Model} from the hotswapper.";
            }
            SaveSettings();
        };
        list.SelectionChanged += (_, _) =>
        {
            var selected = list.SelectedItem as InstalledModelChoice;
            var isPinned = selected is not null && ((_settings.PinnedModelConnections ?? []).Any(model => model.Model.Equals(selected.Model, StringComparison.OrdinalIgnoreCase) && model.Endpoint.Equals(selected.Endpoint, StringComparison.OrdinalIgnoreCase)) || (selected.Endpoint.Equals(_settings.Endpoint, StringComparison.OrdinalIgnoreCase) && (_settings.PinnedModels ?? []).Contains(selected.Model, StringComparer.OrdinalIgnoreCase)));
            pin.Content = isPinned ? "Unpin" : "Pin";
            AutomationProperties.SetName(pin, isPinned ? "Unpin selected model" : "Pin selected model");
            benchmark.IsEnabled = source.SelectedIndex == 1 && selected is not null && _modelBenchmarkCancellation is null && LocalEndpoint.IsLocal(selected.Endpoint);
        };
        stopBenchmark.Click += (_, _) => _modelBenchmarkCancellation?.Cancel();
        benchmark.Click += async (_, _) =>
        {
            if (list.SelectedItem is not InstalledModelChoice selected || !LocalEndpoint.IsLocal(selected.Endpoint))
            {
                status.Text = "Select an installed model connected to a local endpoint to benchmark it.";
                return;
            }
            if (_requestCancellation is not null || _modelBenchmarkCancellation is not null)
            {
                status.Text = "Wait for the current model request or benchmark to finish first.";
                return;
            }
            var endpoint = selected.Endpoint;
            var model = selected.Model;
            var settings = new AppSettings { Endpoint = endpoint, Model = model, ModelProvider = selected.Provider, Temperature = 0, VisionApiFormat = _settings.VisionApiFormat };
            var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(75));
            _modelBenchmarkCancellation = cancellation;
            benchmark.IsEnabled = false;
            stopBenchmark.IsEnabled = true;
            dialog.IsPrimaryButtonEnabled = false;
            source.IsEnabled = false;
            search.IsEnabled = false;
            pin.IsEnabled = false;
            var timer = Stopwatch.StartNew();
            long? firstToken = null;
            var responseCharacters = 0;
            status.Text = $"Benchmarking {model} with the same short prompt used for model comparisons. This loads/runs the model locally; chat is paused until it finishes.";
            try
            {
                if (selected.Provider.Equals("LM Studio", StringComparison.OrdinalIgnoreCase))
                    await _modelLibrary.EnsureLmStudioModelLoadedAsync(endpoint, model, message => status.Text = message, cancellation.Token);
                await _provider.StreamChatAsync(settings,
                    [new ChatMessage("user", "In a concise paragraph of about 50 words, explain one practical benefit of caching in a local AI application. Output only the paragraph.")],
                    chunk =>
                    {
                        firstToken ??= Math.Max(1, timer.ElapsedMilliseconds);
                        responseCharacters += chunk.Length;
                        status.Text = $"Benchmarking {model} · received {responseCharacters} response characters…";
                        if (responseCharacters > 1600) cancellation.Cancel();
                    },
                    cancellation.Token,
                    maxTokens: 96,
                    apiKey: GetProviderApiKey());
                timer.Stop();
                if (responseCharacters == 0 || firstToken is null) throw new InvalidOperationException("The model returned no benchmark text.");
                RecordLocalModelPerformance(endpoint, model, responseCharacters, timer.ElapsedMilliseconds, firstToken.Value);
                await RefreshOrSearchAsync();
                list.SelectedItem = (list.ItemsSource as IEnumerable<InstalledModelChoice>)?.FirstOrDefault(item => item.Model.Equals(model, StringComparison.OrdinalIgnoreCase) && item.Endpoint.Equals(endpoint, StringComparison.OrdinalIgnoreCase));
                status.Text = $"Measured {model} · ≈{responseCharacters / 4d / Math.Max(0.001, (timer.ElapsedMilliseconds - firstToken.Value) / 1000d):0.#} estimated tokens/s · {firstToken.Value} ms first token.";
            }
            catch (OperationCanceledException)
            {
                status.Text = $"Benchmark cancelled or exceeded its 75-second limit for {model}; incomplete runs are not recorded.";
            }
            catch (Exception ex) { status.Text = $"Benchmark failed for {model}: {ex.Message}"; }
            finally
            {
                timer.Stop();
                cancellation.Dispose();
                _modelBenchmarkCancellation = null;
                stopBenchmark.IsEnabled = false;
                benchmark.IsEnabled = source.SelectedIndex == 1 && list.SelectedItem is InstalledModelChoice finalChoice && LocalEndpoint.IsLocal(finalChoice.Endpoint);
                source.IsEnabled = true;
                search.IsEnabled = true;
                pin.IsEnabled = source.SelectedIndex == 1;
                dialog.IsPrimaryButtonEnabled = source.SelectedIndex == 1;
            }
        };
        dialog.Closed += (_, _) =>
        {
            catalogSearchCancellation?.Cancel();
            _modelBenchmarkCancellation?.Cancel();
        };
        dialog.SecondaryButtonText = "Download selected";
        while (true)
        {
            await RefreshOrSearchAsync();
            if (source.SelectedIndex == 1)
                list.SelectedItem = (list.ItemsSource as IEnumerable<InstalledModelChoice>)?.FirstOrDefault(item => item.Model.Equals(_settings.Model, StringComparison.OrdinalIgnoreCase) && item.Endpoint.Equals(_settings.Endpoint, StringComparison.OrdinalIgnoreCase));
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                if (list.SelectedItem is InstalledModelChoice selected)
                {
                    if (confirmModelSwitch)
                    {
                        var confirm = new ContentDialog
                        {
                            Title = "Switch local model?",
                            Content = new TextBlock
                            {
                                Text = $"The previous model failed to return a response. Switch from {_settings.Model} to {selected.Model}? This changes the selected local model only; no cloud route or automatic retry will occur.",
                                TextWrapping = TextWrapping.Wrap
                            },
                            PrimaryButtonText = "Switch model",
                            CloseButtonText = "Keep current model",
                            XamlRoot = XamlRoot
                        };
                        if (await confirm.ShowAsync() != ContentDialogResult.Primary) return;
                    }
                    await SelectModelAsync(selected.Model, selected.Endpoint, selected.Provider);
                }
                return;
            }
            if (result != ContentDialogResult.Secondary) return;
            var selectedId = SelectedModelId(list.SelectedItem);
            if (string.IsNullOrWhiteSpace(selectedId)) { status.Text = "Select a model to download."; continue; }
            try
            {
                status.Text = "Starting model download… Keep the app open while the local runtime installs it.";
                RuntimeStatus.Text = status.Text;
                if (source.SelectedIndex == 0)
                {
                    var endpoint = await GetRuntimeEndpointAsync("LM Studio");
                    await _modelLibrary.DownloadFromHuggingFaceAsync(endpoint, selectedId, onProgress: progress => RuntimeStatus.Text = progress);
                }
                else if (source.SelectedIndex == 2)
                {
                    var endpoint = await GetRuntimeEndpointAsync("Ollama");
                    await _modelLibrary.PullOllamaAsync(endpoint, selectedId, progress => RuntimeStatus.Text = progress);
                }
                else if (source.SelectedIndex == 3 && list.SelectedItem is ModelScopeGgufPackageChoice selectedPackage)
                {
                    var endpoint = await GetRuntimeEndpointAsync("Ollama");
                    var imported = await _modelLibrary.DownloadAndImportModelScopeGgufAsync(endpoint, selectedPackage.Package, progress => RuntimeStatus.Text = progress);
                    status.Text = $"Imported {imported} into Ollama. Refresh Installed models to benchmark and pin it.";
                }
                else { status.Text = "Select a supported model source and a downloadable model."; continue; }
                if (source.SelectedIndex != 3) status.Text = $"Downloaded {selectedId}. Refresh Installed models to select and pin it.";
                source.SelectedIndex = 1;
            }
            catch (Exception ex) { status.Text = $"Download failed: {ex.Message}"; }
        }
    }

    private static T[] ApplyModelPage<T>(IEnumerable<T> source, int sortIndex, ref int pageCount, ref int page, out int shownCount)
    {
        var objects = source.Cast<object>();
        var ordered = sortIndex switch
        {
            1 => objects.OrderBy(item => item switch
            {
                InstalledModelChoice installed => installed.Model,
                CatalogModelChoice catalog => catalog.Model.Id,
                ModelScopeGgufPackageChoice package => package.Package.Repository + "/" + package.Package.Name,
                _ => item.ToString()
            }, StringComparer.OrdinalIgnoreCase),
            2 => objects.OrderByDescending(item => item switch
            {
                CatalogModelChoice catalog => catalog.Model.Downloads,
                ModelScopeGgufPackageChoice package => package.Package.Downloads,
                InstalledModelChoice installed => 0L,
                _ => 0L
            }),
            3 => objects.OrderByDescending(item => item switch
            {
                ModelScopeGgufPackageChoice package => package.Package.SizeBytes,
                InstalledModelChoice installed => installed.Model.Length,
                _ => 0L
            }),
            _ => objects
        };
        var orderedArray = ordered.ToArray();
        pageCount = Math.Max(1, (int)Math.Ceiling(orderedArray.Length / 100d));
        page = Math.Clamp(page, 0, pageCount - 1);
        var pageItems = orderedArray.Skip(page * 100).Take(100).Cast<T>().ToArray();
        shownCount = pageItems.Length;
        return pageItems;
    }

    private async Task<string> GetRuntimeEndpointAsync(string provider)
    {
        if (_settings.ModelProvider.Equals(provider, StringComparison.OrdinalIgnoreCase) && LocalEndpoint.IsLocal(_settings.Endpoint))
            return _settings.Endpoint;
        var installed = await _modelLibrary.DiscoverLocalModelsAsync(_settings.Endpoint);
        var discovered = installed.FirstOrDefault(model => model.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase));
        if (discovered is not null) return discovered.Endpoint;
        return provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase)
            ? "http://localhost:11434/v1"
            : "http://localhost:1234/v1";
    }

    private static string? SelectedModelId(object? item) => item switch
    {
        string model => model,
        HubModel model => model.Id,
        CatalogModelChoice model => model.Model.Id,
        ModelScopeGgufPackageChoice model => model.Package.Repository + "/" + model.Package.Name,
        InstalledModelChoice model => model.Model,
        _ => null
    };

    private string DescribeDataRoute()
    {
        var local = LocalEndpoint.IsLocal(_settings.Endpoint);
        if (!_providerConnected) return "● Model not connected";
        if (_mode == WorkspaceModes.CodingAgent) return local ? "● Project context stays on device" : "● Project files go to remote model";
        if (_includeProjectFilesInChat) return local ? "● Prompt + selected project files stay on device" : "● Project excerpts go to remote model";
        return local ? "● On-device model" : "● Remote model server";
    }

    private static string TruncateComposerLabel(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";

    private void UpdateModelLabel()
    {
        var id = _settings.Model;
        var visible = id.Split('/').Last();
        var tagParts = visible.Split(':', 2);
        var modelName = tagParts[0];
        var providerName = _settings.ModelProvider.Length > 12 ? _settings.ModelProvider[..11] + "…" : _settings.ModelProvider;
        var nameLimit = Math.Max(14, 29 - providerName.Length);
        if (modelName.Length > nameLimit) modelName = modelName[..(nameLimit - 1)] + "…";
        var tag = tagParts.Length > 1 && tagParts[1].Length <= 12 ? " · " + tagParts[1] : "";
        ModelButton.Content = _compactComposerToolbar
            ? $"◉ {TruncateComposerLabel(modelName, 14)} ⌄"
            : $"◉　{providerName} · {modelName}{tag}　⌄";
        AutomationProperties.SetHelpText(ModelButton, $"Model ID: {id}; provider: {_settings.ModelProvider}; endpoint: {_settings.Endpoint}");
    }
}
