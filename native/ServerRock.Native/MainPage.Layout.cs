// Responsive layout for MainPage: switches the sidebar to a drawer on narrow windows and
// rearranges the composer and Coding Agent toolbars into two rows when they get tight.
// Depends on: named elements in MainPage.xaml (AppRoot, Sidebar, SidebarBackdrop, toolbars) and the
// _compact* fields in MainPage.xaml.cs.
// Called by: SizeChanged/Click handlers wired in MainPage.xaml, the constructor, and every sidebar
// action that calls CloseCompactSidebar() before opening a dialog.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    // A 1280px snapped window is a common working width; use a drawer so the
    // conversation area stays comfortable beside the navigation controls.
    private const double CompactNavigationBreakpoint = 1320;
    private const double CompactComposerToolbarBreakpoint = 620;
    private const double CompactCodingWorkspaceToolbarBreakpoint = 820;

    private void AppRootSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ApplyResponsiveNavigation(e.NewSize.Width);
        ApplyComposerHeight();
        PositionCompanion(persistIfChanged: true);
    }

    // Below this window height the prompt box starts shorter so the conversation keeps room.
    private const double ShortWindowHeight = 720;

    // Prompt box height follows Interface density, but on short windows it starts at about two
    // lines (it grows as you type), because the 132 px default plus the toolbars used to fill
    // most of a 560 px window.
    private void ApplyComposerHeight()
    {
        var compactDensity = _settings?.InterfaceDensity.Equals("compact", StringComparison.OrdinalIgnoreCase) == true;
        var shortWindow = AppRoot.ActualHeight is > 0 and < ShortWindowHeight;
        PromptBox.MinHeight = shortWindow ? 56 : compactDensity ? 100 : 132;
        PromptBox.MaxHeight = shortWindow ? 160 : compactDensity ? 240 : 320;
    }

    private void ComposerToolbarSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var compact = e.NewSize.Width > 0 && e.NewSize.Width < CompactComposerToolbarBreakpoint;
        if (_compactComposerToolbar == compact) return;

        _compactComposerToolbar = compact;
        if (compact)
        {
            // Keep model selection and Tools together; place optional switches on
            // a second row so narrow windows never clip or hide them.
            Grid.SetRow(PrimaryComposerToolbar, 0);
            Grid.SetColumn(PrimaryComposerToolbar, 0);
            Grid.SetColumnSpan(PrimaryComposerToolbar, 2);
            Grid.SetRow(SecondaryComposerToolbar, 1);
            Grid.SetColumn(SecondaryComposerToolbar, 0);
            Grid.SetColumnSpan(SecondaryComposerToolbar, 2);
            ModelButton.MaxWidth = 190;
        }
        else
        {
            Grid.SetRow(PrimaryComposerToolbar, 0);
            Grid.SetColumn(PrimaryComposerToolbar, 0);
            Grid.SetColumnSpan(PrimaryComposerToolbar, 1);
            Grid.SetRow(SecondaryComposerToolbar, 0);
            Grid.SetColumn(SecondaryComposerToolbar, 1);
            Grid.SetColumnSpan(SecondaryComposerToolbar, 1);
            ModelButton.MaxWidth = 330;
        }

        if (_settings is not null)
        {
            UpdateModelLabel();
            UpdateWebSearchButton();
            UpdateSmartRewriteButton();
            UpdateTemporaryControls();
            UpdateProjectFilesControls();
        }
    }

    private void CodingWorkspaceToolbarSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var compact = e.NewSize.Width > 0 && e.NewSize.Width < CompactCodingWorkspaceToolbarBreakpoint;
        if (_compactCodingWorkspaceToolbar == compact) return;

        _compactCodingWorkspaceToolbar = compact;
        if (compact)
        {
            // Move project identity above the action groups, which then occupy
            // two short rows. This keeps every project action reachable on phones.
            Grid.SetColumnSpan(CodingWorkspaceSummary, 2);
            Grid.SetColumn(CodingWorkspaceSummary, 0);
            Grid.SetRow(CodingWorkspaceSummary, 0);
            Grid.SetColumnSpan(CodingWorkspaceActions, 2);
            Grid.SetColumn(CodingWorkspaceActions, 0);
            Grid.SetRow(CodingWorkspaceActions, 1);
            Grid.SetColumnSpan(CodingWorkspaceProjectActions, 2);
            Grid.SetColumn(CodingWorkspaceProjectActions, 0);
            Grid.SetRow(CodingWorkspaceProjectActions, 0);
            Grid.SetColumnSpan(CodingWorkspaceTools, 2);
            Grid.SetColumn(CodingWorkspaceTools, 0);
            Grid.SetRow(CodingWorkspaceTools, 1);
        }
        else
        {
            Grid.SetColumnSpan(CodingWorkspaceSummary, 1);
            Grid.SetColumn(CodingWorkspaceSummary, 0);
            Grid.SetRow(CodingWorkspaceSummary, 0);
            Grid.SetColumnSpan(CodingWorkspaceActions, 1);
            Grid.SetColumn(CodingWorkspaceActions, 1);
            Grid.SetRow(CodingWorkspaceActions, 0);
            Grid.SetColumnSpan(CodingWorkspaceProjectActions, 1);
            Grid.SetColumn(CodingWorkspaceProjectActions, 0);
            Grid.SetRow(CodingWorkspaceProjectActions, 0);
            Grid.SetColumnSpan(CodingWorkspaceTools, 1);
            Grid.SetColumn(CodingWorkspaceTools, 1);
            Grid.SetRow(CodingWorkspaceTools, 0);
        }
    }

    private void ApplyResponsiveNavigation(double width)
    {
        if (width <= 0) return;
        var compact = width < CompactNavigationBreakpoint;
        if (_compactNavigation == compact) return;

        _compactNavigation = compact;
        AppRoot.ColumnDefinitions[0].Width = new GridLength(compact ? 0 : 276);
        Grid.SetColumnSpan(Sidebar, compact ? 2 : 1);
        Sidebar.Width = compact ? 276 : double.NaN;
        Sidebar.HorizontalAlignment = compact ? HorizontalAlignment.Left : HorizontalAlignment.Stretch;
        Sidebar.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        SidebarBackdrop.Visibility = Visibility.Collapsed;
        ToggleSidebarButton.Visibility = compact ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ToggleSidebarClick(object sender, RoutedEventArgs e)
    {
        if (!_compactNavigation) return;
        Sidebar.Visibility = Visibility.Visible;
        SidebarBackdrop.Visibility = Visibility.Visible;
        ChatModeButton.Focus(FocusState.Programmatic);
    }

    private void CloseSidebarClick(object sender, RoutedEventArgs e) => CloseCompactSidebar();

    private void CloseCompactSidebar()
    {
        if (!_compactNavigation) return;
        Sidebar.Visibility = Visibility.Collapsed;
        SidebarBackdrop.Visibility = Visibility.Collapsed;
        ToggleSidebarButton.Focus(FocusState.Programmatic);
    }
}
