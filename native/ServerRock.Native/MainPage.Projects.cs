// Projects: a named context (instructions + explicitly chosen local folders) that a chat
// or Coding Agent task runs in. Covers the sidebar project list and its overflow menu, the
// create/edit/remove dialogs, selecting the active project, the folder manager, and saving
// projects.json.
// Depends on: ProjectStore and LocalProject (SkaiDesktop.Core), the folder picker, and page state
// in MainPage.xaml.cs (_projects, _activeProjectId, _activeProjectName, _projectsAvailable).
// Called by: project buttons in MainPage.xaml and the sidebar list built by RenderProjects, the
// /project, /newproject, and /folders slash commands, SwitchModeAsync (Coding Agent without
// folders), and GitHub clone (adds the cloned repo as a project).

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Media;
using SkaiDesktop.Core.Infrastructure;
using SkaiDesktop.Core.Models;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private async void NewProjectClick(object sender, RoutedEventArgs e) => await RunCompactProjectActionAsync(NewProjectClickAsync);

    private async Task RunCompactProjectActionAsync(Func<Task> action)
    {
        var restoreComposerFocus = _compactNavigation;
        CloseCompactSidebar();
        try { await RunUiActionAsync(action); }
        finally
        {
            if (restoreComposerFocus)
                DispatcherQueue.TryEnqueue(() => PromptBox.Focus(FocusState.Programmatic));
        }
    }

    private async Task NewProjectClickAsync()
    {
        if (_requestCancellation is not null)
        {
            RuntimeStatus.Text = "Wait for the current response to finish before changing projects.";
            return;
        }
        if (!await ConfirmTemporaryDiscardAsync("create a project")) return;
        var name = new TextBox { Header = "Project name", PlaceholderText = "Name this workspace" };
        AutomationProperties.SetName(name, "Project name");
        AutomationProperties.SetHelpText(name, "Enter a short name for this Coding Agent workspace.");
        var instructions = new TextBox { Header = "Project instructions", PlaceholderText = "What should the assistant know about this project?", AcceptsReturn = true, Height = 76, TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetName(instructions, "Project instructions");
        AutomationProperties.SetHelpText(instructions, "Optional guidance that will be available in this project’s Coding Agent conversations.");
        var selectedFolders = new List<string>();
        var foldersLabel = new TextBlock { Text = "No folders selected. The agent cannot read local files until you add a folder.", TextWrapping = TextWrapping.Wrap, Opacity = 0.78 };
        AutomationProperties.SetName(foldersLabel, "Selected project folders");
        AutomationProperties.SetHelpText(foldersLabel, "Shows which local folders the Coding Agent can use as project context.");
        AutomationProperties.SetLiveSetting(foldersLabel, AutomationLiveSetting.Polite);
        var addFolder = new Button { Content = "＋ Add local folder" };
        AutomationProperties.SetName(addFolder, "Add local project folder");
        AutomationProperties.SetHelpText(addFolder, "Choose a local folder to make its files available as project context.");
        addFolder.Click += async (_, _) =>
        {
            var path = await PickWorkspaceFolderAsync();
            if (path is not null && !selectedFolders.Contains(path, StringComparer.OrdinalIgnoreCase)) selectedFolders.Add(path);
            foldersLabel.Text = selectedFolders.Count == 0 ? "No folders selected. The agent cannot read local files until you add a folder." : string.Join("\n", selectedFolders);
        };
        var panel = new StackPanel { Spacing = 10 };
        panel.Children.Add(name);
        panel.Children.Add(instructions);
        panel.Children.Add(addFolder);
        panel.Children.Add(foldersLabel);
        var dialog = new ContentDialog { Title = "Create project", Content = panel, PrimaryButtonText = "Create", CloseButtonText = "Cancel", XamlRoot = XamlRoot };
        dialog.Opened += (_, _) => name.Focus(FocusState.Programmatic);
        if (await dialog.ShowAsync() != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(name.Text)) return;
        SaveCurrentSession();
        ClearPendingAttachments();
        var project = new LocalProject { Name = name.Text.Trim(), Instructions = instructions.Text.Trim(), FolderPaths = [.. selectedFolders] };
        _projects.Add(project);
        if (!SaveProjects())
        {
            _projects.Remove(project);
            RenderProjects();
            return;
        }
        _activeProjectId = project.Id;
        _activeProjectName = project.Name;
        _activeChatFolder = null;
        _includeProjectFilesInChat = false;
        _settings.ProjectInstructions = project.Instructions;
        SaveSettings();
        PageTitle.Text = project.Name;
        ProjectBadge.Visibility = Visibility.Visible;
        UpdateProjectFilesControls();
        UpdateWebSearchButton();
        RenderProjects();
        _activeSessionId = null;
        _editingMessageIndex = null;
        _messages.Clear();
        ConversationMessages.Children.Clear();
        WelcomePanel.Visibility = Visibility.Visible;
        ConversationScroll.Visibility = Visibility.Collapsed;
        UpdateTemporaryControls();
        PromptBox.Focus(FocusState.Programmatic);
    }

    private void RenderProjects()
    {
        ProjectList.Children.Clear();
        foreach (var project in _projects.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            var button = new Button
            {
                Content = "▱　" + project.Name,
                Tag = project.Id,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = UiTheme.Brush(project.Id == _activeProjectId ? "AccentSoftBrush" : "SurfaceAltBrush"),
                Foreground = UiTheme.Brush("InkBrush"),
                Padding = new Thickness(10, 8, 10, 8),
                CornerRadius = new CornerRadius(8)
            };
            AutomationProperties.SetName(button, $"Open project: {project.Name}");
            button.Click += ProjectClick;
            var actions = new Button
            {
                Content = "⋯",
                Tag = project.Id,
                Width = 32,
                Height = 36,
                Padding = new Thickness(0),
                Margin = new Thickness(4, 0, 0, 0),
                Background = UiTheme.Brush("SurfaceAltBrush"),
                Foreground = UiTheme.Brush("StatusInkBrush"),
                CornerRadius = new CornerRadius(8)
            };
            AutomationProperties.SetName(actions, $"Project actions: {project.Name}");
            actions.Click += ProjectActionsClick;
            var row = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition { Width = GridLength.Auto } }
            };
            row.Children.Add(button);
            Grid.SetColumn(actions, 1);
            row.Children.Add(actions);
            ProjectList.Children.Add(row);
        }
        if (_projects.Count == 0)
            ProjectList.Children.Add(new TextBlock
            {
                Text = _projectsAvailable ? "Projects you create appear here." : "Project list is unavailable.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = UiTheme.Brush("SubtleInkBrush"),
                FontSize = UiTheme.FontSize("CaptionFontSize"),
                Margin = new Thickness(8, 4, 8, 4)
            });
        RenderProjectFiles();
    }

    private void ProjectActionsClick(object sender, RoutedEventArgs e)
    {
        CloseCompactSidebar();
        if (_requestCancellation is not null)
        {
            RuntimeStatus.Text = "Wait for the response to finish before changing project settings.";
            return;
        }
        if (sender is not Button { Tag: string projectId }) return;
        var project = _projects.FirstOrDefault(item => item.Id == projectId);
        if (project is null) return;
        var menu = new MenuFlyout();
        var edit = new MenuFlyoutItem { Text = "Edit project details" };
        edit.Click += async (_, _) => await EditProjectAsync(projectId);
        var folders = new MenuFlyoutItem { Text = "Manage folders" };
        folders.Click += async (_, _) =>
        {
            if (_activeProjectId != projectId)
            {
                if (!await ConfirmTemporaryDiscardAsync($"open project {project.Name}")) return;
                SaveCurrentSession();
                ClearPendingAttachments();
                _activeProjectId = project.Id;
                _activeProjectName = project.Name;
                _activeChatFolder = null;
                _includeProjectFilesInChat = false;
                _settings.ProjectInstructions = project.Instructions;
                SaveSettings();
                _activeSessionId = null;
                _editingMessageIndex = null;
                _messages.Clear();
                ConversationMessages.Children.Clear();
                WelcomePanel.Visibility = Visibility.Visible;
                ConversationScroll.Visibility = Visibility.Collapsed;
                PageTitle.Text = project.Name;
                ProjectBadge.Visibility = Visibility.Visible;
                UpdateProjectFilesControls();
                UpdateWebSearchButton();
                UpdateTemporaryControls();
                RenderProjects();
            }
            await ProjectFoldersClickAsync();
        };
        var remove = new MenuFlyoutItem { Text = "Remove project" };
        remove.Click += async (_, _) => await RemoveProjectAsync(projectId);
        menu.Items.Add(edit);
        menu.Items.Add(folders);
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(remove);
        menu.ShowAt((FrameworkElement)sender);
    }

    private async Task EditProjectAsync(string projectId)
    {
        if (_requestCancellation is not null)
        {
            RuntimeStatus.Text = "Wait for the response to finish before editing this project.";
            return;
        }
        var project = _projects.FirstOrDefault(item => item.Id == projectId);
        if (project is null) return;
        var name = new TextBox { Header = "Project name", Text = project.Name, MaxLength = 80 };
        var instructions = new TextBox
        {
            Header = "Project instructions",
            Text = project.Instructions,
            AcceptsReturn = true,
            MinHeight = 92,
            MaxHeight = 160,
            TextWrapping = TextWrapping.Wrap,
            PlaceholderText = "How should the assistant work in this project?"
        };
        var panel = new StackPanel { Spacing = 10, Children = { name, instructions } };
        var dialog = new ContentDialog
        {
            Title = "Edit project",
            Content = panel,
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            XamlRoot = XamlRoot
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        var updatedName = name.Text.Trim();
        if (updatedName.Length == 0)
        {
            RuntimeStatus.Text = "A project name cannot be empty.";
            return;
        }
        var oldName = project.Name;
        var oldInstructions = project.Instructions;
        project.Name = updatedName;
        project.Instructions = instructions.Text.Trim();
        if (!SaveProjects())
        {
            project.Name = oldName;
            project.Instructions = oldInstructions;
            return;
        }
        var changedSessions = _sessions.Where(item => item.ProjectId == projectId).ToArray();
        foreach (var session in changedSessions) session.ProjectName = updatedName;
        if (changedSessions.Length > 0 && !PersistConversationEdits())
        {
            project.Name = oldName;
            project.Instructions = oldInstructions;
            foreach (var session in changedSessions) session.ProjectName = oldName;
            SaveProjects();
            return;
        }
        if (_activeProjectId == projectId)
        {
            _activeProjectName = project.Name;
            _settings.ProjectInstructions = project.Instructions;
            SaveSettings();
            PageTitle.Text = project.Name;
        }
        RenderProjects();
        RuntimeStatus.Text = $"Project “{project.Name}” saved.";
    }

    private async Task RemoveProjectAsync(string projectId)
    {
        if (_requestCancellation is not null)
        {
            RuntimeStatus.Text = "Wait for the response to finish before removing this project.";
            return;
        }
        var project = _projects.FirstOrDefault(item => item.Id == projectId);
        if (project is null) return;
        if (_activeProjectId == projectId && !await ConfirmTemporaryDiscardAsync("remove this project")) return;
        var dialog = new ContentDialog
        {
            Title = "Remove project?",
            Content = $"“{project.Name}” will be removed from your project list. Its folders on disk and saved conversations will stay untouched.",
            PrimaryButtonText = "Remove project",
            CloseButtonText = "Cancel",
            XamlRoot = XamlRoot
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        SaveCurrentSession();
        if (_activeProjectId == projectId && _messages.Count > 0 && !_historyAvailable)
        {
            RuntimeStatus.Text = "The active chat could not be saved, so this project was kept. Fix local history and try again.";
            return;
        }
        var index = _projects.IndexOf(project);
        _projects.RemoveAt(index);
        if (!SaveProjects())
        {
            _projects.Insert(index, project);
            return;
        }
        if (_activeProjectId == projectId)
        {
            ClearPendingAttachments();
            _activeProjectId = null;
            _activeProjectName = null;
            _activeChatFolder = null;
            _includeProjectFilesInChat = false;
            _settings.ProjectInstructions = null;
            SaveSettings();
            _activeSessionId = null;
            _editingMessageIndex = null;
            _messages.Clear();
            ConversationMessages.Children.Clear();
            WelcomePanel.Visibility = Visibility.Visible;
            ConversationScroll.Visibility = Visibility.Collapsed;
            PageTitle.Text = "New chat";
            ProjectBadge.Visibility = Visibility.Collapsed;
            UpdateProjectFilesControls();
            UpdateWebSearchButton();
            UpdateTemporaryControls();
        }
        RenderProjects();
        RenderRecent();
        RuntimeStatus.Text = $"Project “{project.Name}” removed. Its saved chats and disk folders remain untouched.";
    }

    private async void ProjectClick(object sender, RoutedEventArgs e) => await RunCompactProjectActionAsync(() => ProjectClickAsync(sender));

    private async Task ProjectClickAsync(object sender)
    {
        if (sender is not Button { Tag: string projectId }) return;
        if (_requestCancellation is not null)
        {
            RuntimeStatus.Text = "Wait for the current response to finish before switching projects.";
            return;
        }
        var project = _projects.FirstOrDefault(item => item.Id == projectId);
        if (project is null || project.Id == _activeProjectId) return;
        if (!await ConfirmTemporaryDiscardAsync($"open project {project.Name}")) return;
        SaveCurrentSession();
        ClearPendingAttachments();
        _activeProjectId = project.Id;
        _activeProjectName = project.Name;
        _activeChatFolder = null;
        _includeProjectFilesInChat = false;
        _settings.ProjectInstructions = project.Instructions;
        SaveSettings();
        _activeSessionId = null;
        _editingMessageIndex = null;
        _messages.Clear();
        ConversationMessages.Children.Clear();
        WelcomePanel.Visibility = Visibility.Visible;
        ConversationScroll.Visibility = Visibility.Collapsed;
        PageTitle.Text = project.Name;
        ProjectBadge.Visibility = Visibility.Visible;
        UpdateProjectFilesControls();
        UpdateWebSearchButton();
        UpdateTemporaryControls();
        RenderProjects();
        PromptBox.Focus(FocusState.Programmatic);
    }

    private async Task<string?> PickWorkspaceFolderAsync()
    {
        try
        {
            var picker = new Windows.Storage.Pickers.FolderPicker
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add("*");
#if WINDOWS || SRAI_DESKTOP
            var window = (Application.Current as App)?.MainWindow;
            if (window is null) throw new InvalidOperationException("The app window is unavailable.");
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(window));
#endif
            var folder = await picker.PickSingleFolderAsync();
            return folder?.Path;
        }
        catch (Exception ex)
        {
            RuntimeStatus.Text = $"Could not open folder picker: {ex.Message}";
            return null;
        }
    }

    private async void ProjectFoldersClick(object sender, RoutedEventArgs e)
    {
        var managesExistingProject = _projects.Any(item => item.Id == _activeProjectId);
        try { await RunUiActionAsync(ProjectFoldersClickAsync); }
        finally
        {
            if (managesExistingProject)
                RestoreNavigationDialogFocus(false, sender as Control);
        }
    }

    private async Task ProjectFoldersClickAsync()
    {
        if (_requestCancellation is not null)
        {
            RuntimeStatus.Text = "Wait for the response to finish before changing project folders.";
            return;
        }
        var project = _projects.FirstOrDefault(item => item.Id == _activeProjectId);
        if (project is null)
        {
            if (_projects.Count == 0) await NewProjectClickAsync();
            else await ChooseProjectAsync();
            return;
        }
        var list = new StackPanel { Spacing = 8 };
        var count = new TextBlock { TextWrapping = TextWrapping.Wrap, Opacity = 0.78 };
        void Refresh()
        {
            list.Children.Clear();
            foreach (var path in project.FolderPaths)
            {
                var remove = new Button { Content = "Remove", Tag = path, VerticalAlignment = VerticalAlignment.Center };
                remove.Click += (_, _) => { project.FolderPaths.Remove(path); Refresh(); };
                list.Children.Add(new Grid
                {
                    ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, new ColumnDefinition { Width = GridLength.Auto } },
                    Children = { new TextBlock { Text = path, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center }, remove }
                });
                Grid.SetColumn(remove, 1);
            }
            count.Text = project.FolderPaths.Count == 0 ? "No folders selected. The Coding Agent will not read workspace files." : "Only files in these selected folders are available as context.";
        }
        Refresh();
        var add = new Button { Content = "＋ Add local folder" };
        add.Click += async (_, _) =>
        {
            var path = await PickWorkspaceFolderAsync();
            if (path is not null && !project.FolderPaths.Contains(path, StringComparer.OrdinalIgnoreCase)) project.FolderPaths.Add(path);
            Refresh();
        };
        var browse = new Button { Content = "Browse files" };
        AutomationProperties.SetName(browse, "Browse project files");
        AutomationProperties.SetHelpText(browse, "Open a read-only nested file list from the selected folders and preview bounded text, image, or PDF files.");
        browse.Click += async (_, _) => await ShowProjectFileBrowserAsync(project);
        var panel = new StackPanel { Spacing = 10, Children = { count, list, add, browse } };
        var dialog = new ContentDialog { Title = $"Folders · {project.Name}", Content = panel, PrimaryButtonText = "Save", CloseButtonText = "Cancel", XamlRoot = XamlRoot };
        var before = project.FolderPaths.ToArray();
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            if (!SaveProjects()) project.FolderPaths = [.. before];
            RenderProjectFiles();
            if (project.FolderPaths.Count == 0)
            {
                _includeProjectFilesInChat = false;
                SaveCurrentSession();
            }
            UpdateProjectFilesControls();
            UpdateWebSearchButton();
            RuntimeStatus.Text = project.FolderPaths.Count == 0 ? "No workspace files selected" : $"{project.FolderPaths.Count} local folder(s) connected";
        }
        else project.FolderPaths = [.. before];
    }

    private async Task ShowProjectFileBrowserAsync(LocalProject project)
    {
        IReadOnlyList<(string Path, string Content)> textFiles;
        IReadOnlyList<WorkspaceFileContext.WorkspaceFileEntry> files;
        try
        {
            textFiles = WorkspaceFileContext.ReadSelectedFiles(project.FolderPaths);
            files = WorkspaceFileContext.EnumeratePreviewableFiles(project.FolderPaths);
        }
        catch (Exception ex)
        {
            RuntimeStatus.Text = $"Project file browser could not read the selected folders: {ex.Message}";
            return;
        }
        var query = new TextBox { Header = "Filter files", PlaceholderText = "relative path" };
        AutomationProperties.SetName(query, "Filter project files");
        var results = new StackPanel { Spacing = 6 };
        AutomationProperties.SetName(results, "Project file browser results");
        void Refresh()
        {
            results.Children.Clear();
            var matches = files.Where(item => string.IsNullOrWhiteSpace(query.Text) || item.RelativePath.Contains(query.Text.Trim(), StringComparison.OrdinalIgnoreCase)).Take(WorkspaceFileContext.MaxPreviewEntries).ToArray();
            results.Children.Add(new TextBlock { Text = $"{matches.Length} bounded file{(matches.Length == 1 ? "" : "s")} shown; nothing was changed.", FontSize = UiTheme.FontSize("CaptionFontSize"), Opacity = 0.72 });
            foreach (var file in matches)
            {
                var preview = new Button { Content = $"{file.Kind} · {file.RelativePath}", HorizontalAlignment = HorizontalAlignment.Left, HorizontalContentAlignment = HorizontalAlignment.Left, Style = (Style?)Application.Current.Resources["ContextButtonStyle"] };
                AutomationProperties.SetName(preview, $"Preview project file {file.RelativePath}");
                preview.Click += async (_, _) =>
                {
                    UIElement body;
                    if (file.Kind == "text")
                    {
                        var content = textFiles.FirstOrDefault(item => string.Equals(item.Path, file.RelativePath, StringComparison.OrdinalIgnoreCase)).Content ?? "The text preview is unavailable.";
                        body = new TextBlock { Text = content, TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true };
                    }
                    else if (file.Kind == "image")
                    {
                        try
                        {
                            var bytes = WorkspaceFileEditor.ReadBytesBounded(file.FullPath, (int)ChatAttachmentPolicy.MaxFileBytes);
                            var mediaType = ChatAttachmentPolicy.GetImageMediaType(file.RelativePath);
                            if (!ChatAttachmentPolicy.HasValidImageSignature(mediaType, bytes)) throw new InvalidDataException("The image signature is not valid.");
                            var bitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                            using var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                            using var output = stream.GetOutputStreamAt(0);
                            using var writer = new Windows.Storage.Streams.DataWriter(output);
                            writer.WriteBytes(bytes);
                            await writer.StoreAsync();
                            await writer.FlushAsync();
                            stream.Seek(0);
                            await bitmap.SetSourceAsync(stream);
                            body = new Image { Source = bitmap, Stretch = Stretch.Uniform, MaxWidth = 720, MaxHeight = 480 };
                        }
                        catch (Exception error) { body = new TextBlock { Text = $"Image preview unavailable: {error.Message}", TextWrapping = TextWrapping.Wrap }; }
                    }
                    else
                    {
                        try
                        {
                            var bytes = WorkspaceFileEditor.ReadBytesBounded(file.FullPath, (int)ChatAttachmentPolicy.MaxPdfBytes);
                            body = new TextBlock { Text = PdfAttachmentTextExtractor.Extract(bytes, 12_000), TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true };
                        }
                        catch (Exception error) { body = new TextBlock { Text = $"PDF text preview unavailable: {error.Message}", TextWrapping = TextWrapping.Wrap }; }
                    }
                    var previewDialog = new ContentDialog
                    {
                        Title = $"Preview · {file.RelativePath}",
                        Content = new ScrollViewer { Content = body, MaxHeight = 560, VerticalScrollBarVisibility = ScrollBarVisibility.Auto },
                        CloseButtonText = "Done",
                        XamlRoot = XamlRoot
                    };
                    AutomationProperties.SetName(previewDialog, $"Preview project file {file.RelativePath}");
                    await previewDialog.ShowAsync();
                };
                results.Children.Add(preview);
            }
        }
        query.TextChanged += (_, _) => Refresh();
        Refresh();
        var dialog = new ContentDialog
        {
            Title = $"Project files · {project.Name}",
            Content = new ScrollViewer { Content = new StackPanel { Spacing = 8, Children = { query, results } }, MaxHeight = 620, VerticalScrollBarVisibility = ScrollBarVisibility.Auto },
            CloseButtonText = "Done",
            XamlRoot = XamlRoot
        };
        AutomationProperties.SetName(dialog, "Browse project files");
        dialog.Opened += (_, _) => DispatcherQueue.TryEnqueue(() => query.Focus(FocusState.Programmatic));
        await dialog.ShowAsync();
    }

    private bool SaveProjects()
    {
        if (!_projectsAvailable)
        {
            RuntimeStatus.Text = "Projects cannot be changed because the saved project list could not be read.";
            return false;
        }
        try
        {
            _projectStore.Save(_projects);
            return true;
        }
        catch (Exception ex)
        {
            _projectsAvailable = false;
            RuntimeStatus.Text = $"Project could not be saved locally: {ex.Message}";
            return false;
        }
    }

    private async Task ChooseProjectAsync()
    {
        if (_projects.Count == 0) { RuntimeStatus.Text = "Create a project first."; return; }
        var choices = _projects.Select(item => item.Name).ToArray();
        var picker = new ListView { ItemsSource = choices, SelectionMode = ListViewSelectionMode.Single, MinWidth = 300 };
        var dialog = new ContentDialog { Title = "Choose a project", Content = picker, PrimaryButtonText = "Open project", CloseButtonText = "Cancel", XamlRoot = XamlRoot };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary || picker.SelectedItem is not string name) return;
        var project = _projects.FirstOrDefault(item => item.Name == name);
        if (project is not null) ProjectClick(new Button { Tag = project.Id }, new RoutedEventArgs());
    }
}
