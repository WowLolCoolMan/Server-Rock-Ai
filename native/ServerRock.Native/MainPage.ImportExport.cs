// Sidebar Export / Import of saved chats as a portable JSON or human-readable Markdown archive.
// Export writes every saved conversation through the canonical archive validation path. Import reads a
// native or legacy Electron archive (25 MB limit), shows a summary of what will be added, skipped
// as duplicates, or given new IDs, and only then merges and saves.
// Depends on: ConversationArchive and ConversationStore (ServerRock.Core), the system file pickers
// (Windows needs the window handle, hence the #if WINDOWS blocks), _sessions in MainPage.xaml.cs.
// Called by: the Export/Import sidebar buttons in MainPage.xaml and the Settings > Chats & history
// section.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ServerRock.Core.Infrastructure;
using ServerRock.Core.Models;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private async void ExportChatsClick(object sender, RoutedEventArgs e) { CloseCompactSidebar(); await RunUiActionAsync(ExportChatsAsync); }
    private async void ExportMarkdownFolderClick(object sender, RoutedEventArgs e) { CloseCompactSidebar(); await RunUiActionAsync(ExportMarkdownFolderAsync); }
    private async void ImportChatsClick(object sender, RoutedEventArgs e) { CloseCompactSidebar(); await RunUiActionAsync(ImportChatsAsync); }

    private async Task ExportChatsAsync()
    {
        if (_requestCancellation is not null)
        {
            RuntimeStatus.Text = "Wait for the current response to finish before exporting chats.";
            return;
        }
        if (!_historyAvailable)
        {
            RuntimeStatus.Text = "Conversation history is unavailable, so an archive cannot be created.";
            return;
        }
        SaveCurrentSession();
        if (!_historyAvailable) return;
        try
        {
            var json = ConversationArchive.Serialize(_sessions);
            var picker = new Windows.Storage.Pickers.FileSavePicker
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
                SuggestedFileName = $"{ProductIdentity.FileNameStem}-chats-{DateTime.Now:yyyy-MM-dd}"
            };
            picker.FileTypeChoices.Add($"{ProductIdentity.ShortName} chat archive", new List<string> { ".json" });
            picker.FileTypeChoices.Add($"{ProductIdentity.ShortName} Markdown transcript", new List<string> { ".md" });
#if WINDOWS
            var window = (Application.Current as App)?.MainWindow;
            if (window is null) throw new InvalidOperationException("The app window is unavailable.");
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(window));
#endif
            // Let the flyout's Win32 pointer and accessibility events drain before the native modal dialog enters its message loop.
            await Task.Delay(50);
            var file = await picker.PickSaveFileAsync();
            if (file is null) return;
            if (file.FileType.Equals(".md", StringComparison.OrdinalIgnoreCase))
            {
                await Windows.Storage.FileIO.WriteTextAsync(file, ConversationMarkdownExporter.Serialize(_sessions));
                RuntimeStatus.Text = $"Exported {_sessions.Count} saved conversation(s) to {file.Name}. The Markdown transcript contains metadata, messages, source links, and attachment names.";
            }
            else
            {
                await Windows.Storage.FileIO.WriteTextAsync(file, json);
                RuntimeStatus.Text = $"Exported {_sessions.Count} saved conversation(s) to {file.Name}. The JSON archive contains their messages and attached content.";
            }
        }
        catch (Exception ex) { RuntimeStatus.Text = $"Could not export chats: {ex.Message}"; }
    }

    private async Task ExportMarkdownFolderAsync()
    {
        if (_requestCancellation is not null)
        {
            RuntimeStatus.Text = "Wait for the current response to finish before exporting chats.";
            return;
        }
        if (!_historyAvailable)
        {
            RuntimeStatus.Text = "Conversation history is unavailable, so Markdown files cannot be created.";
            return;
        }
        SaveCurrentSession();
        if (!_historyAvailable) return;
        try
        {
            var picker = new Windows.Storage.Pickers.FolderPicker
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add("*");
#if WINDOWS
            var window = (Application.Current as App)?.MainWindow;
            if (window is null) throw new InvalidOperationException("The app window is unavailable.");
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(window));
#endif
            await Task.Delay(50);
            var destination = await picker.PickSingleFolderAsync();
            if (destination is null) return;
            var exportFolder = await destination.CreateFolderAsync($"{ProductIdentity.FileNameStem}-markdown-{DateTime.Now:yyyy-MM-dd}", Windows.Storage.CreationCollisionOption.GenerateUniqueName);
            foreach (var session in _sessions)
            {
                var file = await exportFolder.CreateFileAsync(ConversationMarkdownExporter.FileNameFor(session), Windows.Storage.CreationCollisionOption.ReplaceExisting);
                await Windows.Storage.FileIO.WriteTextAsync(file, ConversationMarkdownExporter.Serialize([session]));
            }
            RuntimeStatus.Text = $"Exported {_sessions.Count} conversation(s) as individual Markdown files to {exportFolder.Name}.";
        }
        catch (Exception ex) { RuntimeStatus.Text = $"Could not export Markdown folder: {ex.Message}"; }
    }

    private async Task ImportChatsAsync()
    {
        if (_requestCancellation is not null)
        {
            RuntimeStatus.Text = "Wait for the current response to finish before importing chats.";
            return;
        }
        if (!_historyAvailable)
        {
            RuntimeStatus.Text = "Conversation history is unavailable, so chats cannot be imported.";
            return;
        }
        try
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
                ViewMode = Windows.Storage.Pickers.PickerViewMode.List
            };
            picker.FileTypeFilter.Add(".json");
#if WINDOWS
            var window = (Application.Current as App)?.MainWindow;
            if (window is null) throw new InvalidOperationException("The app window is unavailable.");
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(window));
#endif
            // Keep the native file dialog outside the menu's pointer-release/accessibility event stack.
            await Task.Delay(50);
            var file = await picker.PickSingleFileAsync();
            if (file is null) return;
            var properties = await file.GetBasicPropertiesAsync();
            if (properties.Size > ConversationArchive.MaxArchiveBytes)
            {
                RuntimeStatus.Text = "This archive is larger than the 25 MB import limit.";
                return;
            }
            await using var archiveStream = await file.OpenStreamForReadAsync();
            var incoming = await ConversationArchive.ParseAsync(archiveStream);
            if (incoming.Count == 0)
            {
                RuntimeStatus.Text = "The selected archive contains no conversations.";
                return;
            }
            var plan = ConversationArchive.PlanImport(_sessions, incoming);
            if (plan.Conversations.Count == 0)
            {
                RuntimeStatus.Text = $"No chats added. This file contains {plan.SkippedDuplicates} conversation(s) already in your history.";
                return;
            }
            var previewTitles = string.Join("\n", plan.Conversations.Take(5).Select(session => $"• {session.Title}"));
            if (plan.Conversations.Count > 5) previewTitles += $"\n• and {plan.Conversations.Count - 5} more";
            var dialog = new ContentDialog
            {
                Title = "Import conversations?",
                Content = $"File: {file.Name}\n\nChats to add: {plan.Conversations.Count}\nAlready present: {plan.SkippedDuplicates}\nID conflicts kept as separate chats: {plan.ReassignedIds}\n\nPreview:\n{previewTitles}\n\nImport accepts native chat archives and Electron localStorage JSON exports. It leaves existing history and the selected file unchanged. Electron exports retain chat titles, messages, archive flags, and fork/branch metadata, while project links, folder paths, provider settings, and credentials are intentionally not imported. Native archives retain supported per-chat context. The app does not read an Electron browser profile automatically. Review the added chats before sending them to a model.",
                PrimaryButtonText = "Import chats",
                CloseButtonText = "Cancel",
                XamlRoot = XamlRoot
            };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
            SaveCurrentSession();
            if (!_historyAvailable) return;
            // Re-plan after saving the active chat so the import remains idempotent even
            // if the active conversation changed between preview and confirmation.
            plan = ConversationArchive.PlanImport(_sessions, plan.Conversations);
            if (plan.Conversations.Count == 0)
            {
                RuntimeStatus.Text = "No chats added. The selected conversations are already in your history.";
                return;
            }
            _sessions.AddRange(plan.Conversations);
            if (!PersistConversationEdits())
            {
                _sessions.RemoveAll(session => plan.Conversations.Contains(session));
                RenderRecent();
                return;
            }
            RuntimeStatus.Text = $"Imported {plan.Conversations.Count} conversation(s) from {file.Name}; skipped {plan.SkippedDuplicates} already present.";
        }
        catch (Exception ex) { RuntimeStatus.Text = $"Could not import chats: {ex.Message}"; }
    }
}
