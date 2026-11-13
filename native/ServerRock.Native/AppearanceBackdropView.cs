// Ambient appearance presets. This is intentionally a self-contained, hit-test-free visual
// layer so themes can grow without coupling decorative motion to chat, pets, or navigation.

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace ServerRock.Native;

public sealed class AppearanceBackdropView : Grid
{
    private readonly Canvas _canvas = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(40) };
    private readonly List<MotionItem> _motion = [];
    private readonly Random _random = new(165);
    private string _preset = "none";
    private bool _reduceMotion;
    private DateTimeOffset _lastTick = DateTimeOffset.UtcNow;

    public AppearanceBackdropView()
    {
        IsHitTestVisible = false;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Opacity = 0.36;
        _canvas.IsHitTestVisible = false;
        Children.Add(_canvas);
        SizeChanged += (_, _) => LayoutMotion();
        _timer.Tick += (_, _) => Animate();
        Visibility = Visibility.Collapsed;
    }

    public void Apply(string? preset, bool reduceMotion)
    {
        _preset = Normalize(preset);
        _reduceMotion = reduceMotion;
        _timer.Stop();
        _motion.Clear();
        _canvas.Children.Clear();

        if (_preset == "none")
        {
            Visibility = Visibility.Collapsed;
            return;
        }

        Visibility = Visibility.Visible;
        if (_preset == "deep-sea") BuildDeepSea();
        else BuildDeepSpace();
        LayoutMotion();
        _lastTick = DateTimeOffset.UtcNow;
        if (!_reduceMotion) _timer.Start();
    }

    private static string Normalize(string? preset) => preset?.Trim().ToLowerInvariant() switch
    {
        "deep-sea" or "sea" => "deep-sea",
        "deep-space" or "space" => "deep-space",
        _ => "none"
    };

    private void BuildDeepSea()
    {
        Background = Gradient(Color.FromArgb(210, 3, 31, 57), Color.FromArgb(205, 7, 111, 125));
        for (var i = 0; i < 24; i++)
        {
            var diameter = 7 + _random.NextDouble() * 24;
            var bubble = new Ellipse
            {
                Width = diameter,
                Height = diameter,
                Fill = new SolidColorBrush(Color.FromArgb(34, 220, 248, 246)),
                Stroke = new SolidColorBrush(Color.FromArgb(100, 166, 242, 236)),
                StrokeThickness = 1
            };
            _canvas.Children.Add(bubble);
            _motion.Add(new MotionItem(bubble, _random.NextDouble(), 0.08 + _random.NextDouble() * 1.08, 0.002 + _random.NextDouble() * 0.008, -(0.015 + _random.NextDouble() * 0.035)));
        }
    }

    private void BuildDeepSpace()
    {
        Background = Gradient(Color.FromArgb(235, 3, 7, 28), Color.FromArgb(220, 36, 22, 76));
        for (var i = 0; i < 38; i++)
        {
            var size = 1 + _random.NextDouble() * 2.6;
            var star = new Ellipse { Width = size, Height = size, Fill = new SolidColorBrush(Color.FromArgb((byte)(120 + _random.Next(120)), 235, 244, 255)) };
            _canvas.Children.Add(star);
            _motion.Add(new MotionItem(star, _random.NextDouble(), _random.NextDouble(), 0.0001 + _random.NextDouble() * 0.0004, 0));
        }

        AddPlanet(0.18, 0.28, 126, Color.FromArgb(255, 38, 103, 170), Color.FromArgb(255, 197, 113, 72));
        AddPlanet(0.76, 0.68, 72, Color.FromArgb(255, 133, 65, 171), Color.FromArgb(255, 54, 186, 182));
        AddUfo(0.56, 0.18, 0.035);
        AddUfo(0.34, 0.78, -0.022);
    }

    private void AddPlanet(double x, double y, double size, Color start, Color end)
    {
        var planet = new Ellipse { Width = size, Height = size, Fill = Gradient(start, end), Opacity = 0.8 };
        _canvas.Children.Add(planet);
        _motion.Add(new MotionItem(planet, x, y, 0.00006, 0.00003));
    }

    private void AddUfo(double x, double y, double velocity)
    {
        var ufo = new Grid { Width = 64, Height = 28, Opacity = 0.82 };
        ufo.Children.Add(new Ellipse { Width = 58, Height = 16, Fill = new SolidColorBrush(Color.FromArgb(210, 126, 201, 213)), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom });
        ufo.Children.Add(new Ellipse { Width = 25, Height = 14, Fill = new SolidColorBrush(Color.FromArgb(185, 203, 231, 244)), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top });
        var lights = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom, Spacing = 8, Margin = new Thickness(0, 0, 0, 1) };
        for (var i = 0; i < 3; i++) lights.Children.Add(new Ellipse { Width = 4, Height = 4, Fill = new SolidColorBrush(Color.FromArgb(255, 255, 223, 119)) });
        ufo.Children.Add(lights);
        _canvas.Children.Add(ufo);
        _motion.Add(new MotionItem(ufo, x, y, velocity, 0));
    }

    private static LinearGradientBrush Gradient(Color start, Color end) => new()
    {
        StartPoint = new Windows.Foundation.Point(0, 0),
        EndPoint = new Windows.Foundation.Point(1, 1),
        GradientStops = { new GradientStop { Color = start, Offset = 0 }, new GradientStop { Color = end, Offset = 1 } }
    };

    private void Animate()
    {
        var now = DateTimeOffset.UtcNow;
        var seconds = Math.Clamp((now - _lastTick).TotalSeconds, 0, 0.2);
        _lastTick = now;
        foreach (var item in _motion)
        {
            item.X += item.Vx * seconds;
            item.Y += item.Vy * seconds;
            if (item.X < -0.15) item.X = 1.05;
            if (item.X > 1.15) item.X = -0.1;
            if (item.Y < -0.15) item.Y = 1.05;
            if (item.Y > 1.15) item.Y = -0.1;
        }
        LayoutMotion();
    }

    private void LayoutMotion()
    {
        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 0 || height <= 0) return;
        foreach (var item in _motion)
        {
            var elementWidth = item.Element is FrameworkElement frameworkElement && !double.IsNaN(frameworkElement.Width) ? frameworkElement.Width : 0;
            var elementHeight = item.Element is FrameworkElement frameworkElement2 && !double.IsNaN(frameworkElement2.Height) ? frameworkElement2.Height : 0;
            Canvas.SetLeft(item.Element, item.X * width - elementWidth / 2);
            Canvas.SetTop(item.Element, item.Y * height - elementHeight / 2);
        }
    }

    private sealed class MotionItem(UIElement element, double x, double y, double vx, double vy)
    {
        public UIElement Element { get; } = element;
        public double X { get; set; } = x;
        public double Y { get; set; } = y;
        public double Vx { get; } = vx;
        public double Vy { get; } = vy;
    }
}
