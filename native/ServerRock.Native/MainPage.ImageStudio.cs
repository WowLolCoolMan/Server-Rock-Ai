// Image Studio: runs the user's ComfyUI API workflow against a loopback-only ComfyUI server,
// with cancellation, and shows the resulting images as preview cards.
// Depends on: ComfyImageProvider (ServerRock.Core), SaveSettings (endpoint and workflow are
// saved as non-secret settings).
// Called by: the Image Studio button in MainPage.xaml.

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using ServerRock.Core.Infrastructure;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private async void ImageStudioClick(object sender, RoutedEventArgs e) => await RunUiActionAsync(ImageStudioAsync);

    private async Task ImageStudioAsync()
    {
        var prompt = new TextBox { Header = "Image prompt", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 88, PlaceholderText = "Describe the image you want to create" };
        var endpoint = new TextBox { Header = "Local ComfyUI endpoint", Text = _settings.ComfyEndpoint, PlaceholderText = "http://127.0.0.1:8188" };
        var workflow = new TextBox { Header = "ComfyUI API workflow JSON", Text = _settings.ImageWorkflow, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 120, PlaceholderText = "Put {{prompt}} inside the workflow's prompt text value." };
        var status = new TextBlock { Text = "Images run only through a local loopback ComfyUI server. Your workflow is saved only on this device.", TextWrapping = TextWrapping.Wrap, FontSize = 12, Opacity = .8 };
        var results = new StackPanel { Spacing = 8 };
        CancellationTokenSource? generationCancellation = null;
        var cancel = new Button { Content = "Cancel generation", IsEnabled = false, HorizontalAlignment = HorizontalAlignment.Left };
        AutomationProperties.SetName(cancel, "Cancel local image generation");
        cancel.Click += (_, _) => generationCancellation?.Cancel();
        var panel = new StackPanel { Spacing = 10, Children = { prompt, endpoint, workflow, status, cancel, results } };
        var dialog = new ContentDialog { Title = "Image Studio", Content = new ScrollViewer { Content = panel, MaxHeight = 680 }, PrimaryButtonText = "Generate", CloseButtonText = "Done", XamlRoot = XamlRoot };
        while (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            try
            {
                dialog.IsPrimaryButtonEnabled = false;
                prompt.IsEnabled = endpoint.IsEnabled = workflow.IsEnabled = false;
                generationCancellation = new CancellationTokenSource();
                cancel.IsEnabled = true;
                status.Text = "Preparing local image generation…";
                _settings.ComfyEndpoint = endpoint.Text.Trim().TrimEnd('/');
                _settings.ImageWorkflow = workflow.Text;
                SaveSettings();
                results.Children.Clear();
                var images = await _comfyImageProvider.GenerateAsync(_settings.ComfyEndpoint, _settings.ImageWorkflow, prompt.Text, message => status.Text = message, generationCancellation.Token);
                status.Text = $"Generated {images.Count} image(s) locally. Previews are loaded only from your local ComfyUI server.";
                foreach (var image in images)
                {
                    var preview = new Image
                    {
                        Source = new BitmapImage { UriSource = image.Url },
                        MaxWidth = 520,
                        MaxHeight = 360,
                        Stretch = Stretch.Uniform,
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    AutomationProperties.SetName(preview, $"Generated image preview: {image.FileName}");
                    var open = new Button { Content = image.FileName, HorizontalAlignment = HorizontalAlignment.Left, Tag = image.Url };
                    AutomationProperties.SetName(open, $"Open generated image {image.FileName}");
                    open.Click += async (_, _) =>
                    {
                        if (open.Tag is Uri uri) await Windows.System.Launcher.LaunchUriAsync(uri);
                    };
                    results.Children.Add(new Border
                    {
                        Background = new SolidColorBrush(Colors.White),
                        BorderBrush = new SolidColorBrush(Colors.LightGray),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(10),
                        Padding = new Thickness(10),
                        Child = new StackPanel
                        {
                            Spacing = 8,
                            Children =
                            {
                                preview,
                                new TextBlock { Text = image.FileName, FontSize = 12, Opacity = .8, TextTrimming = TextTrimming.CharacterEllipsis },
                                open
                            }
                        }
                    });
                }
            }
            catch (OperationCanceledException) when (generationCancellation?.IsCancellationRequested == true)
            {
                status.Text = "Image generation cancelled. Your local workflow remains available to try again.";
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                status.Text = $"Image generation failed: {ProviderErrorText.Describe(exception)}";
            }
            finally
            {
                dialog.IsPrimaryButtonEnabled = true;
                prompt.IsEnabled = endpoint.IsEnabled = workflow.IsEnabled = true;
                cancel.IsEnabled = false;
                generationCancellation?.Dispose();
                generationCancellation = null;
            }
        }
    }
}
