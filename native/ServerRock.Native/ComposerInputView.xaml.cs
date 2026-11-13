using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace ServerRock.Native;

public sealed partial class ComposerInputView : UserControl
{
    public event SizeChangedEventHandler? ToolbarResized;
    public event RoutedEventHandler? ModelRequested;
    public event RoutedEventHandler? ToolsRequested;
    public event RoutedEventHandler? WebSearchRequested;
    public event RoutedEventHandler? TemporaryRequested;
    public event RoutedEventHandler? ProjectFilesRequested;
    public event RoutedEventHandler? CancelEditRequested;
    public event RoutedEventHandler? SmartRewriteRequested;
    public event RoutedEventHandler? ImageStudioRequested;
    public event RoutedEventHandler? AdvancedRequested;
    public event RoutedEventHandler? AttachPathRequested;
    public event RoutedEventHandler? DocumentsOnlyRequested;
    public event RoutedEventHandler? ManageQueueRequested;
    public event RoutedEventHandler? StatusHistoryRequested;
    public event RoutedEventHandler? DismissStatusRequested;
    public event RoutedEventHandler? AttachRequested;
    public event RoutedEventHandler? CommandsRequested;
    public event RoutedEventHandler? VoiceInputRequested;
    public event RoutedEventHandler? VoiceOutputRequested;
    public event RoutedEventHandler? StopRequested;
    public event RoutedEventHandler? SendRequested;
    public event TextChangedEventHandler? PromptTextChangedRequested;
    public event RoutedEventHandler? ComposerFocused;
    public event RoutedEventHandler? ComposerUnfocused;

    public ComposerInputView()
    {
        InitializeComponent();
        AutomationProperties.SetName(ManageQueueButton, "Manage queued messages");
        AutomationProperties.SetHelpText(ManageQueueButton, "Review, remove, or clear messages waiting to send.");
        ToolbarView.ToolbarResized += (sender, args) => ToolbarResized?.Invoke(sender, args);
        ToolbarView.ModelRequested += (sender, args) => ModelRequested?.Invoke(sender, args);
        ToolbarView.ToolsRequested += (sender, args) => ToolsRequested?.Invoke(sender, args);
        ToolbarView.WebSearchRequested += (sender, args) => WebSearchRequested?.Invoke(sender, args);
        ToolbarView.TemporaryRequested += (sender, args) => TemporaryRequested?.Invoke(sender, args);
        ToolbarView.ProjectFilesRequested += (sender, args) => ProjectFilesRequested?.Invoke(sender, args);
        ToolbarView.CancelEditRequested += (sender, args) => CancelEditRequested?.Invoke(sender, args);
        ToolbarView.SmartRewriteRequested += (sender, args) => SmartRewriteRequested?.Invoke(sender, args);
        ToolbarView.ImageStudioRequested += (sender, args) => ImageStudioRequested?.Invoke(sender, args);
        ToolbarView.AdvancedRequested += (sender, args) => AdvancedRequested?.Invoke(sender, args);
        ToolbarView.AttachPathRequested += (sender, args) => AttachPathRequested?.Invoke(sender, args);
        ToolbarView.DocumentsOnlyRequested += (sender, args) => DocumentsOnlyRequested?.Invoke(sender, args);
        AutomationProperties.SetName(ToolbarView.DocumentsOnlyButtonControl, "Toggle documents-only mode");
    }

    public StackPanel AttachmentListControl => AttachmentList;
    public Border RuntimeStatusBannerControl => RuntimeStatusBanner;
    public TextBlock RuntimeStatusControl => RuntimeStatus;
    public Button ManageQueueButtonControl => ManageQueueButton;
    public Button StatusHistoryButtonControl => StatusHistoryButton;
    public Grid ToolbarControl => ToolbarView.ToolbarControl;
    public Grid PrimaryToolbarControl => ToolbarView.PrimaryToolbarControl;
    public ScrollViewer SecondaryToolbarControl => ToolbarView.SecondaryToolbarControl;
    public Button ModelButtonControl => ToolbarView.ModelButtonControl;
    public Button ComposerToolsButtonControl => ToolbarView.ComposerToolsButtonControl;
    public Button WebSearchButtonControl => ToolbarView.WebSearchButtonControl;
    public Button TemporaryButtonControl => ToolbarView.TemporaryButtonControl;
    public Button ProjectFilesButtonControl => ToolbarView.ProjectFilesButtonControl;
    public Button CancelEditButtonControl => ToolbarView.CancelEditButtonControl;
    public Button SmartRewriteButtonControl => ToolbarView.SmartRewriteButtonControl;
    public Button DocumentsOnlyButtonControl => ToolbarView.DocumentsOnlyButtonControl;
    public Border ComposerBorderControl => ComposerBorder;
    public Border CommandHintPanelControl => CommandHintPanel;
    public StackPanel CommandSuggestionsControl => CommandSuggestions;
    public TextBox PromptBoxControl => PromptBox;
    public Button VoiceButtonControl => VoiceButton;
    public FontIcon VoiceButtonIconControl => VoiceButtonIcon;
    public Button VoiceOutputButtonControl => VoiceOutputButton;
    public FontIcon VoiceOutputIconControl => VoiceOutputIcon;
    public TextBlock ProcessingLabelControl => ProcessingLabel;
    public Button StopButtonControl => StopButton;
    public Button SendButtonControl => SendButton;
    public TextBlock PrivacyFootnoteControl => PrivacyFootnote;

    private void ManageQueueClick(object sender, RoutedEventArgs e) => ManageQueueRequested?.Invoke(sender, e);
    private void StatusHistoryClick(object sender, RoutedEventArgs e) => StatusHistoryRequested?.Invoke(sender, e);
    private void DismissRuntimeStatusClick(object sender, RoutedEventArgs e) => DismissStatusRequested?.Invoke(sender, e);
    private void AttachClick(object sender, RoutedEventArgs e) => AttachRequested?.Invoke(sender, e);
    private void CommandsClick(object sender, RoutedEventArgs e) => CommandsRequested?.Invoke(sender, e);
    private void VoiceInputClick(object sender, RoutedEventArgs e) => VoiceInputRequested?.Invoke(sender, e);
    private void VoiceOutputClick(object sender, RoutedEventArgs e) => VoiceOutputRequested?.Invoke(sender, e);
    private void StopClick(object sender, RoutedEventArgs e) => StopRequested?.Invoke(sender, e);
    private void SendClick(object sender, RoutedEventArgs e) => SendRequested?.Invoke(sender, e);
    private void PromptTextChanged(object sender, TextChangedEventArgs e) => PromptTextChangedRequested?.Invoke(sender, e);
    private void ComposerGotFocus(object sender, RoutedEventArgs e) => ComposerFocused?.Invoke(sender, e);
    private void ComposerLostFocus(object sender, RoutedEventArgs e) => ComposerUnfocused?.Invoke(sender, e);
}
