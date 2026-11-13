// Coding Agent workspace: the project file list in the sidebar, the built-in text editor
// for a project file, and the persistent undo history of approved file edits.
// Every approved edit (from a reviewed proposal or the editor) goes through RecordWorkspaceUndo,
// which saves an undo entry; undo only runs if the file still matches what was applied.
// Depends on: WorkspaceFileEditor, WorkspaceUndoStore, WorkspaceFileContext (ServerRock.Core) and
// _workspaceUndoEntries / _undoHistoryAvailable in MainPage.xaml.cs.
// Called by: the Undo history button and the file rows built in RenderProjectFiles, the change
// review dialog (MainPage.CodingAgent.Proposals.cs), and project/mode changes that call
// RenderProjectFiles().

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media;
using ServerRock.Core.Models;
using ServerRock.Core.Infrastructure;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private void RenderOpenFileTabs(LocalProject? project)
    {
        OpenFileTabs.Children.Clear();
        if (project is null || !string.Equals(_openProjectFileProjectId, project.Id, StringComparison.Ordinal) || _openProjectFilePaths.Count == 0)
        {
            OpenFileTabsScroll.Visibility = Visibility.Collapsed;
            return;
        }

        foreach (var relativePath in _openProjectFilePaths.ToArray())
        {
            if (!project.FolderPaths.Any(root => _workspaceEditor.GetCurrentHash(root, relativePath) is not null)) continue;
            var tab = new Button
            {
                Content = relativePath,
                Tag = relativePath,
                Style = UiTheme.Style("ContextButtonStyle"),
                Padding = new Thickness(9, 5, 9, 5),
                MaxWidth = 240,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };
            AutomationProperties.SetName(tab, $"Open project file tab {relativePath}");
            tab.Click += OpenFileTabClick;
            OpenFileTabs.Children.Add(tab);

            var close = new Button
            {
                Content = "×",
                Tag = relativePath,
                Style = UiTheme.Style("ContextButtonStyle"),
                Padding = new Thickness(6, 4, 6, 4),
            };
            ToolTipService.SetToolTip(close, $"Close file tab {relativePath}");
            AutomationProperties.SetName(close, $"Close project file tab {relativePath}");
            close.Click += CloseFileTabClick;
            OpenFileTabs.Children.Add(close);
        }
        OpenFileTabsScroll.Visibility = OpenFileTabs.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void OpenFileTabClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string relativePath }) return;
        var project = _projects.FirstOrDefault(item => item.Id == _activeProjectId);
        var root = project?.FolderPaths.FirstOrDefault(folder => _workspaceEditor.GetCurrentHash(folder, relativePath) is not null);
        if (root is null)
        {
            RuntimeStatus.Text = $"Project file is no longer available: {relativePath}";
            RenderOpenFileTabs(project);
            return;
        }
        await OpenProjectFileAsync(root, relativePath);
    }

    private void CloseFileTabClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string relativePath }) return;
        _openProjectFilePaths.RemoveAll(path => path.Equals(relativePath, StringComparison.OrdinalIgnoreCase));
        if (string.Equals(_settings.LastProjectFilePath, relativePath, StringComparison.OrdinalIgnoreCase))
        {
            _settings.LastProjectFilePath = _openProjectFilePaths.LastOrDefault();
            _settings.LastProjectFileProjectId = _settings.LastProjectFilePath is null ? null : _openProjectFileProjectId;
        }
        PersistOpenProjectFiles();
        RenderOpenFileTabs(_projects.FirstOrDefault(item => item.Id == _activeProjectId));
        UpdateWorkspaceBar();
    }

    private void PersistOpenProjectFiles()
    {
        _settings.OpenProjectFileProjectId = _openProjectFileProjectId;
        _settings.OpenProjectFiles = _openProjectFilePaths.Take(8).ToList();
        SaveSettings();
    }

    private async void ResumeFileClick(object sender, RoutedEventArgs e)
    {
        var project = _projects.FirstOrDefault(item => item.Id == _activeProjectId);
        if (!CanResumeLastProjectFile(project))
        {
            RuntimeStatus.Text = "The last project file is no longer available in this project.";
            UpdateWorkspaceBar();
            return;
        }
        var relativePath = _settings.LastProjectFilePath!;
        var root = project!.FolderPaths.First(folder => _workspaceEditor.GetCurrentHash(folder, relativePath) is not null);
        await RunUiActionAsync(() => OpenProjectFileAsync(root, relativePath));
    }

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
                new TextBlock { Text = $"{entry.CreatedUtc.LocalDateTime:g} · {entry.RootPath}", FontSize = UiTheme.FontSize("CaptionFontSize"), Foreground = UiTheme.Brush("MutedInkBrush"), TextTrimming = TextTrimming.CharacterEllipsis }
            } };
            list.Items.Add(item);
        }
        var details = new TextBlock { Text = "Select a change to review its location. Undo stops if the file changed after the approved edit.", TextWrapping = TextWrapping.Wrap, FontSize = UiTheme.FontSize("SmallFontSize"), Foreground = UiTheme.Brush("MutedInkBrush") };
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

    private void RenderProjectFiles()
    {
        UpdateWorkspaceBar();
        ProjectFileList.Items.Clear();
        var project = _projects.FirstOrDefault(item => item.Id == _activeProjectId);
        if (_mode != WorkspaceModes.CodingAgent || project is null || project.FolderPaths.Count == 0)
        {
            CodeFilesHeader.Visibility = Visibility.Collapsed;
            return;
        }
        CodeFilesHeader.Visibility = Visibility.Visible;
        CodeFilesHeader.Text = $"FILES · {Path.GetFileName(project.FolderPaths[0].TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))} · BOUNDED SAFE VIEW";
        AutomationProperties.SetHelpText(CodeFilesHeader, WorkspaceFileContext.BrowserBoundaryDescription);
        var files = WorkspaceFileContext.EnumerateTextFiles([project.FolderPaths[0]])
            .Select(item => (Path: item.RelativePath, Content: string.Empty))
            .OrderBy(item => item.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (!string.Equals(_projectExplorerProjectId, project.Id, StringComparison.Ordinal))
        {
            _projectExplorerProjectId = project.Id;
            _expandedProjectFolders.Clear();
            foreach (var folder in GetProjectFolderPaths(files))
                if (!folder.Contains('/', StringComparison.Ordinal)) _expandedProjectFolders.Add(folder);
        }
        var folders = GetProjectFolderPaths(files);
        foreach (var folder in folders)
        {
            var parent = GetProjectPathParent(folder);
            if (parent is not null && !_expandedProjectFolders.Contains(parent)) continue;
            var depth = folder.Count(character => character == '/');
            var folderButton = new Button
            {
                Content = $"{(_expandedProjectFolders.Contains(folder) ? "▾" : "▸")}　📁 {folder[(folder.LastIndexOf('/') + 1)..]}",
                Tag = folder,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = UiTheme.Brush("SurfaceAltBrush"),
                Foreground = UiTheme.Brush("MutedInkBrush"),
                Padding = new Thickness(9 + depth * 14, 5, 6, 5),
                CornerRadius = new CornerRadius(7),
                FontSize = UiTheme.FontSize("CaptionFontSize")
            };
            AutomationProperties.SetName(folderButton, $"{(_expandedProjectFolders.Contains(folder) ? "Collapse" : "Expand")} project folder {folder}");
            folderButton.Click += ProjectFolderToggleClick;
            ProjectFileList.Items.Add(folderButton);
        }
        foreach (var file in files)
        {
            var normalizedPath = NormalizeProjectRelativePath(file.Path);
            var parent = GetProjectPathParent(normalizedPath);
            if (parent is not null && !_expandedProjectFolders.Contains(parent)) continue;
            var depth = normalizedPath.Count(character => character == '/');
            var button = new Button
            {
                Content = "▤　" + file.Path,
                Tag = (project.FolderPaths[0], file.Path),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = UiTheme.Brush("AccentSoftBrush"),
                Foreground = UiTheme.Brush("AccentInkBrush"),
                Padding = new Thickness(9 + depth * 14, 6, 6, 6),
                CornerRadius = new CornerRadius(7),
                FontSize = UiTheme.FontSize("CaptionFontSize")
            };
            button.Click += ProjectFileClick;
            ProjectFileList.Items.Add(button);
        }
        if (files.Length == 0)
            ProjectFileList.Items.Add(new TextBlock { Text = "No readable text files in this folder.", TextWrapping = TextWrapping.Wrap, FontSize = UiTheme.FontSize("CaptionFontSize"), Foreground = UiTheme.Brush("MutedInkBrush"), Margin = new Thickness(8, 4, 8, 4) });
    }

    private void ProjectFolderToggleClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string folder }) return;
        if (!_expandedProjectFolders.Add(folder)) _expandedProjectFolders.Remove(folder);
        RenderProjectFiles();
    }

    private static SortedSet<string> GetProjectFolderPaths(IEnumerable<(string Path, string Content)> files)
    {
        var folders = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            var path = NormalizeProjectRelativePath(file.Path);
            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            for (var index = 1; index < parts.Length; index++) folders.Add(string.Join("/", parts.Take(index)));
        }
        return folders;
    }

    private static string NormalizeProjectRelativePath(string path) => path.Replace('\\', '/').Trim('/');

    private static string? GetProjectPathParent(string path)
    {
        var separator = path.LastIndexOf('/');
        return separator < 0 ? null : path[..separator];
    }

    private async Task OpenQuickFileAsync()
    {
        if (_mode != WorkspaceModes.CodingAgent)
        {
            RuntimeStatus.Text = "Quick file open is available in the Coding Agent workspace.";
            return;
        }
        var project = _projects.FirstOrDefault(item => item.Id == _activeProjectId);
        if (project is null || project.FolderPaths.Count == 0)
        {
            RuntimeStatus.Text = "Choose a project folder before opening a file.";
            return;
        }
        var root = project.FolderPaths[0];
        var files = WorkspaceFileContext.EnumeratePreviewableFiles([root])
            .Where(file => file.Kind.Equals("text", StringComparison.OrdinalIgnoreCase))
            .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var query = new TextBox { PlaceholderText = "Search project files", MaxLength = 200 };
        var list = new ListView { MaxHeight = 360, SelectionMode = ListViewSelectionMode.Single };
        AutomationProperties.SetName(query, "Search project files");
        AutomationProperties.SetName(list, "Quick open project file results");
        void Refresh()
        {
            list.Items.Clear();
            var needle = query.Text.Trim();
            foreach (var file in files.Where(file => string.IsNullOrWhiteSpace(needle) || file.RelativePath.Contains(needle, StringComparison.OrdinalIgnoreCase)))
            {
                var item = new ListViewItem { Content = file.RelativePath, Tag = file.RelativePath };
                AutomationProperties.SetName(item, $"Open project file {file.RelativePath}");
                list.Items.Add(item);
            }
            if (list.Items.Count > 0) list.SelectedIndex = 0;
        }
        query.TextChanged += (_, _) => Refresh();
        var dialog = new ContentDialog
        {
            Title = "Quick open project file",
            Content = new StackPanel { Spacing = 8, Children = { query, list, new TextBlock { Text = "Only bounded, secret-filtered text files from the selected project folder are shown. Opening a file uses the existing reviewable local editor.", TextWrapping = TextWrapping.Wrap, FontSize = UiTheme.FontSize("CaptionFontSize"), Opacity = 0.78 } } },
            PrimaryButtonText = "Open selected",
            CloseButtonText = "Cancel",
            IsPrimaryButtonEnabled = false,
            XamlRoot = XamlRoot
        };
        list.SelectionChanged += (_, _) => dialog.IsPrimaryButtonEnabled = list.SelectedItem is ListViewItem;
        dialog.Opened += (_, _) => query.Focus(FocusState.Programmatic);
        Refresh();
        if (await dialog.ShowAsync() == ContentDialogResult.Primary && list.SelectedItem is ListViewItem { Tag: string relativePath })
            await OpenProjectFileAsync(root, relativePath);
    }

    private async void ProjectFileClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ValueTuple<string, string> selection }) return;
        await OpenProjectFileAsync(selection.Item1, selection.Item2);
    }

    private async Task OpenProjectFileAsync(string root, string relativePath)
    {
        try
        {
            var originalHash = _workspaceEditor.GetCurrentHash(root, relativePath)
                ?? throw new FileNotFoundException("This file no longer exists in the selected project.");
            var originalText = _workspaceEditor.ReadText(root, relativePath);
            _openProjectFileProjectId = _projects.FirstOrDefault(item => item.FolderPaths.Any(folder => WorkspacePath.AreSame(folder, root)))?.Id;
            _openProjectFilePaths.RemoveAll(path => path.Equals(relativePath, StringComparison.OrdinalIgnoreCase));
            _openProjectFilePaths.Add(relativePath);
            while (_openProjectFilePaths.Count > 8) _openProjectFilePaths.RemoveAt(0);
            _settings.LastProjectFileProjectId = _openProjectFileProjectId;
            _settings.LastProjectFilePath = relativePath;
            PersistOpenProjectFiles();
            var editor = new TextBox
            {
                Text = originalText,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.NoWrap,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                Height = 420,
                MaxLength = WorkspaceFileEditor.MaxCharacters
            };
            AutomationProperties.SetName(editor, $"Edit project file {relativePath}");
            var saveHint = new TextBlock { Text = "No unsaved changes", FontSize = UiTheme.FontSize("CaptionFontSize"), Foreground = UiTheme.Brush("MutedInkBrush"), Opacity = 0.75 };
            var dialog = new ContentDialog
            {
                Title = $"Edit project file · {relativePath}",
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
            var receipt = _workspaceEditor.Apply(root, relativePath, editor.Text, originalHash);
            RenderProjectFiles();
            RuntimeStatus.Text = $"Saved {relativePath} in the selected project.";
            var projectName = _projects.FirstOrDefault(item => item.FolderPaths.Any(folder => WorkspacePath.AreSame(folder, root)))?.Name ?? "Project";
            var undoEntry = RecordWorkspaceUndo(projectName, root, relativePath, receipt);
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
                    RuntimeStatus.Text = $"Undid changes to {relativePath}.";
                }
            }
        }
        catch (Exception ex) { RuntimeStatus.Text = $"Project file could not be opened or saved: {ex.Message}"; }
        finally { RestoreProjectFileFocus(root, relativePath); }
    }

    private void RestoreProjectFileFocus(string root, string relativePath)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var fileButton = ProjectFileList.Items.OfType<Button>().FirstOrDefault(button =>
                button.Tag is ValueTuple<string, string> path &&
                WorkspacePath.AreSame(path.Item1, root) &&
                path.Item2.Equals(relativePath, StringComparison.OrdinalIgnoreCase));
            if (fileButton?.Focus(FocusState.Programmatic) == true) return;
            if (ProjectFoldersButton.Visibility == Visibility.Visible &&
                ProjectFoldersButton.IsEnabled && ProjectFoldersButton.Focus(FocusState.Programmatic))
                return;
            PromptBox.Focus(FocusState.Programmatic);
        });
    }
}
