using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ServerRock.Native;

public sealed partial class ComposerToolbarView : UserControl
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

    public ComposerToolbarView()
    {
        InitializeComponent();
    }

    public Grid ToolbarControl => Toolbar;
    public Grid PrimaryToolbarControl => PrimaryComposerToolbar;
    public ScrollViewer SecondaryToolbarControl => SecondaryComposerToolbar;
    public Button ModelButtonControl => ModelButton;
    public Button ComposerToolsButtonControl => ComposerToolsButton;
    public Button WebSearchButtonControl => WebSearchButton;
    public Button TemporaryButtonControl => TemporaryButton;
    public Button ProjectFilesButtonControl => ProjectFilesButton;
    public Button CancelEditButtonControl => CancelEditButton;
    public Button SmartRewriteButtonControl => SmartRewriteButton;
    public Button DocumentsOnlyButtonControl => DocumentsOnlyButton;

    private void ToolbarSizeChanged(object sender, SizeChangedEventArgs e) => ToolbarResized?.Invoke(sender, e);
    private void ModelHotswapClick(object sender, RoutedEventArgs e) => ModelRequested?.Invoke(sender, e);
    private void ComposerToolsClick(object sender, RoutedEventArgs e) => ToolsRequested?.Invoke(sender, e);
    private void WebSearchClick(object sender, RoutedEventArgs e) => WebSearchRequested?.Invoke(sender, e);
    private void TemporaryClick(object sender, RoutedEventArgs e) => TemporaryRequested?.Invoke(sender, e);
    private void ProjectFilesClick(object sender, RoutedEventArgs e) => ProjectFilesRequested?.Invoke(sender, e);
    private void CancelEditClick(object sender, RoutedEventArgs e) => CancelEditRequested?.Invoke(sender, e);
    private void SmartRewriteClick(object sender, RoutedEventArgs e) => SmartRewriteRequested?.Invoke(sender, e);
    private void ImageStudioClick(object sender, RoutedEventArgs e) => ImageStudioRequested?.Invoke(sender, e);
    private void AdvancedClick(object sender, RoutedEventArgs e) => AdvancedRequested?.Invoke(sender, e);
    private void AttachLocalPathClick(object sender, RoutedEventArgs e) => AttachPathRequested?.Invoke(sender, e);
    private void DocumentsOnlyClick(object sender, RoutedEventArgs e) => DocumentsOnlyRequested?.Invoke(sender, e);
}
