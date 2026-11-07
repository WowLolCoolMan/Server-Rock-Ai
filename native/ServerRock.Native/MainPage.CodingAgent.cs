// Coding Agent workspace: the project file list in the sidebar, the built-in text editor
// for a project file, and the persistent undo history of approved file edits.
// Every approved edit (from a reviewed proposal or the editor) goes through RecordWorkspaceUndo,
// which saves an undo entry; undo only runs if the file still matches what was applied.
// Depends on: WorkspaceFileEditor, WorkspaceUndoStore, WorkspaceFileContext (ServerRock.Core) and
// _workspaceUndoEntries / _undoHistoryAvailable in MainPage.xaml.cs.
// Called by: the Undo history button and the file rows built in RenderProjectFiles, the change
// review dialog (MainPage.CodingAgent.Proposals.cs), and project/mode changes that call
// RenderProjectFiles().

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media;
using ServerRock.Core.Infrastructure;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private WorkspaceUndoEntry RecordWorkspaceUndo(string projectName, string root, string relativePath, WorkspaceEditReceipt receipt)
    {
        var entry = new WorkspaceUndoEntry(Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow, projectName,
            Path.GetFullPath(root), relativePath, receipt.PreviousContent, receipt.AppliedHash);
        if (!_undoHistoryAvailable)
        {
            RuntimeStatus.Text = "The file was saved, but undo history is unavailable. The saved history file was left untouched for recovery.";
            return entry;
        }
        try
        {
            _workspaceUndoStore.Add(_workspaceUndoEntries, entry);
            _workspaceUndoEntries.RemoveAll(item => item.Id == entry.Id);
            _workspaceUndoEntries.Insert(0, entry);
            _workspaceUndoEntries = _workspaceUndoEntries.Take(WorkspaceUndoStore.MaxEntries).ToList();
            _persistedUndoIds.Add(entry.Id);
            UpdateWorkspaceBar();
        }
        catch (Exception ex)
        {
            RuntimeStatus.Text = $"The file was saved, but undo history could not be saved: {ex.Message}";
        }
        return entry;
    }

    private void UndoWorkspaceEdit(WorkspaceUndoEntry entry, Button? sourceButton = null)
    {
        try
        {
            _workspaceUndoStore.Undo(entry, _workspaceEditor);
            if (_persistedUndoIds.Remove(entry.Id)) _workspaceUndoStore.Remove(_workspaceUndoEntries, entry.Id);
            _workspaceUndoEntries.RemoveAll(item => item.Id == entry.Id);
            UpdateWorkspaceBar();
            if (sourceButton is not null)
            {
                sourceButton.Content = "Change undone";
                sourceButton.IsEnabled = false;
            }
            if (_lastEditReceipt?.AppliedHash == entry.AppliedHash) _lastEditReceipt = null;
            RenderProjectFiles();
            RuntimeStatus.Text = $"Undid change to {entry.RelativePath}";
        }
        catch (Exception ex) { RuntimeStatus.Text = $"Undo stopped: {ex.Message}"; }
    }

    private async void UndoHistoryClick(object sender, RoutedEventArgs e) => await RunUiActionAsync(UndoHistoryClickAsync);

    private async Task UndoHistoryClickAsync()
    {
        if (!_undoHistoryAvailable)
        {
            RuntimeStatus.Text = "Undo history could not be read. Its saved file was preserved; inspect workspace-undo.json in the app data folder before trying again.";
            return;
        }
        if (_workspaceUndoEntries.Count == 0)
        {
            RuntimeStatus.Text = "Approved project file changes will appear here for undo.";
            return;
        }
        var list = new ListView { SelectionMode = ListViewSelectionMode.Single, MaxHeight = 360 };
        foreach (var entry in _workspaceUndoEntries.OrderByDescending(item => item.CreatedUtc))
        {
            var item = new ListViewItem { Tag = entry, Padding = new Thickness(10, 7, 10, 7) };
            item.Content = new StackPanel { Spacing = 2, Children =
            {
                new TextBlock { Text = $"{entry.ProjectName} · {entry.RelativePath}", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis },
                new TextBlock { Text = $"{entry.CreatedUtc.LocalDateTime:g} · {entry.RootPath}", FontSize = 10, Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 105, 124, 129)), TextTrimming = TextTrimming.CharacterEllipsis }
            } };
            list.Items.Add(item);
        }
        var details = new TextBlock { Text = "Select a change to review its location. Undo stops if the file changed after the approved edit.", TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 84, 102, 107)) };
        var panel = new StackPanel { Spacing = 8, Children = { list, details } };
        var dialog = new ContentDialog { Title = "Project change history", Content = panel, PrimaryButtonText = "Undo selected", CloseButtonText = "Done", IsPrimaryButtonEnabled = false, XamlRoot = XamlRoot };
        list.SelectionChanged += (_, _) =>
        {
            dialog.IsPrimaryButtonEnabled = list.SelectedItem is ListViewItem;
            if (list.SelectedItem is ListViewItem { Tag: WorkspaceUndoEntry selected })
                details.Text = $"{selected.RootPath}{Path.DirectorySeparatorChar}{selected.RelativePath}\nThe previous file contents are stored locally so you can undo this approved change. Undo stops if the file has changed since then.";
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary && list.SelectedItem is ListViewItem { Tag: WorkspaceUndoEntry selectedEntry })
            UndoWorkspaceEdit(selectedEntry);
    }

    private static bool SamePath(string left, string right)
    {
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(left)), Path.TrimEndingDirectorySeparator(Path.GetFullPath(right)), comparison);
    }

    private void RenderProjectFiles()
    {
        UpdateWorkspaceBar();
        ProjectFileList.Children.Clear();
        var project = _projects.FirstOrDefault(item => item.Id == _activeProjectId);
        if (_mode != "Coding Agent" || project is null || project.FolderPaths.Count == 0)
        {
            CodeFilesHeader.Visibility = Visibility.Collapsed;
            return;
        }
        CodeFilesHeader.Visibility = Visibility.Visible;
        CodeFilesHeader.Text = $"FILES · {Path.GetFileName(project.FolderPaths[0].TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))}";
        foreach (var file in WorkspaceFileContext.ReadSelectedFiles([project.FolderPaths[0]])
            .OrderBy(item => item.Path, StringComparer.OrdinalIgnoreCase))
        {
            var button = new Button
            {
                Content = "▤　" + file.Path,
                Tag = (project.FolderPaths[0], file.Path),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = new SolidColorBrush(ColorHelper.FromArgb(255, 223, 243, 247)),
                Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 57, 82, 90)),
                Padding = new Thickness(9, 6, 6, 6),
                CornerRadius = new CornerRadius(7),
                FontSize = 11
            };
            button.Click += ProjectFileClick;
            ProjectFileList.Children.Add(button);
        }
        if (ProjectFileList.Children.Count == 0)
            ProjectFileList.Children.Add(new TextBlock { Text = "No readable text files in this folder.", TextWrapping = TextWrapping.Wrap, FontSize = 11, Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 103, 126, 133)), Margin = new Thickness(8, 4, 8, 4) });
    }

    private async void ProjectFileClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ValueTuple<string, string> selection }) return;
        try
        {
            var originalHash = _workspaceEditor.GetCurrentHash(selection.Item1, selection.Item2)
                ?? throw new FileNotFoundException("This file no longer exists in the selected project.");
            var originalText = _workspaceEditor.ReadText(selection.Item1, selection.Item2);
            var editor = new TextBox
            {
                Text = originalText,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.NoWrap,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                Height = 420,
                MaxLength = WorkspaceFileEditor.MaxCharacters
            };
            AutomationProperties.SetName(editor, $"Edit project file {selection.Item2}");
            var saveHint = new TextBlock { Text = "No unsaved changes", FontSize = 11, Opacity = 0.75 };
            var dialog = new ContentDialog
            {
                Title = $"Edit project file · {selection.Item2}",
                Content = new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock { Text = "Changes stay local and are written only when you select Save changes. The file is checked for newer edits before saving.", TextWrapping = TextWrapping.Wrap },
                        editor,
                        saveHint
                    }
                },
                PrimaryButtonText = "Save changes",
                CloseButtonText = "Cancel",
                IsPrimaryButtonEnabled = false,
                XamlRoot = XamlRoot
            };
            dialog.Opened += (_, _) => editor.Focus(FocusState.Programmatic);
            editor.TextChanged += (_, _) =>
            {
                var changed = !string.Equals(editor.Text, originalText, StringComparison.Ordinal);
                var valid = changed && editor.Text.Length <= WorkspaceFileEditor.MaxCharacters && !editor.Text.Contains('\0');
                dialog.IsPrimaryButtonEnabled = valid;
                saveHint.Text = editor.Text.Length > WorkspaceFileEditor.MaxCharacters
                    ? $"File exceeds {WorkspaceFileEditor.MaxCharacters:N0} characters and cannot be saved."
                    : editor.Text.Contains('\0') ? "Binary null characters are not supported."
                    : changed ? $"Unsaved changes · {editor.Text.Length:N0} characters"
                    : "No unsaved changes";
            };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
            var receipt = _workspaceEditor.Apply(selection.Item1, selection.Item2, editor.Text, originalHash);
            RenderProjectFiles();
            RuntimeStatus.Text = $"Saved {selection.Item2} in the selected project.";
            var projectName = _projects.FirstOrDefault(item => item.FolderPaths.Any(folder => SamePath(folder, selection.Item1)))?.Name ?? "Project";
            var undoEntry = RecordWorkspaceUndo(projectName, selection.Item1, selection.Item2, receipt);
            if (!_persistedUndoIds.Contains(undoEntry.Id))
            {
                var fallback = new ContentDialog
                {
                    Title = "Undo history could not be saved",
                    Content = new TextBlock { Text = "The file was saved, but this change could not be added to persistent undo history. You can undo it now while this app remains open.", TextWrapping = TextWrapping.Wrap },
                    PrimaryButtonText = "Undo this edit",
                    CloseButtonText = "Keep saved",
                    XamlRoot = XamlRoot
                };
                if (await fallback.ShowAsync() == ContentDialogResult.Primary)
                {
                    _workspaceEditor.Undo(receipt);
                    RenderProjectFiles();
                    RuntimeStatus.Text = $"Undid changes to {selection.Item2}.";
                }
            }
        }
        catch (Exception ex) { RuntimeStatus.Text = $"Project file could not be opened or saved: {ex.Message}"; }
        finally { RestoreProjectFileFocus(selection.Item1, selection.Item2); }
    }

    private void RestoreProjectFileFocus(string root, string relativePath)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var fileButton = ProjectFileList.Children.OfType<Button>().FirstOrDefault(button =>
                button.Tag is ValueTuple<string, string> path &&
                SamePath(path.Item1, root) &&
                path.Item2.Equals(relativePath, StringComparison.OrdinalIgnoreCase));
            if (fileButton?.Focus(FocusState.Programmatic) == true) return;
            if (ProjectFoldersButton.Visibility == Visibility.Visible &&
                ProjectFoldersButton.IsEnabled && ProjectFoldersButton.Focus(FocusState.Programmatic))
                return;
            PromptBox.Focus(FocusState.Programmatic);
        });
    }
}
