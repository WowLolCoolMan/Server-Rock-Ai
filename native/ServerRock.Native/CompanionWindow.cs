using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace ServerRock.Native;

/// <summary>A lightweight desktop companion surface. The root is transparent so only the sprite and speech bubble are visible.</summary>
internal sealed class CompanionWindow
{
    private readonly Window _window;
    private readonly TextBlock _status;
    private readonly StackPanel _choices;

    public CompanionWindow(string name, string assetUri, string status)
    {
        _status = new TextBlock { Text = status, FontSize = 12, Foreground = Brush(40, 71, 78), TextWrapping = TextWrapping.Wrap, MaxWidth = 250, HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center };
        var image = new Image { Width = 112, Height = 112, Stretch = Stretch.Uniform, Source = assetUri.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ? new SvgImageSource { UriSource = new Uri(assetUri) } : new BitmapImage { UriSource = new Uri(assetUri) } };
        var hide = new Button { Content = "×", Width = 26, Height = 26, Padding = new Thickness(0), Background = new SolidColorBrush(Colors.Transparent), BorderThickness = new Thickness(0), HorizontalAlignment = HorizontalAlignment.Right };
        hide.Click += (_, _) => Close();
        _choices = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, HorizontalAlignment = HorizontalAlignment.Center, Visibility = Visibility.Collapsed };
        var content = new StackPanel { Spacing = 4, HorizontalAlignment = HorizontalAlignment.Center, Background = new SolidColorBrush(Colors.Transparent), Padding = new Thickness(10) };
        content.Children.Add(hide);
        content.Children.Add(_status);
        content.Children.Add(image);
        content.Children.Add(new TextBlock { Text = name, FontSize = 11, Foreground = Brush(91, 111, 117), HorizontalAlignment = HorizontalAlignment.Center });
        content.Children.Add(_choices);
        _window = new Window { Content = content };
        _window.Closed += (_, _) => Closed?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? Closed;
    public event EventHandler<string>? ChoiceSelected;

    public void Show() => _window.Activate();
    public void Close() { try { _window.Close(); } catch { } }
    public void UpdateStatus(string status) => _status.Text = status;

    public void Ask(string question, IReadOnlyList<string> options)
    {
        _status.Text = question;
        _choices.Children.Clear();
        foreach (var option in options.Take(4))
        {
            var button = new Button { Content = option, Padding = new Thickness(9, 4, 9, 4), CornerRadius = new CornerRadius(7), Background = Brush(235, 247, 247), Foreground = Brush(41, 72, 80) };
            button.Click += (_, _) => ChoiceSelected?.Invoke(this, option);
            _choices.Children.Add(button);
        }
        _choices.Visibility = _choices.Children.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        Show();
    }

    private static SolidColorBrush Brush(byte r, byte g, byte b) => new(ColorHelper.FromArgb(255, r, g, b));
}
