// Shared access to the App.xaml design tokens for UI that is built in C#.
// Use these helpers instead of ColorHelper.FromArgb / hard-coded font sizes so code-built
// dialogs, cards, and menus match the XAML shell. See DESIGN.md for the token meanings.
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace ServerRock.Native;

internal static class UiTheme
{
    /// <summary>Returns a token brush from App.xaml (e.g. "InkBrush"); magenta if it is missing so the gap is obvious.</summary>
    public static Brush Brush(string key)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Brush brush) return brush;
        return new SolidColorBrush(Colors.Magenta);
    }

    /// <summary>Returns a token color from App.xaml (e.g. "AccentColor").</summary>
    public static Color Color(string key) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color ? color : Colors.Magenta;

    /// <summary>Returns a token font size from App.xaml (e.g. "SmallFontSize").</summary>
    public static double FontSize(string key) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is double size ? size : 14;

    public static FontFamily CodeFont =>
        Application.Current?.Resources.TryGetValue("CodeFontFamily", out var value) == true && value is FontFamily font ? font : new FontFamily("monospace");

    public static Style? Style(string key) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true ? value as Style : null;

    /// <summary>
    /// Standard dialog setup: the primary action uses the accent button style so it stands
    /// out from Cancel/Close, and optional width/height lift ContentDialog's built-in
    /// 548 px cap (which otherwise clips wide content such as Settings).
    /// </summary>
    public static ContentDialog Standardize(ContentDialog dialog, double? maxWidth = null, double? maxHeight = null)
    {
        if (!string.IsNullOrEmpty(dialog.PrimaryButtonText) && Style("DialogPrimaryButtonStyle") is { } primary)
            dialog.PrimaryButtonStyle = primary;
        if (maxWidth is double width)
        {
            dialog.Resources["ContentDialogMaxWidth"] = width;
            dialog.MaxWidth = width;
        }
        if (maxHeight is double height)
        {
            dialog.Resources["ContentDialogMaxHeight"] = height;
            dialog.MaxHeight = height;
        }
        return dialog;
    }

    /// <summary>Styles one half of a segmented switch as selected or not (see SegmentButtonStyle).</summary>
    public static void ApplySegmentState(Button button, bool selected)
    {
        button.Background = selected ? Brush("SurfaceBrush") : new SolidColorBrush(Colors.Transparent);
        button.BorderBrush = selected ? Brush("LineBrush") : new SolidColorBrush(Colors.Transparent);
        button.Foreground = selected ? Brush("InkBrush") : Brush("MutedInkBrush");
        button.FontWeight = selected ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;
    }

    /// <summary>Styles a vertical navigation item (Settings sections) as selected or not.</summary>
    public static void ApplyNavItemState(Button button, bool selected)
    {
        button.Background = selected ? Brush("AccentSoftBrush") : new SolidColorBrush(Colors.Transparent);
        button.Foreground = selected ? Brush("AccentInkBrush") : Brush("MutedInkBrush");
        button.FontWeight = selected ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;
    }

    /// <summary>
    /// Applies the user's accent choice to every accent token in place, so XAML that binds
    /// the shared brushes (send button, primary actions, selected states) updates together.
    /// </summary>
    public static void ApplyAccent(Color accent, Color hover, Color ink)
    {
        void Set(string key, Color color)
        {
            if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is SolidColorBrush brush) brush.Color = color;
        }
        Set("AccentBrush", accent);
        Set("AccentButtonBackground", accent);
        Set("AccentHoverBrush", hover);
        Set("AccentButtonBackgroundPointerOver", hover);
        Set("AccentPressedBrush", hover);
        Set("AccentButtonBackgroundPressed", hover);
        Set("AccentInkBrush", ink);
        Set("AccentSoftBrush", Microsoft.UI.ColorHelper.FromArgb(255,
            (byte)(255 - (255 - accent.R) * 0.12), (byte)(255 - (255 - accent.G) * 0.12), (byte)(255 - (255 - accent.B) * 0.12)));
    }
}
