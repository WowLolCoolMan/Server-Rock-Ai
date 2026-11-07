using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Ellipse = Microsoft.UI.Xaml.Shapes.Ellipse;

namespace ServerRock.Native;

public sealed partial class SidebarView : UserControl
{
    public Button ChatModeButtonControl => ChatModeButton;
    public Button CodingModeButtonControl => CodingModeButton;
    public Button NewConversationButtonControl => NewConversationButton;
    public Button NewProjectButtonControl => NewProjectButton;
    public Button GitHubButtonControl => GitHubButton;
    public StackPanel CodingNavigationSectionControl => CodingNavigationSection;
    public StackPanel ProjectListControl => ProjectList;
    public TextBlock CodeFilesHeaderControl => CodeFilesHeader;
    public StackPanel ProjectFileListControl => ProjectFileList;
    public StackPanel ChatFolderListControl => ChatFolderList;
    public StackPanel RecentListControl => RecentList;
    public Ellipse ConnectionDotControl => ConnectionDot;
    public TextBlock ConnectionLabelControl => ConnectionLabel;
    public Button CompanionButtonControl => CompanionButton;
    public Button ImportChatsButtonControl => ImportChatsButton;
    public Button ExportChatsButtonControl => ExportChatsButton;

    public event RoutedEventHandler? ChatModeRequested;
    public event RoutedEventHandler? CodingModeRequested;
    public event RoutedEventHandler? NewChatRequested;
    public event RoutedEventHandler? NewProjectRequested;
    public event RoutedEventHandler? SearchRequested;
    public event RoutedEventHandler? ModelsRequested;
    public event RoutedEventHandler? SettingsRequested;
    public event RoutedEventHandler? PluginsRequested;
    public event RoutedEventHandler? GitHubRequested;
    public event RoutedEventHandler? NewChatFolderRequested;
    public event RoutedEventHandler? ImportChatsRequested;
    public event RoutedEventHandler? ExportChatsRequested;
    public event RoutedEventHandler? ToggleCompanionRequested;
    public event RoutedEventHandler? CustomizeCompanionRequested;

    public SidebarView() => InitializeComponent();

    private void ChatModeClick(object sender, RoutedEventArgs e) => ChatModeRequested?.Invoke(sender, e);
    private void CodingModeClick(object sender, RoutedEventArgs e) => CodingModeRequested?.Invoke(sender, e);
    private void NewChatClick(object sender, RoutedEventArgs e) => NewChatRequested?.Invoke(sender, e);
    private void NewProjectClick(object sender, RoutedEventArgs e) => NewProjectRequested?.Invoke(sender, e);
    private void SearchClick(object sender, RoutedEventArgs e) => SearchRequested?.Invoke(sender, e);
    private void ModelClick(object sender, RoutedEventArgs e) => ModelsRequested?.Invoke(sender, e);
    private void SettingsClick(object sender, RoutedEventArgs e) => SettingsRequested?.Invoke(sender, e);
    private void PluginStoreClick(object sender, RoutedEventArgs e) => PluginsRequested?.Invoke(sender, e);
    private void GitHubClick(object sender, RoutedEventArgs e) => GitHubRequested?.Invoke(sender, e);
    private void NewChatFolderClick(object sender, RoutedEventArgs e) => NewChatFolderRequested?.Invoke(sender, e);
    private void ImportChatsClick(object sender, RoutedEventArgs e) => ImportChatsRequested?.Invoke(sender, e);
    private void ExportChatsClick(object sender, RoutedEventArgs e) => ExportChatsRequested?.Invoke(sender, e);
    private void ToggleCompanionClick(object sender, RoutedEventArgs e) => ToggleCompanionRequested?.Invoke(sender, e);
    private void CustomizeCompanionClick(object sender, RoutedEventArgs e) => CustomizeCompanionRequested?.Invoke(sender, e);
}
