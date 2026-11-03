using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using ServerRock.Core.Models;
using ServerRock.Core.Services;
using ServerRock.Core.Infrastructure;

namespace ServerRock.Native;

public sealed partial class MainPage : Page
{
    private readonly List<ChatMessage> _messages = [];
    private readonly OpenAiCompatibleProvider _provider = new();
    private readonly JsonSettingsStore _settingsStore = new();
    private AppSettings _settings;
    private string _mode = "ChatBot";
    private string? _project;
    private CancellationTokenSource? _requestCancellation;

    public MainPage()
    {
        InitializeComponent();
        _settings = _settingsStore.Load();
        UpdateModelLabel();
        RuntimeStatus.Text = $"⌖　{_settings.Endpoint}";
    }

    private void NavigateClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string destination }) return;
        PageTitle.Text = destination switch { "images" => "Images", "activity" => "Activity", _ => _project ?? "New chat" };
        RuntimeStatus.Text = destination switch { "images" => "Image generation is not connected yet", "activity" => "Recent conversations are being migrated", _ => $"⌖　{_settings.Endpoint}" };
    }

    private async void ModeClick(object sender, RoutedEventArgs e)
    {
        var choice = new ComboBox { ItemsSource = new[] { "ChatBot", "Coding Agent" }, SelectedItem = _mode, MinWidth = 220 };
        var description = new TextBlock { TextWrapping = TextWrapping.Wrap, Opacity = 0.82, Text = ModeDescription(_mode) };
        choice.SelectionChanged += (_, _) => description.Text = ModeDescription(choice.SelectedItem as string);
        var content = new StackPanel { Spacing = 10 };
        content.Children.Add(choice);
        content.Children.Add(description);
        var dialog = new ContentDialog { Title = "Choose a workspace", Content = content, PrimaryButtonText = "Continue", CloseButtonText = "Cancel", XamlRoot = XamlRoot };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary || choice.SelectedItem is not string selected) return;
        _mode = selected;
        ModeButton.Content = selected + "　⌄";
        if (selected == "Coding Agent" && _project is null) await NewProjectClickAsync();
    }

    private static string ModeDescription(string? mode) => mode == "Coding Agent"
        ? "The native Coding Agent is still being migrated. It can chat about code, but it cannot read or change workspace files yet."
        : "Chat with the selected model. You can add project instructions for context.";

    private async void NewProjectClick(object sender, RoutedEventArgs e) => await NewProjectClickAsync();

    private async Task NewProjectClickAsync()
    {
        var name = new TextBox { PlaceholderText = "Project name" };
        var instructions = new TextBox { PlaceholderText = "What should the assistant know about this project?", AcceptsReturn = true, Height = 76, TextWrapping = TextWrapping.Wrap };
        var panel = new StackPanel { Spacing = 10 };
        panel.Children.Add(name);
        panel.Children.Add(instructions);
        var dialog = new ContentDialog { Title = "Create project", Content = panel, PrimaryButtonText = "Create", CloseButtonText = "Cancel", XamlRoot = XamlRoot };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(name.Text)) return;
        _project = name.Text.Trim();
        _settings.ProjectInstructions = instructions.Text.Trim();
        SaveSettings();
        PageTitle.Text = _project;
        ProjectBadge.Visibility = Visibility.Visible;
        ProjectList.Children.Add(new Button { Content = "▱　" + _project, HorizontalContentAlignment = HorizontalAlignment.Left, Background = new SolidColorBrush(ColorHelper.FromArgb(255, 43, 45, 39)), Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 231, 232, 222)), Padding = new Thickness(10, 8, 10, 8), CornerRadius = new CornerRadius(8) });
        _messages.Clear();
        ConversationMessages.Children.Clear();
        WelcomePanel.Visibility = Visibility.Visible;
        ConversationScroll.Visibility = Visibility.Collapsed;
    }

    private void NewChatClick(object sender, RoutedEventArgs e)
    {
        if (_requestCancellation is not null) CancelRequest();
        _messages.Clear();
        ConversationMessages.Children.Clear();
        WelcomePanel.Visibility = Visibility.Visible;
        ConversationScroll.Visibility = Visibility.Collapsed;
        PageTitle.Text = _project ?? "New chat";
        ProjectBadge.Visibility = _project is null ? Visibility.Collapsed : Visibility.Visible;
    }

    private void SuggestionClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string text })
        {
            PromptBox.Text = text;
            PromptBox.Focus(FocusState.Programmatic);
        }
    }

    private void PromptKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter && !Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
        {
            e.Handled = true;
            SendClick(sender, new RoutedEventArgs());
        }
    }

    private async void SendClick(object sender, RoutedEventArgs e)
    {
        var text = PromptBox.Text.Trim();
        if (text.Length == 0 || _requestCancellation is not null) return;
        WelcomePanel.Visibility = Visibility.Collapsed;
        ConversationScroll.Visibility = Visibility.Visible;
        _messages.Add(new ChatMessage("user", text));
        AddMessage("You", text, true);
        PromptBox.Text = string.Empty;
        var response = AddMessage("Server Rock", "", false);
        _messages.Add(new ChatMessage("assistant", ""));
        var assistantIndex = _messages.Count - 1;
        _requestCancellation = new CancellationTokenSource();
        SendButton.Visibility = Visibility.Collapsed;
        StopButton.Visibility = Visibility.Visible;
        RuntimeStatus.Text = $"Connecting to {_settings.Endpoint}…";
        try
        {
            await StreamChatAsync(response, assistantIndex, _requestCancellation.Token);
            RuntimeStatus.Text = $"⌖　{_settings.Model}";
        }
        catch (OperationCanceledException)
        {
            if (_requestCancellation?.IsCancellationRequested == true)
            {
                if (string.IsNullOrEmpty(_messages[assistantIndex].Content))
                {
                    _messages[assistantIndex] = new ChatMessage("assistant", "Generation stopped.");
                    response.Text = "Generation stopped.";
                }
                RuntimeStatus.Text = "Response stopped";
            }
            else RuntimeStatus.Text = "The model request timed out after five minutes.";
        }
        catch (Exception ex)
        {
            _messages.RemoveAt(assistantIndex);
            if (response.Parent is FrameworkElement stack && stack.Parent is Border bubble)
                ConversationMessages.Children.Remove(bubble);
            AddMessage("Connection error", FriendlyError(ex), false);
            RuntimeStatus.Text = "Connection failed · check Settings";
        }
        finally
        {
            _requestCancellation?.Dispose();
            _requestCancellation = null;
            SendButton.Visibility = Visibility.Visible;
            StopButton.Visibility = Visibility.Collapsed;
            PromptBox.Focus(FocusState.Programmatic);
        }
    }

    private async Task StreamChatAsync(TextBlock output, int assistantIndex, CancellationToken cancellationToken)
    {
        var answer = new System.Text.StringBuilder();
        await _provider.StreamChatAsync(_settings, BuildProviderMessages(), text =>
        {
            answer.Append(text);
            _messages[assistantIndex] = new ChatMessage("assistant", answer.ToString());
            output.Text = answer.ToString();
            ConversationScroll.ChangeView(null, double.MaxValue, null);
        }, cancellationToken);
    }

    private List<ChatMessage> BuildProviderMessages()
    {
        var messages = new List<ChatMessage>();
        var context = new List<string>();
        if (!string.IsNullOrWhiteSpace(_settings.ProjectInstructions)) context.Add(_settings.ProjectInstructions);
        if (context.Count > 0) messages.Add(new ChatMessage("system", string.Join("\n\n", context)));
        messages.AddRange(_messages);
        return messages;
    }

    private static string FriendlyError(Exception ex)
    {
        if (ex is HttpRequestException { StatusCode: not null }) return $"Model server error: {ex.Message}";
        if (ex is HttpRequestException) return $"Could not reach the model server. Start it, then check its address in Settings.\n\nDetails: {ex.Message}";
        if (ex is TaskCanceledException) return "The request timed out or was interrupted.";
        return ex.Message;
    }

    private TextBlock AddMessage(string author, string text, bool user)
    {
        var bubble = new Border { MaxWidth = user ? 620 : 760, HorizontalAlignment = user ? HorizontalAlignment.Right : HorizontalAlignment.Left, Background = new SolidColorBrush(user ? ColorHelper.FromArgb(255, 42, 48, 40) : ColorHelper.FromArgb(255, 17, 18, 16)), CornerRadius = new CornerRadius(12), Padding = new Thickness(14, 12, 14, 12) };
        var content = new StackPanel { Spacing = 6 };
        content.Children.Add(new TextBlock { Text = author, FontSize = 11, Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 166, 187, 145)) });
        var body = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 230, 231, 223)), IsTextSelectionEnabled = true };
        content.Children.Add(body);
        bubble.Child = content;
        ConversationMessages.Children.Add(bubble);
        ConversationScroll.ChangeView(null, double.MaxValue, null);
        return body;
    }

    private async void ModelClick(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog { Title = "Choose a local model", PrimaryButtonText = "Use selected model", SecondaryButtonText = "Refresh models", CloseButtonText = "Cancel", XamlRoot = XamlRoot };
        var models = new ListView { MinWidth = 320, MinHeight = 220, SelectionMode = ListViewSelectionMode.Single };
        dialog.Content = new StackPanel { Spacing = 8, Children = { new TextBlock { Text = $"From {_settings.Endpoint}" }, models } };
        try
        {
            await RefreshModelListAsync(models);
        }
        catch (Exception ex)
        {
            RuntimeStatus.Text = $"Model discovery failed: {ex.Message}";
        }
        while (true)
        {
            models.SelectedItem = _settings.Model;
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                if (models.SelectedItem is string selected)
                {
                    _settings.Model = selected;
                    SaveSettings();
                    UpdateModelLabel();
                }
                return;
            }
            if (result != ContentDialogResult.Secondary) return;
            try { await RefreshModelListAsync(models); }
            catch (Exception ex) { RuntimeStatus.Text = $"Model discovery failed: {ex.Message}"; }
        }
    }

    private async Task RefreshModelListAsync(ListView models)
    {
        var discovered = await _provider.DiscoverModelsAsync(_settings.Endpoint);
        models.ItemsSource = discovered;
        RuntimeStatus.Text = $"Found {discovered.Count} model(s) at {_settings.Endpoint}";
    }

    private void UpdateModelLabel() => ModelButton.Content = $"◉　{_settings.Model}　⌄";

    private void CancelRequest() => _requestCancellation?.Cancel();
    private void StopClick(object sender, RoutedEventArgs e) => CancelRequest();

    private async void AdvancedClick(object sender, RoutedEventArgs e)
    {
        var temperature = new Slider { Minimum = 0, Maximum = 2, Value = _settings.Temperature, StepFrequency = 0.1, Header = "Temperature" };
        var dialog = new ContentDialog { Title = "Advanced generation", Content = temperature, PrimaryButtonText = "Save", CloseButtonText = "Cancel", XamlRoot = XamlRoot };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            _settings.Temperature = temperature.Value;
            SaveSettings();
        }
    }

    private async void SettingsClick(object sender, RoutedEventArgs e)
    {
        var endpoint = new TextBox { Header = "OpenAI-compatible endpoint", Text = _settings.Endpoint, PlaceholderText = "http://localhost:11434/v1" };
        var model = new TextBox { Header = "Model ID", Text = _settings.Model, PlaceholderText = "Model ID reported by the server" };
        var note = new TextBlock { Text = "Use a local Ollama, LM Studio, Jan, GPT4All, or LocalAI server. API-key and hosted-provider connections are not enabled in the native app yet.", TextWrapping = TextWrapping.Wrap, Opacity = 0.8 };
        var panel = new StackPanel { Spacing = 10 };
        panel.Children.Add(endpoint);
        panel.Children.Add(model);
        panel.Children.Add(note);
        var dialog = new ContentDialog { Title = "Model connection", Content = panel, PrimaryButtonText = "Save", CloseButtonText = "Cancel", XamlRoot = XamlRoot };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        if (string.IsNullOrWhiteSpace(endpoint.Text) || string.IsNullOrWhiteSpace(model.Text))
        {
            RuntimeStatus.Text = "Enter both a server address and model ID.";
            return;
        }
        _settings.Endpoint = endpoint.Text.Trim().TrimEnd('/');
        _settings.Model = model.Text.Trim();
        SaveSettings();
        UpdateModelLabel();
        RuntimeStatus.Text = $"⌖　{_settings.Endpoint}";
    }

    private async void AttachClick(object sender, RoutedEventArgs e)
    {
        await new ContentDialog
        {
            Title = "File attachments",
            Content = "Native chat currently accepts text prompts. File picking and project-file context are still being migrated from the existing desktop app.",
            CloseButtonText = "Got it",
            XamlRoot = XamlRoot
        }.ShowAsync();
    }

    private async void HelpClick(object sender, RoutedEventArgs e)
    {
        await new ContentDialog
        {
            Title = "Using local chat",
            Content = "Enter a prompt and press Enter to send. Press Shift+Enter for a new line. Use the model button to refresh models from your configured OpenAI-compatible server. The stop button cancels a response in progress.",
            CloseButtonText = "Close",
            XamlRoot = XamlRoot
        }.ShowAsync();
    }

    private void SaveSettings()
    {
        try { _settingsStore.Save(_settings); }
        catch (Exception ex) { RuntimeStatus.Text = $"Could not save settings: {ex.Message}"; }
    }
}
