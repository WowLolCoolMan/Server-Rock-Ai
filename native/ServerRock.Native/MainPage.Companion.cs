// The desktop pet companion: a small draggable sprite over the page that changes state
// (idle, thinking, coding, success, error, …) and shows a one-line speech bubble.
// Covers choosing a pet, custom per-state sprites, drag/keyboard positioning, and saving
// those choices.
// Depends on: CompanionStateStore / CompanionState (companion.json), the Companion* elements in
// MainPage.xaml, and _companionState / _companionPersistenceAvailable in MainPage.xaml.cs.
// Called by: pointer/keyboard/click handlers in MainPage.xaml, and chat/connection code that
// calls SetCompanionState, UpdateCompanionSpeech, or SetCompanionCompletionSummary.

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private sealed record CompanionChoice(string Id, string Name, string Description, string AssetUri, string SpriteSheetUri);
    private static readonly CompanionChoice[] CompanionChoices =
    [
        new("rocky", "Rocky", "A steady rock when the diff gets large.", "ms-appx:///Assets/Svg/rocky-idle.png", "ms-appx:///Assets/Svg/rocky-sprite-sheet.png"),
        new("byte", "Byte", "A tiny blue-screen gremlin.", "ms-appx:///Assets/Svg/byte-idle.png", "ms-appx:///Assets/Svg/byte-sprite-sheet.png"),
        new("sprout", "Sprout", "Small green shoots for new ideas.", "ms-appx:///Assets/Svg/sprout-idle.png", "ms-appx:///Assets/Svg/sprout-sprite-sheet.png"),
        new("nova", "Nova", "A focused little star for big ideas.", "ms-appx:///Assets/Svg/nova-idle.png", "ms-appx:///Assets/Svg/nova-sprite-sheet.png"),
        new("ember", "Ember", "A fiery friend for ambitious builds.", "ms-appx:///Assets/Svg/ember-idle.png", "ms-appx:///Assets/Svg/ember-sprite-sheet.png")
    ];
    private static readonly string[] CompanionStates = CompanionStateStore.AllowedStates;

    private void CompanionOverlayLoaded(object sender, RoutedEventArgs e) => PositionCompanion(persistIfChanged: false);

    private void CompanionOverlaySizeChanged(object sender, SizeChangedEventArgs e) => PositionCompanion(persistIfChanged: true);

    private void UpdateCompanionDisplay()
    {
        var choice = CompanionChoices.FirstOrDefault(item => item.Id == _companionState.PetId) ?? CompanionChoices[0];
        _companionState = _companionState with { PetId = choice.Id };
        CompanionImage.Source = ResolveCompanionAsset(choice);
        CompanionName.Text = choice.Name;
        CompanionOverlay.Visibility = _companionState.IsVisible ? Visibility.Visible : Visibility.Collapsed;
        CompanionButtonLabel.Text = _companionState.IsVisible ? "Hide pet" : "Show pet";
        AutomationProperties.SetName(CompanionButton, _companionState.IsVisible ? "Hide pet companion" : "Show pet companion");
        if (_companionState.IsVisible) PositionCompanion(persistIfChanged: false);
    }

    private ImageSource ResolveCompanionAsset(CompanionChoice choice)
    {
        if (_companionState.StateAssets?.TryGetValue(_companionState.CurrentState, out var customPath) == true && File.Exists(customPath))
        {
            try
            {
                return Path.GetExtension(customPath).Equals(".svg", StringComparison.OrdinalIgnoreCase)
                    ? new SvgImageSource { UriSource = new Uri(customPath) }
                    : new BitmapImage { UriSource = new Uri(customPath) };
            }
            catch { }
        }
        if (!_companionState.CurrentState.Equals("idle", StringComparison.OrdinalIgnoreCase))
        {
            try { return new BitmapImage { UriSource = new Uri($"ms-appx:///Assets/Svg/{choice.Id}-{_companionState.CurrentState}.png") }; }
            catch { }
        }
        return choice.AssetUri.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
            ? new SvgImageSource { UriSource = new Uri(choice.AssetUri) }
            : new BitmapImage { UriSource = new Uri(choice.AssetUri) };
    }

    private void SetCompanionState(string state)
    {
        var normalized = CompanionStates.Contains(state, StringComparer.OrdinalIgnoreCase) ? state.ToLowerInvariant() : "idle";
        if (_companionState.CurrentState.Equals(normalized, StringComparison.OrdinalIgnoreCase)) return;
        _companionState = _companionState with { CurrentState = normalized };
        UpdateCompanionDisplay();
    }

    private void UpdateCompanionSpeech(string message)
    {
        var normalized = message?.Trim() ?? string.Empty;
        var looksLikeError = normalized.StartsWith("Error", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("Could not", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("blocked", StringComparison.OrdinalIgnoreCase);
        if (looksLikeError && !_companionState.NotifyOnErrors) return;
        if (normalized.StartsWith("Done", StringComparison.OrdinalIgnoreCase) && !_companionState.NotifyOnCompletion) return;
        var safe = string.IsNullOrWhiteSpace(message) ? "Ready for your next prompt." : message.Trim();
        if (safe.Length > 240) safe = safe[..237].TrimEnd() + "…";
        CompanionSpeech.Text = safe;
    }

    private void SetCompanionCompletionSummary(string response)
    {
        if (!_companionState.NotifyOnCompletion) return;
        var plain = System.Text.RegularExpressions.Regex.Replace(response ?? string.Empty, "[`*_>#]", string.Empty);
        plain = System.Text.RegularExpressions.Regex.Replace(plain, "\\s+", " ").Trim();
        if (plain.Length == 0)
        {
            SetCompanionState("success");
            UpdateCompanionSpeech("Done · completed a response.");
            return;
        }
        var sentenceEnd = plain.IndexOfAny(['.', '!', '?']);
        var summary = sentenceEnd is > 0 and < 180 ? plain[..(sentenceEnd + 1)] : plain;
        SetCompanionState("success");
        UpdateCompanionSpeech("Done · " + summary);
    }

    private void CompanionImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        RuntimeStatus.Text = $"The companion artwork could not be loaded: {e.ErrorMessage}";
    }

    private void PositionCompanion(bool persistIfChanged)
    {
        if (CompanionOverlay.ActualWidth <= 0 || CompanionOverlay.ActualHeight <= 0) return;
        var width = Math.Min(218, Math.Max(170, CompanionOverlay.ActualWidth - 16));
        CompanionCard.Width = width;
        var cardHeight = CompanionCard.ActualHeight > 0 ? CompanionCard.ActualHeight : 68;
        var maxLeft = Math.Max(8, CompanionOverlay.ActualWidth - width - 8);
        var maxTop = Math.Max(8, CompanionOverlay.ActualHeight - cardHeight - 8);

        // On compact windows the companion must not sit over the welcome prompt
        // or the message composer. Keep it in the otherwise-unused strip below
        // the header for that layout, but retain the person's desktop position
        // so it returns when the window has room again.
        if (_compactNavigation)
        {
            Canvas.SetLeft(CompanionCard, maxLeft);
            // Keep clear of the header's connection control while preserving the
            // welcome panel's first line of sight below the compact overlay.
            Canvas.SetTop(CompanionCard, Math.Clamp(Header.ActualHeight + 16, 8, maxTop));
            return;
        }

        var left = _companionState.Left < 0 ? maxLeft : Math.Clamp(_companionState.Left, 8, maxLeft);
        var top = Math.Clamp(_companionState.Top, 8, maxTop);
        Canvas.SetLeft(CompanionCard, left);
        Canvas.SetTop(CompanionCard, top);
        if (persistIfChanged && (Math.Abs(left - _companionState.Left) > 0.5 || Math.Abs(top - _companionState.Top) > 0.5))
        {
            _companionState = _companionState with { Left = left, Top = top };
            SaveCompanionState();
        }
        else if (!persistIfChanged && _companionState.Left < 0)
        {
            _companionState = _companionState with { Left = left, Top = top };
        }
    }

    private void SetCompanionPosition(double left, double top)
    {
        if (CompanionOverlay.ActualWidth <= 0 || CompanionOverlay.ActualHeight <= 0) return;
        var maxLeft = Math.Max(8, CompanionOverlay.ActualWidth - CompanionCard.Width - 8);
        var maxTop = Math.Max(8, CompanionOverlay.ActualHeight - Math.Max(68, CompanionCard.ActualHeight) - 8);
        left = Math.Clamp(left, 8, maxLeft);
        top = Math.Clamp(top, 8, maxTop);
        Canvas.SetLeft(CompanionCard, left);
        Canvas.SetTop(CompanionCard, top);
        _companionState = _companionState with { Left = left, Top = top };
    }

    private void SaveCompanionState()
    {
        if (!_companionPersistenceAvailable)
        {
            RuntimeStatus.Text = "Pet preferences are not being saved because the existing preference file could not be read.";
            return;
        }
        try { _companionStore.Save(_companionState); }
        catch (Exception ex) { RuntimeStatus.Text = $"Pet preference change is active for this session but could not be saved: {ex.Message}"; }
    }

    private void ToggleCompanionClick(object sender, RoutedEventArgs e)
    {
        _companionState = _companionState with { IsVisible = !_companionState.IsVisible };
        // UpdateCompanionDisplay also shows or hides the overlay from IsVisible.
        UpdateCompanionDisplay();
        SaveCompanionState();
        RuntimeStatus.Text = _companionState.IsVisible ? $"{CompanionName.Text} is visible as a draggable sprite." : "Pet companion hidden.";
    }

    private void HideCompanionClick(object sender, RoutedEventArgs e)
    {
        if (!_companionState.IsVisible) return;
        _companionState = _companionState with { IsVisible = false };
        UpdateCompanionDisplay();
        SaveCompanionState();
        RuntimeStatus.Text = "Pet companion hidden.";
    }

    private async void CustomizeCompanionClick(object sender, RoutedEventArgs e) => await RunUiActionAsync(CustomizeCompanionAsync);

    private async Task CustomizeCompanionAsync()
    {
        string? selectedId = null;
        var configureRequested = false;
        var choices = new StackPanel { Spacing = 7 };
        var dialog = new ContentDialog { Title = "Choose a companion", Content = choices, CloseButtonText = "Done", XamlRoot = XamlRoot };
        foreach (var choice in CompanionChoices)
        {
            var row = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(54) }, new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, new ColumnDefinition { Width = GridLength.Auto } }, ColumnSpacing = 9 };
            row.Children.Add(new Image { Source = new SvgImageSource { UriSource = new Uri(choice.AssetUri) }, Width = 42, Height = 42, Stretch = Stretch.Uniform, VerticalAlignment = VerticalAlignment.Center });
            var details = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Spacing = 2 };
            Grid.SetColumn(details, 1);
            details.Children.Add(new TextBlock { Text = choice.Name, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 40, 59, 65)) });
            details.Children.Add(new TextBlock { Text = choice.Description, FontSize = 11, Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 112, 128, 133)), TextWrapping = TextWrapping.Wrap });
            details.Children.Add(new TextBlock { Text = "Includes a 4×4 state sprite sheet", FontSize = 10, Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 21, 126, 142)) });
            row.Children.Add(details);
            var selected = new TextBlock { Text = choice.Id == _companionState.PetId ? "Selected" : "", FontSize = 10, Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 21, 126, 142)), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(selected, 2);
            row.Children.Add(selected);
            var option = new Button { Content = row, HorizontalContentAlignment = HorizontalAlignment.Stretch, Background = choice.Id == _companionState.PetId ? new SolidColorBrush(ColorHelper.FromArgb(255, 235, 247, 247)) : new SolidColorBrush(ColorHelper.FromArgb(255, 255, 255, 255)), BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 220, 231, 232)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Padding = new Thickness(10, 7, 10, 7) };
            AutomationProperties.SetName(option, $"Choose {choice.Name} companion");
            AutomationProperties.SetHelpText(option, choice.Description);
            option.Click += (_, _) => { selectedId = choice.Id; dialog.Hide(); };
            choices.Children.Add(option);
        }

        var configure = new Button { Content = "Configure custom state sprites…", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 0) };
        AutomationProperties.SetName(configure, "Configure custom state sprites");
        configure.Click += (_, _) => { configureRequested = true; dialog.Hide(); };
        choices.Children.Add(configure);

        await dialog.ShowAsync();
        if (configureRequested)
        {
            await ConfigureCompanionStatesAsync();
            return;
        }
        if (selectedId is null) return;
        _companionState = _companionState with { PetId = selectedId };
        UpdateCompanionDisplay();
        SaveCompanionState();
        RuntimeStatus.Text = $"Selected {CompanionName.Text} as your local companion.";
    }

    private async Task ConfigureCompanionStatesAsync()
    {
        var statePicker = new ComboBox { Header = "Pet state", MinWidth = 260, ItemsSource = CompanionStates, SelectedIndex = Math.Max(0, Array.IndexOf(CompanionStates, _companionState.CurrentState)) };
        var choose = new Button { Content = "Choose local sprite…", HorizontalAlignment = HorizontalAlignment.Left };
        var clear = new Button { Content = "Clear custom sprite", HorizontalAlignment = HorizontalAlignment.Left };
        var status = new TextBlock { Text = "Choose an SVG or image for the selected state. Changes are stored locally on this device.", TextWrapping = TextWrapping.Wrap, Opacity = 0.8 };
        var panel = new StackPanel { Spacing = 10 };
        panel.Children.Add(statePicker); panel.Children.Add(choose); panel.Children.Add(clear); panel.Children.Add(status);
        var dialog = new ContentDialog { Title = "Custom pet state sprites", Content = panel, CloseButtonText = "Done", XamlRoot = XamlRoot };
        choose.Click += async (_, _) =>
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker { SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary, ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail };
                foreach (var extension in new[] { ".svg", ".png", ".jpg", ".jpeg", ".webp" }) picker.FileTypeFilter.Add(extension);
#if WINDOWS
                var window = (Application.Current as App)?.MainWindow;
                if (window is null) throw new InvalidOperationException("The app window is unavailable.");
                WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(window));
#endif
                var file = await picker.PickSingleFileAsync();
                if (file is null) return;
                var state = statePicker.SelectedItem?.ToString() ?? "idle";
                var assets = _companionState.StateAssets is null
                    ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(_companionState.StateAssets, StringComparer.OrdinalIgnoreCase);
                assets[state] = file.Path;
                _companionState = _companionState with { StateAssets = assets };
                SaveCompanionState();
                UpdateCompanionDisplay();
                status.Text = $"{state}: {file.Name} · saved locally";
            }
            catch (Exception ex) { status.Text = $"Could not choose a sprite: {ex.Message}"; }
        };
        clear.Click += (_, _) =>
        {
            var state = statePicker.SelectedItem?.ToString() ?? "idle";
            var assets = _companionState.StateAssets is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(_companionState.StateAssets, StringComparer.OrdinalIgnoreCase);
            assets.Remove(state);
            _companionState = _companionState with { StateAssets = assets };
            SaveCompanionState(); UpdateCompanionDisplay(); status.Text = $"{state}: using the built-in fallback sprite.";
        };
        await dialog.ShowAsync();
    }

    private void CompanionPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(CompanionOverlay).Position;
        _companionDragOffset = new Point(point.X - Canvas.GetLeft(CompanionCard), point.Y - Canvas.GetTop(CompanionCard));
        _isCompanionDragging = true;
        CompanionImage.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void CompanionPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isCompanionDragging) return;
        var point = e.GetCurrentPoint(CompanionOverlay).Position;
        SetCompanionPosition(point.X - _companionDragOffset.X, point.Y - _companionDragOffset.Y);
        e.Handled = true;
    }

    private void CompanionPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isCompanionDragging) return;
        _isCompanionDragging = false;
        CompanionImage.ReleasePointerCapture(e.Pointer);
        SaveCompanionState();
        e.Handled = true;
    }

    private void CompanionPointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        if (!_isCompanionDragging) return;
        _isCompanionDragging = false;
        SaveCompanionState();
    }

    private void CompanionKeyDown(object sender, KeyRoutedEventArgs e)
    {
        var delta = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down) ? 48 : 16;
        var left = Canvas.GetLeft(CompanionCard);
        var top = Canvas.GetTop(CompanionCard);
        switch (e.Key)
        {
            case Windows.System.VirtualKey.Left: left -= delta; break;
            case Windows.System.VirtualKey.Right: left += delta; break;
            case Windows.System.VirtualKey.Up: top -= delta; break;
            case Windows.System.VirtualKey.Down: top += delta; break;
            default: return;
        }
        SetCompanionPosition(left, top);
        SaveCompanionState();
        e.Handled = true;
    }
}
