// Project terminal dialog: an interactive shell that starts in a folder of the active
// project. Output comes from a VT screen buffer and is shown read-only; input is only what the
// user types into the input box (model output is never sent to the shell).
// Depends on: WorkspaceTerminalSession (pseudo-terminal + screen buffer in SkaiDesktop.Core) and
// the active project's FolderPaths.
// Called by: the Terminal button in the Coding Agent workspace bar (MainPage.xaml).

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SkaiDesktop.Core.Infrastructure;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private async void OpenTerminalClick(object sender, RoutedEventArgs e)
    {
        try { await RunUiActionAsync(OpenTerminalClickAsync); }
        finally { RestoreNavigationDialogFocus(false, sender as Control); }
    }

    private async Task OpenTerminalClickAsync()
    {
        var project = _projects.FirstOrDefault(item => item.Id == _activeProjectId);
        var root = project?.FolderPaths.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            RuntimeStatus.Text = "Choose an existing project folder before opening its terminal.";
            return;
        }

        var shellPicker = new ComboBox { MinWidth = 180, SelectedIndex = 0 };
        shellPicker.Items.Add(OperatingSystem.IsWindows() ? "Command Prompt" : "Default shell");
        if (OperatingSystem.IsWindows()) shellPicker.Items.Add("PowerShell");
        var output = new TextBlock
        {
            FontFamily = new FontFamily("Consolas, Cascadia Mono, monospace"),
            FontSize = UiTheme.FontSize("SmallFontSize"),
            TextWrapping = TextWrapping.NoWrap,
            Foreground = UiTheme.Brush("BrandMarkInkBrush"),
            IsTextSelectionEnabled = true
        };
        var outputScroll = new ScrollViewer
        {
            Content = output,
            IsTabStop = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = UiTheme.Brush("BrandMarkBrush"),
            Padding = new Thickness(14),
            MinHeight = 300,
            MaxHeight = 520
        };
        AutomationProperties.SetName(outputScroll, "Project terminal output pane");
        AutomationProperties.SetHelpText(outputScroll, "Scrollable, read-only history from the shell running in the active project folder.");
        var input = new TextBox
        {
            PlaceholderText = "Type a command and press Enter",
            FontFamily = new FontFamily("Consolas, Cascadia Mono, monospace"),
            IsEnabled = false
        };
        AutomationProperties.SetName(input, "Project terminal input");
        AutomationProperties.SetHelpText(input, "Type into the shell running in the active project folder. Commands use your account permissions on this device.");
        var status = new TextBlock { Text = "Starting project shell…", FontSize = UiTheme.FontSize("CaptionFontSize"), Foreground = UiTheme.Brush("MutedInkBrush") };
        AutomationProperties.SetName(status, "Project terminal status");
        AutomationProperties.SetLiveSetting(status, AutomationLiveSetting.Polite);
        var cancel = new CancellationTokenSource();
        WorkspaceTerminalSession? session = null;
        var content = new Grid { RowSpacing = 10, RowDefinitions = { new RowDefinition { Height = GridLength.Auto }, new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }, new RowDefinition { Height = GridLength.Auto }, new RowDefinition { Height = GridLength.Auto } } };
        var top = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, new ColumnDefinition { Width = GridLength.Auto } }, ColumnSpacing = 10 };
        top.Children.Add(new TextBlock { Text = $"{project!.Name} · {root}", TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center, FontSize = UiTheme.FontSize("SmallFontSize"), Foreground = UiTheme.Brush("MutedInkBrush") });
        Grid.SetColumn(shellPicker, 1);
        top.Children.Add(shellPicker);
        content.Children.Add(top);
        Grid.SetRow(outputScroll, 1);
        content.Children.Add(outputScroll);
        var inputRow = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, new ColumnDefinition { Width = GridLength.Auto } }, ColumnSpacing = 8 };
        inputRow.Children.Add(input);
        var interrupt = new Button { Content = "Send Ctrl+C", IsEnabled = false, Style = (Style)Resources["ContextButtonStyle"] };
        AutomationProperties.SetName(interrupt, "Send Ctrl+C to project terminal");
        Grid.SetColumn(interrupt, 1);
        inputRow.Children.Add(interrupt);
        Grid.SetRow(inputRow, 2);
        content.Children.Add(inputRow);
        Grid.SetRow(status, 3);
        content.Children.Add(status);

        var dialog = new ContentDialog
        {
            Title = "Project terminal",
            Content = content,
            CloseButtonText = "Close terminal",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };
        dialog.Opened += (_, _) => input.Focus(FocusState.Programmatic);
        var queuedRender = 0;
        void RenderTerminal()
        {
            if (session is null || Interlocked.Exchange(ref queuedRender, 1) != 0) return;
            DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    if (session is null) return;
                    output.Text = session.ScreenText;
                    outputScroll.ChangeView(null, outputScroll.ScrollableHeight, null, true);
                    if (session.Completion.IsCompleted)
                    {
                        input.IsEnabled = false;
                        interrupt.IsEnabled = false;
                        status.Text = $"Shell exited (code {session.ExitCode?.ToString() ?? "unknown"}).";
                    }
                }
                finally { Interlocked.Exchange(ref queuedRender, 0); }
            });
        }
        try
        {
            session = new WorkspaceTerminalSession();
            session.ScreenChanged += (_, _) => RenderTerminal();
            await session.StartAsync(root, shell: OperatingSystem.IsWindows() && shellPicker.SelectedItem is string selected && selected == "PowerShell" ? selected : null, cancellationToken: cancel.Token);
            input.IsEnabled = true;
            interrupt.IsEnabled = true;
            status.Text = "Commands run on your computer with your account permissions, in this project folder.";
            input.TextChanged += async (_, args) =>
            {
                if (string.IsNullOrEmpty(input.Text) || session is null) return;
                var typed = input.Text;
                input.Text = string.Empty;
                try { await session.SendTextAsync(typed, cancel.Token); }
                catch (Exception ex) when (ex is IOException or ObjectDisposedException or OperationCanceledException) { status.Text = "The shell is no longer accepting input."; }
            };
            input.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(async (_, args) =>
            {
                if (session is null) return;
                var sequence = args.Key switch
                {
                    Windows.System.VirtualKey.Enter => "\r",
                    Windows.System.VirtualKey.Back => "\u007f",
                    Windows.System.VirtualKey.Tab => "\t",
                    Windows.System.VirtualKey.Escape => "\u001b",
                    Windows.System.VirtualKey.Up => "\u001b[A",
                    Windows.System.VirtualKey.Down => "\u001b[B",
                    Windows.System.VirtualKey.Right => "\u001b[C",
                    Windows.System.VirtualKey.Left => "\u001b[D",
                    _ => null
                };
                if (sequence is null) return;
                args.Handled = true;
                if (args.Key == Windows.System.VirtualKey.Enter) { input.Text = string.Empty; input.Focus(FocusState.Programmatic); }
                try { await session.SendTextAsync(sequence, cancel.Token); }
                catch (Exception ex) when (ex is IOException or ObjectDisposedException or OperationCanceledException) { status.Text = "The shell is no longer accepting input."; }
            }), handledEventsToo: true);
            interrupt.Click += async (_, _) =>
            {
                if (session is null) return;
                try { await session.SendTextAsync("\u0003", cancel.Token); input.Focus(FocusState.Programmatic); }
                catch (Exception ex) when (ex is IOException or ObjectDisposedException or OperationCanceledException) { status.Text = "The shell is no longer accepting input."; }
            };
            RenderTerminal();
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            RuntimeStatus.Text = $"Could not open the project terminal: {ex.Message}";
        }
        finally
        {
            cancel.Cancel();
            if (session is not null)
            {
                session.ScreenChanged -= (_, _) => RenderTerminal();
                await session.DisposeAsync();
            }
            cancel.Dispose();
        }
    }
}
