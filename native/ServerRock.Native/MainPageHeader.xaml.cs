using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;

namespace ServerRock.Native;

public sealed partial class MainPageHeader : UserControl
{
    public event RoutedEventHandler? ToggleSidebarRequested;
    public event RoutedEventHandler? BackRequested;
    public event RoutedEventHandler? ForwardRequested;
    public event RoutedEventHandler? SettingsRequested;
    public event RoutedEventHandler? NewChatRequested;
    public event RoutedEventHandler? PreviousBranchRequested;
    public event RoutedEventHandler? NextBranchRequested;

    public MainPageHeader()
    {
        InitializeComponent();
        AutomationProperties.SetName(BackButton, "Navigate back");
        AutomationProperties.SetHelpText(BackButton, "Return to the previous workspace route.");
        AutomationProperties.SetName(ForwardButton, "Navigate forward");
        AutomationProperties.SetHelpText(ForwardButton, "Return to the next workspace route.");
    }

    public Grid HeaderControl => Header;
    public Button ToggleSidebarButtonControl => ToggleSidebarButton;
    public Button BackButtonControl => BackButton;
    public Button ForwardButtonControl => ForwardButton;
    public TextBlock PageTitleControl => PageTitle;
    public Border ProjectBadgeControl => ProjectBadge;
    public TextBlock ProjectBadgeTextControl => ProjectBadgeText;
    public Border ChatFolderBadgeControl => ChatFolderBadge;
    public TextBlock ChatFolderBadgeTextControl => ChatFolderBadgeText;
    public Border TemporaryBadgeControl => TemporaryBadge;
    public StackPanel BranchControlsControl => BranchControls;
    public Button PreviousBranchButtonControl => PreviousBranchButton;
    public TextBlock BranchPositionTextControl => BranchPositionText;
    public Button NextBranchButtonControl => NextBranchButton;
    public Ellipse HeaderConnectionDotControl => HeaderConnectionDot;
    public TextBlock HeaderConnectionLabelControl => HeaderConnectionLabel;

    private void ToggleSidebarClick(object sender, RoutedEventArgs e) => ToggleSidebarRequested?.Invoke(sender, e);
    private void BackClick(object sender, RoutedEventArgs e) => BackRequested?.Invoke(sender, e);
    private void ForwardClick(object sender, RoutedEventArgs e) => ForwardRequested?.Invoke(sender, e);
    private void SettingsClick(object sender, RoutedEventArgs e) => SettingsRequested?.Invoke(sender, e);
    private void NewChatClick(object sender, RoutedEventArgs e) => NewChatRequested?.Invoke(sender, e);
    private void PreviousBranchClick(object sender, RoutedEventArgs e) => PreviousBranchRequested?.Invoke(sender, e);
    private void NextBranchClick(object sender, RoutedEventArgs e) => NextBranchRequested?.Invoke(sender, e);
}
