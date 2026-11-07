// Settings dialog (sections: General, Models & downloads, Chats & history, Appearance, Pets,
// GitHub workspace, Host configuration, Advanced), the Advanced generation dialog (temperature),
// and ApplySavedCustomization, which applies the accent color, density, and voice-output icon.
// Some Settings buttons close the dialog and then open another feature (Model Library, GitHub,
// import/export) through actionAfterClose.
// Depends on: SaveSettings, RefreshModelConnectionAsync, AppLogService export, UiTheme, and most
// feature dialogs it links to.
// Called by: the Settings button and Connect a model in MainPage.xaml, /settings and /connect,
// and the constructor/settings changes (ApplySavedCustomization).

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Media;
using ServerRock.Core.Models;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private void ApplySavedCustomization()
    {
        // Accent choices are darkened so white text on them passes WCAG AA (4.5:1); the
        // default comes from the AccentColor token. UiTheme.ApplyAccent updates every
        // accent brush in place, so the send button and dialog primary actions follow.
        var (accent, hover) = _settings.AccentColor?.ToLowerInvariant() switch
        {
            "violet" => (ColorHelper.FromArgb(255, 107, 79, 200), ColorHelper.FromArgb(255, 90, 64, 176)),
            "green" => (ColorHelper.FromArgb(255, 30, 127, 85), ColorHelper.FromArgb(255, 24, 107, 71)),
            "orange" => (ColorHelper.FromArgb(255, 168, 90, 18), ColorHelper.FromArgb(255, 143, 76, 15)),
            _ => (UiTheme.Color("AccentColor"), UiTheme.Color("AccentHoverColor"))
        };
        UiTheme.ApplyAccent(accent, hover, hover);
        ApplyComposerHeight();
        // Voice output is an icon toggle (speaker on / muted) with a tooltip, matching the other
        // composer icons; the on state also gets the accent color.
        VoiceOutputIcon.Glyph = _settings.VoiceOutputEnabled ? "\uE767" : "\uE74F";
        VoiceOutputButton.Foreground = UiTheme.Brush(_settings.VoiceOutputEnabled ? "AccentInkBrush" : "MutedInkBrush");
        ToolTipService.SetToolTip(VoiceOutputButton, _settings.VoiceOutputEnabled ? "Read replies aloud: on" : "Read replies aloud: off");
        AutomationProperties.SetName(VoiceOutputButton, _settings.VoiceOutputEnabled ? "Disable spoken responses" : "Enable spoken responses");
    }

    private async void AdvancedClick(object sender, RoutedEventArgs e) => await RunUiActionAsync(AdvancedClickAsync);

    private async Task AdvancedClickAsync()
    {
        App.Log.Info("UI", "Opening advanced response controls");
        var temperatureValue = new TextBlock
        {
            Text = _settings.Temperature.ToString("0.0"),
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        var temperature = new Slider { Minimum = 0, Maximum = 2, Value = _settings.Temperature, StepFrequency = 0.1 };
        AutomationProperties.SetName(temperature, "Temperature");
        temperature.ValueChanged += (_, args) => temperatureValue.Text = args.NewValue.ToString("0.0");
        var reset = new Button { Content = "Reset to default (0.7)", HorizontalAlignment = HorizontalAlignment.Left };
        reset.Click += (_, _) => temperature.Value = 0.7;
        var temperatureRow = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };
        temperatureRow.Children.Add(temperature);
        Grid.SetColumn(temperatureValue, 1);
        temperatureRow.Children.Add(temperatureValue);
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock { Text = "Temperature", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = "Lower values usually keep answers more focused; higher values allow more variation. Some local servers may ignore this setting.", TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 102, 120, 125)) });
        panel.Children.Add(temperatureRow);
        panel.Children.Add(reset);
        var dialog = new ContentDialog { Title = "Advanced generation", Content = panel, PrimaryButtonText = "Save", CloseButtonText = "Cancel", XamlRoot = XamlRoot };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            _settings.Temperature = temperature.Value;
            SaveSettings();
        }
    }

    private async void SettingsClick(object sender, RoutedEventArgs e)
    {
        var returnToCompactSidebar = _compactNavigation && Sidebar.Visibility == Visibility.Visible;
        CloseCompactSidebar();
        RuntimeStatus.Text = "Opening connection setup…";
        try { await RunUiActionAsync(SettingsDialogAsync); }
        finally { RestoreNavigationDialogFocus(returnToCompactSidebar, sender as Control); }
    }

    private void RestoreNavigationDialogFocus(bool returnToCompactSidebar, Control? trigger)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (returnToCompactSidebar && ToggleSidebarButton.Visibility == Visibility.Visible &&
                ToggleSidebarButton.Focus(FocusState.Programmatic))
                return;
            if (trigger is { Visibility: Visibility.Visible, IsEnabled: true } && trigger.Focus(FocusState.Programmatic))
                return;
            if (_compactNavigation && ToggleSidebarButton.Visibility == Visibility.Visible &&
                ToggleSidebarButton.Focus(FocusState.Programmatic))
                return;
            PromptBox.Focus(FocusState.Programmatic);
        });
    }

    private async Task SettingsDialogAsync()
    {
        var endpoint = new TextBox { Header = "OpenAI-compatible endpoint", Text = _settings.Endpoint, PlaceholderText = "http://localhost:11434/v1" };
        var model = new TextBox { Header = "Model ID", Text = _settings.Model, PlaceholderText = "Model ID reported by the server" };
        var openRouterKey = new PasswordBox { Header = "OpenRouter API key (session only)", PlaceholderText = "sk-or-v1-…", Password = _openRouterApiKey };
        AutomationProperties.SetName(openRouterKey, "OpenRouter API key");
        AutomationProperties.SetHelpText(openRouterKey, "Required by OpenRouter, including its free models. The key is used only in memory for this app session and is never saved to settings.");
        var openRouter = new Button { Content = "Use OpenRouter free models", MinWidth = 190 };
        AutomationProperties.SetName(openRouter, "Use OpenRouter free models");
        AutomationProperties.SetHelpText(openRouter, "Set the OpenRouter endpoint and automatic free-model router.");
        var smartRewrite = new CheckBox { Content = "Show a smart prompt rewrite before sending", IsChecked = _settings.SmartPromptRewritingEnabled };
        AutomationProperties.SetName(smartRewrite, "Enable smart prompt rewriting");
        AutomationProperties.SetHelpText(smartRewrite, "Use the selected model to rewrite your draft using the current conversation, then review it before sending.");
        var accent = new ComboBox { MinWidth = 220 };
        AutomationProperties.SetName(accent, "Accent color");
        accent.Items.Add("Cyan"); accent.Items.Add("Violet"); accent.Items.Add("Green"); accent.Items.Add("Orange");
        accent.SelectedIndex = _settings.AccentColor.ToLowerInvariant() switch { "violet" => 1, "green" => 2, "orange" => 3, _ => 0 };
        var density = new ComboBox { MinWidth = 220 };
        AutomationProperties.SetName(density, "Interface density");
        density.Items.Add("Comfortable"); density.Items.Add("Compact");
        density.SelectedIndex = _settings.InterfaceDensity.Equals("compact", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        var hostLabel = new TextBox { Header = "Host label", Text = _settings.HostLabel, PlaceholderText = "This device" };
        var localOnly = new CheckBox { Content = "Only allow local model endpoints", IsChecked = _settings.RequireLocalModelEndpoints };
        AutomationProperties.SetHelpText(localOnly, "When enabled, saving a remote endpoint is rejected. OpenRouter remains available when this is off.");
        var showPet = new CheckBox { Content = "Show pet when the app starts", IsChecked = _companionState.IsVisible };
        var petCompletion = new CheckBox { Content = "Let the pet announce completed responses", IsChecked = _companionState.NotifyOnCompletion };
        var petErrors = new CheckBox { Content = "Let the pet announce errors and blocked actions", IsChecked = _companionState.NotifyOnErrors };
        var petQuestions = new CheckBox { Content = "Let the pet ask for approval before risky actions", IsChecked = _companionState.AskBeforeActions };
        var note = new TextBlock { Text = "OpenRouter provides an OpenAI-compatible endpoint. Its openrouter/free router selects an available free model, but OpenRouter still requires your own API key and applies free-tier rate limits. Smart rewriting sends the draft and conversation context to the selected endpoint for a reviewable rewrite; it never changes your original text automatically.", TextWrapping = TextWrapping.Wrap, Opacity = 0.8 };
        var visionFormat = new ComboBox { Header = "Image API format", MinWidth = 300 };
        visionFormat.Items.Add("Automatic (Ollama at localhost:11434)");
        visionFormat.Items.Add("OpenAI-compatible image_url object");
        visionFormat.Items.Add("Ollama-compatible image_url string");
        visionFormat.SelectedIndex = _settings.VisionApiFormat.ToLowerInvariant() switch { "openai" => 1, "ollama" => 2, _ => 0 };
        var visionNote = new TextBlock { Text = "Choose a vision-capable model. Automatic selects Ollama format for its standard local port and OpenAI format for other endpoints.", TextWrapping = TextWrapping.Wrap, FontSize = 11, Opacity = 0.75 };
        var checkConnection = new Button { Content = "Check connection", MinWidth = 132 };
        var exportLogs = new Button { Content = "Export redacted diagnostics", MinWidth = 190 };
        var checkStatus = new TextBlock
        {
            Text = "Save to make this connection the active model server. Use Models in the navigation to browse and install compatible local models.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12,
            Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 78, 105, 111))
        };
        AutomationProperties.SetName(checkConnection, "Check model connection");
        AutomationProperties.SetHelpText(checkConnection, "Probe the entered local endpoint without saving changes.");
        AutomationProperties.SetName(checkStatus, "Connection check status");
        AutomationProperties.SetLiveSetting(checkStatus, AutomationLiveSetting.Polite);

        var search = new TextBox { PlaceholderText = "Find a settings section", Margin = new Thickness(0, 8, 0, 12) };
        AutomationProperties.SetName(search, "Search settings sections");
        var navigation = new StackPanel { Spacing = 4 };
        var pageTitle = new TextBlock { FontSize = UiTheme.FontSize("TitleFontSize"), FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = UiTheme.Brush("InkBrush") };
        var pageDescription = new TextBlock { FontSize = UiTheme.FontSize("SmallFontSize"), TextWrapping = TextWrapping.Wrap, Foreground = UiTheme.Brush("MutedInkBrush"), Margin = new Thickness(0, 4, 0, 18) };
        var pageBody = new StackPanel { Spacing = 12 };
        var pageContent = new StackPanel { Spacing = 0, MaxWidth = 700, HorizontalAlignment = HorizontalAlignment.Left };
        pageContent.Children.Add(pageTitle);
        pageContent.Children.Add(pageDescription);
        pageContent.Children.Add(pageBody);
        var noSearchMatches = new TextBlock { Text = "No settings sections match that search.", FontSize = 12, TextWrapping = TextWrapping.Wrap, Visibility = Visibility.Collapsed, Margin = new Thickness(8, 10, 8, 0) };
        var rootWidth = XamlRoot?.Size.Width ?? 1024;
        var rootHeight = XamlRoot?.Size.Height ?? 768;
        // The settings grid is sized to the window; the dialog's width cap is lifted to match
        // (see UiTheme.Standardize below) so section content is no longer clipped at 548 px.
        var settingsWidth = Math.Min(1040, Math.Max(360, rootWidth - 96));
        var settingsGrid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(205) }, new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) } },
            Width = settingsWidth,
            Height = Math.Min(700, Math.Max(360, rootHeight - 190))
        };
        var settingsSidebar = new Border
        {
            Background = UiTheme.Brush("SurfaceAltBrush"),
            BorderBrush = UiTheme.Brush("LineSoftBrush"),
            BorderThickness = new Thickness(0, 0, 1, 0),
            Padding = new Thickness(14, 8, 12, 12),
            Child = new StackPanel { Spacing = 4, Children =
            {
                // No second "Settings" heading here: the dialog title already names it.
                search,
                new TextBlock { Text = "PERSONAL", Style = UiTheme.Style("SectionLabelStyle"), Margin = new Thickness(6, 7, 0, 3) },
                navigation,
                noSearchMatches
            } }
        };
        settingsGrid.Children.Add(settingsSidebar);
        var settingsScroll = new ScrollViewer { Content = pageContent, Padding = new Thickness(28, 24, 28, 24), VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Grid.SetColumn(settingsScroll, 1);
        settingsGrid.Children.Add(settingsScroll);
        var dialog = new ContentDialog
        {
            Title = "Settings",
            Content = settingsGrid,
            PrimaryButtonText = "Save changes",
            CloseButtonText = "Cancel",
            XamlRoot = XamlRoot
        };
        UiTheme.Standardize(dialog, maxWidth: settingsWidth + 48, maxHeight: 900);
        Func<Task>? actionAfterClose = null;
        var chatRestoreButtons = new List<Button>();

        Border MakeSettingsCard(string title, string description, FrameworkElement action)
        {
            var copy = new StackPanel { Spacing = 3, Children =
            {
                new TextBlock { Text = title, FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = UiTheme.Brush("InkBrush"), TextWrapping = TextWrapping.Wrap },
                new TextBlock { Text = description, FontSize = UiTheme.FontSize("CaptionFontSize"), Foreground = UiTheme.Brush("MutedInkBrush"), TextWrapping = TextWrapping.Wrap }
            } };
            var row = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, new ColumnDefinition { Width = GridLength.Auto } }, ColumnSpacing = 14, VerticalAlignment = VerticalAlignment.Center };
            row.Children.Add(copy);
            Grid.SetColumn(action, 1);
            row.Children.Add(action);
            return new Border
            {
                Background = UiTheme.Brush("SurfaceAltBrush"),
                BorderBrush = UiTheme.Brush("LineSoftBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(12),
                Child = row
            };
        }

        var sectionButtons = new List<(Button Button, string SearchText)>();
        void SelectSection(Button selected, string title, string description, Action build)
        {
            // The open section gets the accent-soft selected state (it was almost the same
            // grey as idle rows, so you could not tell which section you were in).
            foreach (var item in sectionButtons)
                UiTheme.ApplyNavItemState(item.Button, ReferenceEquals(item.Button, selected));
            pageTitle.Text = title;
            pageDescription.Text = description;
            pageBody.Children.Clear();
            build();
        }

        void AddSection(string label, string keywords, string title, string description, Action build)
        {
            var button = new Button
            {
                Content = label,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = new SolidColorBrush(Colors.Transparent),
                Foreground = UiTheme.Brush("MutedInkBrush"),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(9, 8, 9, 8)
            };
            AutomationProperties.SetName(button, $"Open {label} settings");
            sectionButtons.Add((button, $"{label} {keywords}"));
            navigation.Children.Add(button);
            button.Click += (_, _) => SelectSection(button, title, description, build);
        }

        void BuildGeneralPage()
        {
            pageBody.Children.Add(new TextBlock { Text = "MODEL CONNECTION", Style = UiTheme.Style("SectionLabelStyle"), Margin = new Thickness(0, 0, 0, 2) });
            pageBody.Children.Add(endpoint);
            pageBody.Children.Add(model);
            pageBody.Children.Add(openRouter);
            pageBody.Children.Add(openRouterKey);
            pageBody.Children.Add(smartRewrite);
            pageBody.Children.Add(note);
            pageBody.Children.Add(checkConnection);
            pageBody.Children.Add(checkStatus);
        }

        void BuildAppearancePage()
        {
            pageBody.Children.Add(new TextBlock { Text = "WORKSPACE APPEARANCE", Style = UiTheme.Style("SectionLabelStyle"), Margin = new Thickness(0, 0, 0, 2) });
            pageBody.Children.Add(MakeSettingsCard("Accent color", "Choose the highlight color used for send and connection actions.", accent));
            pageBody.Children.Add(MakeSettingsCard("Interface density", "Use comfortable spacing for readability or compact spacing for smaller screens.", density));
            var reset = new Button { Content = "Reset appearance" };
            reset.Click += (_, _) => { accent.SelectedIndex = 0; density.SelectedIndex = 0; RuntimeStatus.Text = "Appearance defaults will be restored when you save settings."; };
            pageBody.Children.Add(MakeSettingsCard("Defaults", "Restore the calm cyan accent and comfortable spacing.", reset));
            pageBody.Children.Add(new TextBlock { Text = "Changes apply when you save settings.", FontSize = 11, Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 113, 130, 134)), Margin = new Thickness(4, 2, 4, 0) });
        }

        void BuildPetsPage()
        {
            pageBody.Children.Add(new TextBlock { Text = "COMPANION", Style = UiTheme.Style("SectionLabelStyle"), Margin = new Thickness(0, 0, 0, 2) });
            var customize = new Button { Content = "Choose pet" };
            customize.Click += (_, _) => { actionAfterClose = () => RunUiActionAsync(CustomizeCompanionAsync); dialog.Hide(); };
            pageBody.Children.Add(MakeSettingsCard("Pet companion", $"Current pet: {CompanionChoices.FirstOrDefault(item => item.Id == _companionState.PetId)?.Name ?? "Rocky"}. Choose a character and keep it available while you work.", customize));
            pageBody.Children.Add(showPet);
            pageBody.Children.Add(petCompletion);
            pageBody.Children.Add(petErrors);
            pageBody.Children.Add(petQuestions);
            var reset = new Button { Content = "Reset pet preferences" };
            reset.Click += (_, _) => { showPet.IsChecked = false; petCompletion.IsChecked = true; petErrors.IsChecked = true; petQuestions.IsChecked = true; RuntimeStatus.Text = "Pet preferences will be restored when you save settings."; };
            pageBody.Children.Add(MakeSettingsCard("Defaults", "Show the pet only when requested, with helpful notifications enabled.", reset));
            pageBody.Children.Add(new TextBlock { Text = "The pet stays local. These notifications update its speech and state without sending telemetry.", TextWrapping = TextWrapping.Wrap, FontSize = 11, Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 113, 130, 134)), Margin = new Thickness(4, 6, 4, 0) });
        }

        void BuildHostPage()
        {
            pageBody.Children.Add(new TextBlock { Text = "HOST CONFIGURATION", Style = UiTheme.Style("SectionLabelStyle"), Margin = new Thickness(0, 0, 0, 2) });
            pageBody.Children.Add(MakeSettingsCard("Host identity", "Give this local workspace a recognizable label in status messages and support diagnostics.", hostLabel));
            pageBody.Children.Add(localOnly);
            pageBody.Children.Add(MakeSettingsCard("Current runtime", $"{Environment.OSVersion.Platform} · {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture} · {Environment.Version}", new TextBlock { Text = "Read-only", FontSize = 11, Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 113, 130, 134)), VerticalAlignment = VerticalAlignment.Center }));
            pageBody.Children.Add(new TextBlock { Text = "Model endpoint and credentials remain in General. Local-only mode is a safety guard for environments where remote model hosts are not allowed.", TextWrapping = TextWrapping.Wrap, FontSize = 11, Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 113, 130, 134)), Margin = new Thickness(4, 2, 4, 0) });
        }

        void BuildChatsPage()
        {
            chatRestoreButtons.Clear();
            var archived = _sessions.Where(session => session.Archived).OrderByDescending(session => session.UpdatedAt).ToArray();
            var count = new TextBlock { Text = archived.Length == 1 ? "1 archived conversation" : $"{archived.Length} archived conversations", FontSize = 12, Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 78, 105, 111)), Margin = new Thickness(0, 0, 0, 4) };
            pageBody.Children.Add(MakeSettingsCard("Archived chats", "Archived chats stay saved on this device and are hidden from the main conversation list until restored.", count));
            if (archived.Length == 0)
            {
                pageBody.Children.Add(new TextBlock { Text = "Nothing is archived yet. Use a chat’s menu to move it out of Recents; it will appear here so you can restore it later.", TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 113, 130, 134)), Margin = new Thickness(4, 2, 4, 8) });
            }
            foreach (var session in archived)
            {
                var restore = new Button { Content = "Restore", Padding = new Thickness(10, 5, 10, 5) };
                AutomationProperties.SetName(restore, $"Restore archived chat {session.Title}");
                restore.Click += (_, _) =>
                {
                    ToggleSessionArchive(session.Id);
                    BuildChatsPage();
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        if (chatRestoreButtons.FirstOrDefault()?.Focus(FocusState.Programmatic) != true)
                        {
                            var chatsSection = sectionButtons.FirstOrDefault(item => item.SearchText.Contains("Chats & history", StringComparison.OrdinalIgnoreCase));
                            chatsSection.Button.Focus(FocusState.Programmatic);
                        }
                    });
                };
                chatRestoreButtons.Add(restore);
                pageBody.Children.Add(MakeSettingsCard(session.Title, $"{session.Mode} · updated {session.UpdatedAt.ToLocalTime():g}", restore));
            }
            pageBody.Children.Add(new TextBlock { Text = "CHAT ARCHIVE", Style = UiTheme.Style("SectionLabelStyle"), Margin = new Thickness(0, 12, 0, 0) });
            var import = new Button { Content = "Import chats" };
            var export = new Button { Content = "Export chats" };
            import.Click += (_, _) => { actionAfterClose = () => RunUiActionAsync(ImportChatsAsync); dialog.Hide(); };
            export.Click += (_, _) => { actionAfterClose = () => RunUiActionAsync(ExportChatsAsync); dialog.Hide(); };
            pageBody.Children.Add(MakeSettingsCard("Import conversations", "Add chats from a supported local archive.", import));
            pageBody.Children.Add(MakeSettingsCard("Export conversations", "Create a portable archive of saved chats.", export));
        }

        async Task RefreshSettingsModelsAsync(StackPanel inventory, TextBlock status, Button refresh)
        {
            refresh.IsEnabled = false;
            status.Text = "Refreshing local model inventory…";
            try
            {
                var models = await _modelLibrary.DiscoverLocalModelsAsync(_settings.Endpoint);
                inventory.Children.Clear();
                if (models.Count == 0)
                {
                    inventory.Children.Add(new TextBlock { Text = "No downloaded models were found on the configured local runtimes. Start Ollama, LM Studio, or another compatible server, then refresh.", TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 113, 130, 134)) });
                }
                foreach (var model in models)
                {
                    var performance = (_settings.LocalModelPerformance ?? []).GetValueOrDefault(LocalModelPerformance.Key(model.Endpoint, model.Model));
                    var isPinned = (_settings.PinnedModelConnections ?? []).Any(item => item.Model.Equals(model.Model, StringComparison.OrdinalIgnoreCase) && item.Endpoint.Equals(model.Endpoint, StringComparison.OrdinalIgnoreCase));
                    var metrics = performance is null
                        ? "No live performance sample yet"
                        : $"LIVE · ≈{performance.EstimatedTokensPerSecond:0.#} tok/s · {performance.TimeToFirstTokenMilliseconds} ms first token · {performance.Samples} sample{(performance.Samples == 1 ? "" : "s")} · updated {performance.LastMeasuredAt.ToLocalTime():g}";
                    var pin = new Button { Content = isPinned ? "Unpin" : "Pin", Padding = new Thickness(9, 5, 9, 5) };
                    var remove = new Button { Content = "Delete", Padding = new Thickness(9, 5, 9, 5), IsEnabled = model.Provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase) };
                    AutomationProperties.SetName(pin, $"{(isPinned ? "Unpin" : "Pin")} {model.Model}");
                    AutomationProperties.SetName(remove, $"Delete {model.Model}");
                    pin.Click += (_, _) =>
                    {
                        _settings.PinnedModelConnections ??= [];
                        if (isPinned)
                            _settings.PinnedModelConnections.RemoveAll(item => item.Model.Equals(model.Model, StringComparison.OrdinalIgnoreCase) && item.Endpoint.Equals(model.Endpoint, StringComparison.OrdinalIgnoreCase));
                        else if (!_settings.PinnedModelConnections.Any(item => item.Model.Equals(model.Model, StringComparison.OrdinalIgnoreCase) && item.Endpoint.Equals(model.Endpoint, StringComparison.OrdinalIgnoreCase)))
                            _settings.PinnedModelConnections.Add(new PinnedModelConnection(model.Provider, model.Model, model.Endpoint));
                        SaveSettings();
                        _ = RefreshSettingsModelsAsync(inventory, status, refresh);
                    };
                    remove.Click += async (_, _) =>
                    {
                        if (!model.Provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase)) return;
                        var confirm = new ContentDialog
                        {
                            Title = $"Delete {model.Model}?",
                            Content = "This removes the model from the local Ollama runtime. Your conversations and settings remain unchanged.",
                            PrimaryButtonText = "Delete model",
                            CloseButtonText = "Cancel",
                            XamlRoot = XamlRoot
                        };
                        if (await confirm.ShowAsync() != ContentDialogResult.Primary) return;
                        try
                        {
                            await _modelLibrary.DeleteOllamaAsync(model.Endpoint, model.Model);
                            _settings.PinnedModelConnections?.RemoveAll(item => item.Model.Equals(model.Model, StringComparison.OrdinalIgnoreCase) && item.Endpoint.Equals(model.Endpoint, StringComparison.OrdinalIgnoreCase));
                            SaveSettings();
                            status.Text = $"Deleted {model.Model}.";
                            await RefreshSettingsModelsAsync(inventory, status, refresh);
                        }
                        catch (Exception ex) { status.Text = $"Could not delete {model.Model}: {ex.Message}"; }
                    };
                    var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { pin, remove } };
                    inventory.Children.Add(MakeSettingsCard($"{model.Model} · {model.Provider}", metrics, actions));
                }
                status.Text = $"{models.Count} downloaded model(s) · refreshing live performance every 5 seconds";
            }
            catch (Exception ex) { status.Text = $"Could not refresh local models: {ex.Message}"; }
            finally { refresh.IsEnabled = true; }
        }

        var modelRefreshTimer = DispatcherQueue.CreateTimer();
        var modelTimerAttached = false;
        StackPanel? activeModelInventory = null;
        TextBlock? activeModelStatus = null;
        Button? activeModelRefresh = null;
        modelRefreshTimer.Interval = TimeSpan.FromSeconds(5);
        dialog.Closed += (_, _) => modelRefreshTimer.Stop();

        void BuildModelsPage()
        {
            var manage = new Button { Content = "Open Model Library" };
            manage.Click += (_, _) => { actionAfterClose = () => OpenModelLibraryAndRestoreFocusAsync(false, ModelButton); dialog.Hide(); };
            var refresh = new Button { Content = "Refresh now" };
            var status = new TextBlock { FontSize = 11, TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 92, 111, 116)) };
            var inventory = new StackPanel { Spacing = 8 };
            pageBody.Children.Add(MakeSettingsCard("Models & downloads", "Downloaded models from local runtimes appear below. Pin models for the composer, delete Ollama models, and watch live performance samples update as you use them.", manage));
            pageBody.Children.Add(new TextBlock { Text = "DOWNLOADED MODELS", Style = UiTheme.Style("SectionLabelStyle"), Margin = new Thickness(0, 12, 0, 2) });
            pageBody.Children.Add(refresh);
            pageBody.Children.Add(status);
            pageBody.Children.Add(inventory);
            activeModelInventory = inventory;
            activeModelStatus = status;
            activeModelRefresh = refresh;
            refresh.Click += async (_, _) => await RefreshSettingsModelsAsync(inventory, status, refresh);
            _ = RefreshSettingsModelsAsync(inventory, status, refresh);
            modelRefreshTimer.Start();
            if (!modelTimerAttached)
            {
                modelTimerAttached = true;
                modelRefreshTimer.Tick += async (_, _) =>
                {
                    if (activeModelInventory is not null && activeModelStatus is not null && activeModelRefresh is not null)
                        await RefreshSettingsModelsAsync(activeModelInventory, activeModelStatus, activeModelRefresh);
                };
            }
        }

        void BuildGitHubPage()
        {
            var open = new Button { Content = "Open GitHub workspace" };
            open.Click += (_, _) => { actionAfterClose = () => RunUiActionAsync(GitHubWorkspaceDialogAsync); dialog.Hide(); };
            pageBody.Children.Add(MakeSettingsCard("GitHub workspace", "Sign in, browse or enter a repository, clone it as a project, and review commit or push changes.", open));
        }

        void BuildAdvancedPage()
        {
            pageBody.Children.Add(new TextBlock { Text = "VISION", Style = UiTheme.Style("SectionLabelStyle"), Margin = new Thickness(0, 0, 0, 2) });
            pageBody.Children.Add(visionFormat);
            pageBody.Children.Add(visionNote);
            pageBody.Children.Add(new TextBlock { Text = "DIAGNOSTICS", Style = UiTheme.Style("SectionLabelStyle"), Margin = new Thickness(0, 14, 0, 2) });
            pageBody.Children.Add(MakeSettingsCard("Support bundle", "Export a diagnostics archive with sensitive values redacted.", exportLogs));
        }

        AddSection("General", "connection endpoint model", "General", "Choose which local model server and model the app uses by default.", BuildGeneralPage);
        AddSection("Appearance", "accent color density customize interface", "Appearance", "Tune the workspace colors and spacing for the way you work.", BuildAppearancePage);
        AddSection("Pets", "companion mascot notifications approval", "Pets", "Choose your companion and decide when it should speak up.", BuildPetsPage);
        AddSection("Host configuration", "device runtime local only safety", "Host configuration", "Name this workspace and control whether remote model endpoints are allowed.", BuildHostPage);
        AddSection("Chats & history", "archive import export conversations", "Chats & history", "Organize, restore, import, and export your local conversations.", BuildChatsPage);
        AddSection("Models & downloads", "installed library benchmark pin", "Models & downloads", "Manage installed models and the models shown in the composer switcher.", BuildModelsPage);
        AddSection("GitHub workspace", "repositories coding agent clone push", "GitHub workspace", "Manage repository access and project setup.", BuildGitHubPage);
        AddSection("Advanced", "vision diagnostics support", "Advanced", "Configure image input behavior and local diagnostics.", BuildAdvancedPage);
        search.TextChanged += (_, _) =>
        {
            var query = search.Text.Trim();
            var visible = 0;
            foreach (var item in sectionButtons)
            {
                item.Button.Visibility = query.Length == 0 || item.SearchText.Contains(query, StringComparison.OrdinalIgnoreCase)
                    ? Visibility.Visible : Visibility.Collapsed;
                if (item.Button.Visibility == Visibility.Visible) visible++;
            }
            noSearchMatches.Visibility = visible == 0 ? Visibility.Visible : Visibility.Collapsed;
        };
        SelectSection(sectionButtons[0].Button, "General", "Choose which local model server and model the app uses by default.", BuildGeneralPage);
        dialog.Opened += (_, _) => sectionButtons[0].Button.Focus(FocusState.Programmatic);

        openRouter.Click += (_, _) =>
        {
            endpoint.Text = "https://openrouter.ai/api/v1";
            model.Text = "openrouter/free";
            checkStatus.Text = "OpenRouter free-model routing selected. Enter your API key, then check the connection.";
            openRouterKey.Focus(FocusState.Programmatic);
        };
        checkConnection.Click += async (_, _) =>
        {
            var candidateEndpoint = endpoint.Text.Trim().TrimEnd('/');
            if (string.IsNullOrWhiteSpace(candidateEndpoint))
            {
                checkStatus.Text = "Enter a server address before checking the connection.";
                return;
            }

            checkConnection.IsEnabled = false;
            checkStatus.Text = "Checking local model servers…";
            try
            {
                var models = await _provider.DiscoverModelsAsync(candidateEndpoint, apiKey: openRouterKey.Password.Trim());
                checkStatus.Text = models.Count switch
                {
                    0 => "No local models responded. Start the server, confirm its address, then check again. Models in navigation can help you find compatible downloads.",
                    1 => $"Connected: found {models[0]} at this endpoint.",
                    _ => $"Connected: found {models.Count} models at this endpoint. Save this connection, or use Models in navigation to browse the catalog."
                };
            }
            catch (Exception ex)
            {
                checkStatus.Text = $"Could not check this connection: {ex.Message}";
            }
            finally
            {
                checkConnection.IsEnabled = true;
            }
        };
        exportLogs.Click += (_, _) =>
        {
            try
            {
                var destination = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Server Rock diagnostics");
                var archive = App.Log.Export(destination);
                checkStatus.Text = "Redacted diagnostics exported. You can attach the archive to support.";
                RuntimeStatus.Text = $"Diagnostics exported to {archive}";
            }
            catch (Exception ex)
            {
                checkStatus.Text = $"Could not export diagnostics: {ex.Message}";
            }
        };
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            if (string.IsNullOrWhiteSpace(endpoint.Text) || string.IsNullOrWhiteSpace(model.Text))
            {
                RuntimeStatus.Text = "Enter both a server address and model ID.";
                return;
            }
            MigrateLegacyPinnedModels();
            _settings.Endpoint = endpoint.Text.Trim().TrimEnd('/');
            var endpointIsLocal = Uri.TryCreate(_settings.Endpoint, UriKind.Absolute, out var savedEndpoint) && savedEndpoint.IsLoopback;
            if (localOnly.IsChecked == true && !endpointIsLocal)
            {
                RuntimeStatus.Text = "Local-only mode is enabled, but the endpoint is not a local address. No connection changes were saved.";
                return;
            }
            _settings.Model = model.Text.Trim();
            _settings.ModelProvider = _settings.Endpoint.Contains("openrouter.ai", StringComparison.OrdinalIgnoreCase) ? "OpenRouter" : "Custom endpoint";
            _openRouterApiKey = _settings.ModelProvider == "OpenRouter" ? openRouterKey.Password.Trim() : string.Empty;
            _settings.SmartPromptRewritingEnabled = smartRewrite.IsChecked == true;
            _settings.VisionApiFormat = visionFormat.SelectedIndex switch { 1 => "openai", 2 => "ollama", _ => "auto" };
            _settings.AccentColor = accent.SelectedIndex switch { 1 => "violet", 2 => "green", 3 => "orange", _ => "cyan" };
            _settings.InterfaceDensity = density.SelectedIndex == 1 ? "compact" : "comfortable";
            _settings.HostLabel = string.IsNullOrWhiteSpace(hostLabel.Text) ? "This device" : hostLabel.Text.Trim();
            _settings.RequireLocalModelEndpoints = localOnly.IsChecked == true;
            _companionState = _companionState with
            {
                IsVisible = showPet.IsChecked == true,
                NotifyOnCompletion = petCompletion.IsChecked == true,
                NotifyOnErrors = petErrors.IsChecked == true,
                AskBeforeActions = petQuestions.IsChecked == true
            };
            SaveSettings();
            SaveCompanionState();
            ApplySavedCustomization();
            UpdateCompanionDisplay();
            UpdateModelLabel();
            UpdateSmartRewriteButton();
            await RefreshModelConnectionAsync();
        }
        if (actionAfterClose is not null) await actionAfterClose();
    }
}
