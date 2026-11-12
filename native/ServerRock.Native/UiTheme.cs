// Shared access to the App.xaml design tokens for UI that is built in C#.
// Use these helpers instead of ColorHelper.FromArgb / hard-coded font sizes so code-built
// dialogs, cards, and menus match the XAML shell. See DESIGN.md for the token meanings.
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using ColorHelper = Microsoft.UI.ColorHelper;

namespace ServerRock.Native;

internal static class UiTheme
{
    public static ElementTheme CurrentElementTheme { get; private set; } = ElementTheme.Light;

    /// <summary>Applies the persisted light/dark palette to the shared native token brushes.</summary>
    public static void ApplyTheme(string? mode)
    {
        var dark = string.Equals(mode, "dark", StringComparison.OrdinalIgnoreCase);
        CurrentElementTheme = dark ? ElementTheme.Dark : ElementTheme.Light;

        if (Application.Current is App app && app.MainWindow?.Content is FrameworkElement root)
            root.RequestedTheme = CurrentElementTheme;

        if (dark)
        {
            SetColor("CanvasColor", "CanvasBrush", ColorHelper.FromArgb(255, 17, 25, 27));
            SetColor("SurfaceColor", "SurfaceBrush", ColorHelper.FromArgb(255, 24, 35, 38));
            SetColor("SurfaceAltColor", "SurfaceAltBrush", ColorHelper.FromArgb(255, 32, 48, 52));
            SetColor("SidebarColor", "SidebarBrush", ColorHelper.FromArgb(255, 20, 33, 36));
            SetColor("LineColor", "LineBrush", ColorHelper.FromArgb(255, 58, 77, 81));
            SetColor("LineSoftColor", "LineSoftBrush", ColorHelper.FromArgb(255, 44, 60, 64));
            SetColor("InkColor", "InkBrush", ColorHelper.FromArgb(255, 229, 240, 240));
            SetColor("MutedInkColor", "MutedInkBrush", ColorHelper.FromArgb(255, 168, 184, 186));
            SetColor("SubtleInkColor", "SubtleInkBrush", ColorHelper.FromArgb(255, 126, 146, 149));
            SetColor("AccentColor", "AccentBrush", ColorHelper.FromArgb(255, 20, 141, 157));
            SetColor("AccentHoverColor", "AccentHoverBrush", ColorHelper.FromArgb(255, 24, 163, 178));
            SetColor("AccentPressedColor", "AccentPressedBrush", ColorHelper.FromArgb(255, 17, 119, 134));
            SetColor("AccentInkColor", "AccentInkBrush", ColorHelper.FromArgb(255, 123, 225, 231));
            SetColor("AccentSoftColor", "AccentSoftBrush", ColorHelper.FromArgb(255, 27, 74, 82));
            SetColor("ScrimColor", "ScrimBrush", ColorHelper.FromArgb(210, 0, 0, 0));
            SetColor("BrandMarkColor", "BrandMarkBrush", ColorHelper.FromArgb(255, 12, 22, 24));
            SetColor("BrandMarkInkColor", "BrandMarkInkBrush", ColorHelper.FromArgb(255, 223, 247, 250));
            SetColor("SuccessColor", "SuccessBrush", ColorHelper.FromArgb(255, 91, 194, 137));
            SetColor("WarningColor", "WarningBrush", ColorHelper.FromArgb(255, 232, 176, 91));
            SetColor("WarningSoftColor", "WarningSoftBrush", ColorHelper.FromArgb(255, 73, 57, 31));
            SetColor("WarningLineColor", "WarningLineBrush", ColorHelper.FromArgb(255, 117, 88, 43));
            SetColor("WarningInkColor", "WarningInkBrush", ColorHelper.FromArgb(255, 245, 205, 130));
            SetColor("DangerColor", "DangerBrush", ColorHelper.FromArgb(255, 219, 113, 103));
        }
        else
        {
            SetColor("CanvasColor", "CanvasBrush", ColorHelper.FromArgb(255, 247, 250, 249));
            SetColor("SurfaceColor", "SurfaceBrush", ColorHelper.FromArgb(255, 255, 255, 255));
            SetColor("SurfaceAltColor", "SurfaceAltBrush", ColorHelper.FromArgb(255, 244, 248, 247));
            SetColor("SidebarColor", "SidebarBrush", ColorHelper.FromArgb(255, 238, 245, 243));
            SetColor("LineColor", "LineBrush", ColorHelper.FromArgb(255, 216, 229, 226));
            SetColor("LineSoftColor", "LineSoftBrush", ColorHelper.FromArgb(255, 229, 236, 235));
            SetColor("InkColor", "InkBrush", ColorHelper.FromArgb(255, 24, 51, 59));
            SetColor("MutedInkColor", "MutedInkBrush", ColorHelper.FromArgb(255, 88, 109, 114));
            SetColor("SubtleInkColor", "SubtleInkBrush", ColorHelper.FromArgb(255, 139, 153, 157));
            SetColor("AccentColor", "AccentBrush", ColorHelper.FromArgb(255, 10, 128, 148));
            SetColor("AccentHoverColor", "AccentHoverBrush", ColorHelper.FromArgb(255, 8, 112, 127));
            SetColor("AccentPressedColor", "AccentPressedBrush", ColorHelper.FromArgb(255, 7, 104, 122));
            SetColor("AccentInkColor", "AccentInkBrush", ColorHelper.FromArgb(255, 11, 111, 128));
            SetColor("AccentSoftColor", "AccentSoftBrush", ColorHelper.FromArgb(255, 229, 245, 246));
            SetColor("ScrimColor", "ScrimBrush", ColorHelper.FromArgb(102, 0, 0, 0));
            SetColor("BrandMarkColor", "BrandMarkBrush", ColorHelper.FromArgb(255, 24, 56, 68));
            SetColor("BrandMarkInkColor", "BrandMarkInkBrush", ColorHelper.FromArgb(255, 223, 247, 250));
            SetColor("SuccessColor", "SuccessBrush", ColorHelper.FromArgb(255, 62, 155, 106));
            SetColor("WarningColor", "WarningBrush", ColorHelper.FromArgb(255, 214, 154, 75));
            SetColor("WarningSoftColor", "WarningSoftBrush", ColorHelper.FromArgb(255, 255, 247, 230));
            SetColor("WarningLineColor", "WarningLineBrush", ColorHelper.FromArgb(255, 239, 222, 184));
            SetColor("WarningInkColor", "WarningInkBrush", ColorHelper.FromArgb(255, 113, 82, 42));
            SetColor("DangerColor", "DangerBrush", ColorHelper.FromArgb(255, 166, 77, 67));
        }
    }

    private static void SetColor(string colorKey, string brushKey, Color color)
    {
        if (Application.Current?.Resources is not { } resources) return;
        resources[colorKey] = color;
        if (resources.TryGetValue(brushKey, out var value) && value is SolidColorBrush brush) brush.Color = color;
    }

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
        dialog.RequestedTheme = CurrentElementTheme;
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

    /// <summary>Makes DisplayMemberPath rows in a list wrap instead of being cut off.</summary>
    public static T WrapListText<T>(T list) where T : ListViewBase
    {
        // Uno does not apply an implicit TextBlock style to DisplayMemberPath rows, so wrap
        // each row's text as its container is realized.
        list.ContainerContentChanging += (_, args) =>
        {
            void Wrap(DependencyObject node)
            {
                if (node is TextBlock text) { text.TextWrapping = TextWrapping.Wrap; text.Margin = new Thickness(0, 4, 0, 4); }
                for (var index = 0; index < VisualTreeHelper.GetChildrenCount(node); index++) Wrap(VisualTreeHelper.GetChild(node, index));
            }
            var container = args.ItemContainer;
            if (container is null) return;
            Wrap(container);
            container.DispatcherQueue?.TryEnqueue(() => Wrap(container));
        };
        return list;
    }

    /// <summary>Styles one half of a segmented switch as selected or not (see SegmentButtonStyle).</summary>
    public static void ApplySegmentState(Button button, bool selected)
    {
        button.Background = selected ? Brush("SurfaceBrush") : new SolidColorBrush(Colors.Transparent);
        button.BorderBrush = selected ? Brush("LineBrush") : new SolidColorBrush(Colors.Transparent);
        button.Foreground = selected ? Brush("InkBrush") : Brush("MutedInkBrush");
        button.FontWeight = selected ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;
    }

    /// <summary>
    /// Styles an on/off toggle that uses ContextButtonStyle: "on" gets the accent-soft fill and
    /// accent text/border so the state is visible at a glance, not only in its label.
    /// </summary>
    public static void ApplyToggleState(Button button, bool on)
    {
        button.Background = on ? Brush("AccentSoftBrush") : Brush("SurfaceBrush");
        button.BorderBrush = on ? Brush("AccentBrush") : Brush("LineBrush");
        button.Foreground = on ? Brush("AccentInkBrush") : Brush("MutedInkBrush");
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
