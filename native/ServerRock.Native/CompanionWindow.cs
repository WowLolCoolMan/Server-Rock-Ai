using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI;

namespace ServerRock.Native;

/// <summary>A small independent desktop window for the local pet.</summary>
internal sealed class CompanionWindow
{
    private readonly Window _window;
    private readonly TextBlock _status;

    public CompanionWindow(string name, string assetUri, string status)
    {
        _status = new TextBlock { Text = status, FontSize = 11, Foreground = Brush(102, 120, 125), TextWrapping = TextWrapping.Wrap, MaxWidth = 190 };
        var hide = new Button { Content = "×", Width = 28, Height = 28, Padding = new Thickness(0), Background = new SolidColorBrush(Colors.Transparent), BorderThickness = new Thickness(0) };
        hide.Click += (_, _) => Close();
        var content = new Grid { Padding = new Thickness(12), Background = Brush(255, 254, 254), ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Auto }, new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, new ColumnDefinition { Width = GridLength.Auto } } };
        var image = new Image { Width = 54, Height = 54, Stretch = Stretch.Uniform, Source = new SvgImageSource { UriSource = new Uri(assetUri) } };
        content.Children.Add(image);
        var details = new StackPanel { Margin = new Thickness(10, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center, Spacing = 3 };
        details.Children.Add(new TextBlock { Text = name, FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = Brush(40, 59, 65) });
        details.Children.Add(_status);
        Grid.SetColumn(details, 1);
        content.Children.Add(details);
        Grid.SetColumn(hide, 2);
        content.Children.Add(hide);
        _window = new Window { Content = content };
        _window.Closed += (_, _) => Closed?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? Closed;

    public void Show() { _window.Activate(); }
    public void Close() { try { _window.Close(); } catch (Exception) { } }
    public void UpdateStatus(string status) => _status.Text = status;

    private static SolidColorBrush Brush(byte r, byte g, byte b) => new(ColorHelper.FromArgb(255, r, g, b));
}
