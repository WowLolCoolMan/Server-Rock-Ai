using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ServerRock.Native;

public sealed partial class CodingWorkspaceBar : UserControl
{
    public event SizeChangedEventHandler? ToolbarResized;
    public event RoutedEventHandler? ProjectFoldersRequested;
    public event RoutedEventHandler? GitHubActionsRequested;
    public event RoutedEventHandler? TerminalRequested;
    public event RoutedEventHandler? UndoHistoryRequested;

    public CodingWorkspaceBar()
    {
        InitializeComponent();
    }

    public Border BarControl => WorkspaceBar;
    public Grid ToolbarControl => CodingWorkspaceToolbar;
    public StackPanel SummaryControl => CodingWorkspaceSummary;
    public Grid ActionsControl => CodingWorkspaceActions;
    public Grid ProjectActionsControl => CodingWorkspaceProjectActions;
    public Grid ToolsControl => CodingWorkspaceTools;
    public TextBlock WorkspaceProjectNameControl => WorkspaceProjectName;
    public Border WorkspaceFolderBadgeControl => WorkspaceFolderBadge;
    public TextBlock WorkspaceFolderCountControl => WorkspaceFolderCount;
    public TextBlock WorkspaceProjectPathControl => WorkspaceProjectPath;
    public TextBlock WorkspaceSessionStateControl => WorkspaceSessionState;
    public Button ProjectFoldersButtonControl => ProjectFoldersButton;
    public Button GitHubActionsButtonControl => GitHubActionsButton;
    public Button TerminalButtonControl => TerminalButton;
    public Button UndoHistoryButtonControl => UndoHistoryButton;

    private void ToolbarSizeChanged(object sender, SizeChangedEventArgs e) => ToolbarResized?.Invoke(sender, e);
    private void ProjectFoldersClick(object sender, RoutedEventArgs e) => ProjectFoldersRequested?.Invoke(sender, e);
    private void GitHubActionsClick(object sender, RoutedEventArgs e) => GitHubActionsRequested?.Invoke(sender, e);
    private void OpenTerminalClick(object sender, RoutedEventArgs e) => TerminalRequested?.Invoke(sender, e);
    private void UndoHistoryClick(object sender, RoutedEventArgs e) => UndoHistoryRequested?.Invoke(sender, e);
}
