// Composer attachments: picking up to five text/code files, PDFs (text extracted on-device), or
// PNG/JPEG images, enforcing size and type limits, and showing removable chips.
// ChatAttachmentPolicy.ConsentKey (Core) identifies a set of attachments for one endpoint so the remote-upload
// consent prompt is asked once per set.
// Depends on: ChatAttachmentPolicy, PdfAttachmentTextExtractor (ServerRock.Core), the file picker,
// and _pendingAttachments in MainPage.xaml.cs.
// Called by: the attach (+) button in MainPage.xaml, SendClick (consent + clearing), and mode
// switching (ClearPendingAttachments).

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using ServerRock.Core.Models;
using ServerRock.Core.Infrastructure;
using Windows.Storage.Streams;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private async void AttachClick(object sender, RoutedEventArgs e)
    {
        if (_requestCancellation is not null) return;
        try
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
                ViewMode = Windows.Storage.Pickers.PickerViewMode.List
            };
            foreach (var extension in new[] { ".txt", ".md", ".markdown", ".csv", ".json", ".xml", ".yaml", ".yml", ".toml", ".html", ".htm", ".css", ".js", ".ts", ".jsx", ".tsx", ".cs", ".py", ".java", ".go", ".rs", ".c", ".h", ".cpp", ".hpp", ".sh", ".sql", ".log", ".pdf", ".png", ".jpg", ".jpeg" })
                picker.FileTypeFilter.Add(extension);
#if WINDOWS
            var window = (Application.Current as App)?.MainWindow;
            if (window is null) throw new InvalidOperationException("The app window is unavailable.");
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(window));
#endif
            var files = await picker.PickMultipleFilesAsync();
            if (files is null || files.Count == 0) return;
            var accepted = new List<ChatAttachment>();
            var rejected = new List<string>();
            var remaining = ChatAttachmentPolicy.RemainingCharacters(_pendingAttachments);
            var remainingImageBytes = ChatAttachmentPolicy.RemainingImageBytes(_pendingAttachments);
            foreach (var file in files)
            {
                if (_pendingAttachments.Count + accepted.Count >= ChatAttachmentPolicy.MaxFiles)
                {
                    rejected.Add($"{file.Name}: maximum of {ChatAttachmentPolicy.MaxFiles} files per message");
                    continue;
                }
                var mediaType = ChatAttachmentPolicy.GetImageMediaType(file.Name);
                var isPdf = ChatAttachmentPolicy.IsAllowedPdfName(file.Name);
                if (!ChatAttachmentPolicy.IsAllowedName(file.Name))
                {
                    rejected.Add($"{file.Name}: file type is unsupported or blocked for safety");
                    continue;
                }
                var properties = await file.GetBasicPropertiesAsync();
                var fileLimit = isPdf ? ChatAttachmentPolicy.MaxPdfBytes : ChatAttachmentPolicy.MaxFileBytes;
                if (properties.Size > (ulong)fileLimit)
                {
                    rejected.Add(isPdf ? $"{file.Name}: larger than the 20 MB PDF limit" : $"{file.Name}: larger than the 2 MB limit");
                    continue;
                }
                if (mediaType is not null)
                {
                    if (properties.Size > (ulong)remainingImageBytes)
                    {
                        rejected.Add($"{file.Name}: image total exceeds the 6 MB limit");
                        continue;
                    }
                    byte[] imageBytes;
                    try { imageBytes = await ChatAttachmentPolicy.ReadBytesAsync(file.Path, ChatAttachmentPolicy.MaxFileBytes); }
                    catch (InvalidDataException error) { rejected.Add($"{file.Name}: {error.Message}"); continue; }
                    if (!ChatAttachmentPolicy.HasValidImageSignature(mediaType, imageBytes))
                    {
                        rejected.Add($"{file.Name}: file content does not match its PNG/JPEG extension");
                        continue;
                    }
                    var encoded = Convert.ToBase64String(imageBytes);
                    var duplicateImage = accepted.Concat(_pendingAttachments).Any(item => item.FileName.Equals(file.Name, StringComparison.OrdinalIgnoreCase));
                    var imageName = duplicateImage
                        ? $"{Path.GetFileNameWithoutExtension(file.Name)} ({accepted.Count + _pendingAttachments.Count + 2}){Path.GetExtension(file.Name)}"
                        : file.Name;
                    accepted.Add(new ChatAttachment(imageName, encoded, mediaType));
                    remainingImageBytes -= (long)properties.Size;
                    continue;
                }
                if (isPdf)
                {
                    byte[] pdfBytes;
                    try { pdfBytes = await ChatAttachmentPolicy.ReadBytesAsync(file.Path, ChatAttachmentPolicy.MaxPdfBytes); }
                    catch (InvalidDataException error) { rejected.Add($"{file.Name}: {error.Message}"); continue; }
                    if (!PdfAttachmentTextExtractor.HasPdfSignature(pdfBytes))
                    {
                        rejected.Add($"{file.Name}: file content does not have a valid PDF signature");
                        continue;
                    }
                    if (remaining <= 0)
                    {
                        rejected.Add($"{file.Name}: message text limit reached");
                        continue;
                    }
                    string pdfText;
                    try
                    {
                        pdfText = await Task.Run(() => PdfAttachmentTextExtractor.Extract(pdfBytes, Math.Min(ChatAttachmentPolicy.MaxCharactersPerFile, remaining)));
                    }
                    catch (Exception ex) when (ex is not (OutOfMemoryException or OperationCanceledException))
                    {
                        rejected.Add($"{file.Name}: {ex.Message}");
                        continue;
                    }
                    var duplicatePdf = accepted.Concat(_pendingAttachments).Any(item => item.FileName.Equals(file.Name, StringComparison.OrdinalIgnoreCase));
                    var pdfName = duplicatePdf
                        ? $"{Path.GetFileNameWithoutExtension(file.Name)} ({accepted.Count + _pendingAttachments.Count + 2}).pdf"
                        : file.Name;
                    accepted.Add(new ChatAttachment(pdfName, pdfText));
                    remaining -= pdfText.Length;
                    continue;
                }
                if (!ChatAttachmentPolicy.IsAllowedTextName(file.Name))
                {
                    rejected.Add($"{file.Name}: file type is unsupported or blocked for safety");
                    continue;
                }
                var content = ChatAttachmentPolicy.BoundContent(await Windows.Storage.FileIO.ReadTextAsync(file));
                var take = Math.Min(content.Length, remaining);
                if (take == 0)
                {
                    rejected.Add($"{file.Name}: message text limit reached");
                    continue;
                }
                var duplicate = accepted.Concat(_pendingAttachments).Any(item => item.FileName.Equals(file.Name, StringComparison.OrdinalIgnoreCase));
                var displayName = duplicate
                    ? $"{Path.GetFileNameWithoutExtension(file.Name)} ({accepted.Count + _pendingAttachments.Count + 2}){Path.GetExtension(file.Name)}"
                    : file.Name;
                accepted.Add(new ChatAttachment(displayName, content[..take]));
                remaining -= take;
                if (take < content.Length) rejected.Add($"{file.Name}: content trimmed to fit the 48,000 character message limit");
            }
            _pendingAttachments.AddRange(accepted);
            PersistPendingAttachments();
            RenderPendingAttachments();
            var textCount = accepted.Count(item => !item.IsImage && !ChatAttachmentPolicy.IsAllowedPdfName(item.FileName));
            var pdfCount = accepted.Count(item => ChatAttachmentPolicy.IsAllowedPdfName(item.FileName));
            var imageCount = accepted.Count(item => item.IsImage);
            var attachmentSummary = new List<string>();
            if (textCount > 0) attachmentSummary.Add(textCount == 1 ? "1 text file" : $"{textCount} text files");
            if (pdfCount > 0) attachmentSummary.Add(pdfCount == 1 ? "1 PDF" : $"{pdfCount} PDFs");
            if (imageCount > 0) attachmentSummary.Add(imageCount == 1 ? "1 image" : $"{imageCount} images");
            RuntimeStatus.Text = accepted.Count == 0
                ? "No supported files were attached. " + string.Join(" · ", rejected)
                : $"Attached {string.Join(" and ", attachmentSummary)}." + (pdfCount > 0 ? " PDF text was extracted locally; scanned PDFs need OCR." : "") + (imageCount > 0 ? " Images require a vision-capable model and an endpoint that accepts image inputs." : "") + (rejected.Count > 0 ? " Some files were skipped: " + string.Join(" · ", rejected) : "");
        }
        catch (Exception ex)
        {
            RuntimeStatus.Text = $"Could not attach files: {ex.Message}";
        }
    }

    private void ClearPendingAttachments(bool persist = true)
    {
        _pendingAttachments.Clear();
        if (persist) PersistPendingAttachments();
        RenderPendingAttachments();
    }

    private void RestorePendingAttachments()
    {
        _pendingAttachments.Clear();
        _pendingAttachments.AddRange(_attachmentDraftStore.Load(_mode));
        RenderPendingAttachments();
    }

    private void PersistPendingAttachments()
    {
        try { _attachmentDraftStore.Save(_mode, _pendingAttachments); }
        catch (Exception ex) { RuntimeStatus.Text = $"Pending attachments could not be saved: {ex.Message}"; }
    }

    private void RenderPendingAttachments()
    {
        AttachmentList.Children.Clear();
        AttachmentList.Visibility = _pendingAttachments.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        foreach (var attachment in _pendingAttachments)
        {
            var chip = new Button
            {
                Content = "▤　" + attachment.FileName + "　×",
                Tag = attachment,
                Background = new SolidColorBrush(ColorHelper.FromArgb(255, 238, 248, 249)),
                Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 44, 79, 87)),
                BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 212, 231, 234)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10, 5, 10, 5)
            };
            AutomationProperties.SetName(chip, $"Remove attachment {attachment.FileName}");
            chip.Click += (_, _) =>
            {
                _pendingAttachments.Remove(attachment);
                PersistPendingAttachments();
                RenderPendingAttachments();
            };
            AttachmentList.Children.Add(chip);
            var preview = new Button
            {
                Content = "Preview",
                Tag = attachment,
                Padding = new Thickness(9, 5, 9, 5),
                Background = new SolidColorBrush(ColorHelper.FromArgb(255, 247, 250, 250)),
                Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 44, 79, 87)),
                BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 212, 231, 234)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12)
            };
            AutomationProperties.SetName(preview, $"Preview attachment {attachment.FileName}");
            preview.Click += async (_, _) => await PreviewPendingAttachmentAsync(attachment);
            AttachmentList.Children.Add(preview);
        }
    }

    private async Task PreviewPendingAttachmentAsync(ChatAttachment attachment)
    {
        UIElement content;
        if (attachment.IsImage)
        {
            try
            {
                var bitmap = new BitmapImage();
                using var stream = new InMemoryRandomAccessStream();
                using var output = stream.GetOutputStreamAt(0);
                using var writer = new DataWriter(output);
                writer.WriteBytes(Convert.FromBase64String(attachment.Content));
                await writer.StoreAsync();
                await writer.FlushAsync();
                stream.Seek(0);
                await bitmap.SetSourceAsync(stream);
                content = new Image { Source = bitmap, Stretch = Stretch.Uniform, MaxWidth = 700, MaxHeight = 420 };
            }
            catch
            {
                content = new TextBlock { Text = "The image could not be rendered locally, but it remains available for the selected vision-capable model.", TextWrapping = TextWrapping.Wrap };
            }
        }
        else
        {
            var text = attachment.Content.Length > 12000 ? attachment.Content[..12000] + "\n\n[Preview truncated locally]" : attachment.Content;
            var preview = new TextBox { Text = text, IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MaxHeight = 420 };
            AutomationProperties.SetName(preview, $"Attachment preview {attachment.FileName}");
            content = preview;
        }
        var note = new TextBlock { Text = "Local read-only preview. The attachment remains untrusted content and is not sent by opening this preview.", TextWrapping = TextWrapping.Wrap, Opacity = 0.82 };
        var dialog = new ContentDialog
        {
            Title = $"Preview · {attachment.FileName}",
            Content = new StackPanel { Spacing = 9, Children = { note, content } },
            CloseButtonText = "Done",
            XamlRoot = XamlRoot
        };
        AutomationProperties.SetName(dialog, $"Attachment preview {attachment.FileName}");
        await dialog.ShowAsync();
    }
}
