using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ServerRock.Native;

public sealed partial class ConversationSurface : UserControl
{
    public event RoutedEventHandler? SettingsRequested;
    public event RoutedEventHandler? SuggestionRequested;

    public ConversationSurface()
    {
        InitializeComponent();
    }

    public StackPanel WelcomePanelControl => WelcomePanel;
    public TextBlock WelcomeTitleControl => WelcomeTitle;
    public TextBlock WelcomeSubtitleControl => WelcomeSubtitle;
    public Button ConnectionSetupButtonControl => ConnectionSetupButton;
    public Button SuggestionOneControl => SuggestionOne;
    public Button SuggestionTwoControl => SuggestionTwo;
    public Button SuggestionThreeControl => SuggestionThree;
    public ScrollViewer ConversationScrollControl => ConversationScroll;
    public StackPanel ConversationMessagesControl => ConversationMessages;

    private void SettingsClick(object sender, RoutedEventArgs e) => SettingsRequested?.Invoke(sender, e);
    private void SuggestionClick(object sender, RoutedEventArgs e) => SuggestionRequested?.Invoke(sender, e);
}
