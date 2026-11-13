using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ServerRock.Native;

public sealed partial class ConversationNavigationView : UserControl
{
    public StackPanel ChatFolderListControl => ChatFolderList;
    public StackPanel RecentListControl => RecentList;
    public Button ImportChatsButtonControl => ImportChatsButton;
    public Button ExportChatsButtonControl => ExportChatsButton;

    public event RoutedEventHandler? NewChatFolderRequested;
    public event RoutedEventHandler? ImportChatsRequested;
    public event RoutedEventHandler? ExportChatsRequested;

    public ConversationNavigationView()
    {
        InitializeComponent();
    }

    private void NewChatFolderClick(object sender, RoutedEventArgs e) => NewChatFolderRequested?.Invoke(sender, e);
    private void ImportChatsClick(object sender, RoutedEventArgs e) => ImportChatsRequested?.Invoke(sender, e);
    private void ExportChatsClick(object sender, RoutedEventArgs e) => ExportChatsRequested?.Invoke(sender, e);
}
