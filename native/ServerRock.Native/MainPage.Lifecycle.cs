// MainPage lifecycle and startup wiring. The partial keeps event subscription,
// local-store recovery, timers, and initial rendering together while feature
// files remain responsible for their own actions.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using ServerRock.Core.Models;
using ServerRock.Core.Services;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    public MainPage()
    {
        InitializeComponent();
        AutomationProperties.SetName(BackButton, "Navigate back");
        AutomationProperties.SetHelpText(BackButton, "Return to the previous workspace route.");
        AutomationProperties.SetName(ForwardButton, "Navigate forward");
        AutomationProperties.SetHelpText(ForwardButton, "Return to the next workspace route.");
        UpdateNavigationControls();
        _workflowRunService = new WorkflowRunService(_workflowService);
        MainPageHeaderControl.ToggleSidebarRequested += ToggleSidebarClick;
        MainPageHeaderControl.BackRequested += NavigateBackClick;
        MainPageHeaderControl.ForwardRequested += NavigateForwardClick;
        MainPageHeaderControl.SettingsRequested += SettingsClick;
        MainPageHeaderControl.NewChatRequested += NewChatClick;
        MainPageHeaderControl.PreviousBranchRequested += PreviousBranchClick;
        MainPageHeaderControl.NextBranchRequested += NextBranchClick;
        MainPageHeaderControl.SplitChatRequested += SplitChatClick;
        SplitConversationViewControl.ComparisonChanged += SplitComparisonChanged;
        SplitConversationViewControl.RatioChanged += SplitRatioChanged;
        CodingWorkspaceBarControl.ToolbarResized += CodingWorkspaceToolbarSizeChanged;
        CodingWorkspaceBarControl.ProjectFoldersRequested += ProjectFoldersClick;
        CodingWorkspaceBarControl.GitHubActionsRequested += GitHubActionsClick;
        CodingWorkspaceBarControl.TerminalRequested += OpenTerminalClick;
        CodingWorkspaceBarControl.UndoHistoryRequested += UndoHistoryClick;
        ConversationSurfaceControl.SettingsRequested += SettingsClick;
        ConversationSurfaceControl.SuggestionRequested += SuggestionClick;
        ComposerToolbarControl.ToolbarResized += ComposerToolbarSizeChanged;
        ComposerToolbarControl.PromptTextChangedRequested += PromptTextChanged;
        ComposerToolbarControl.ManageQueueRequested += ManageQueueClick;
        ComposerToolbarControl.ModelRequested += ModelHotswapClick;
        ComposerToolbarControl.ToolsRequested += ComposerToolsClick;
        ComposerToolbarControl.WebSearchRequested += WebSearchClick;
        ComposerToolbarControl.TemporaryRequested += TemporaryClick;
        ComposerToolbarControl.ProjectFilesRequested += ProjectFilesClick;
        ComposerToolbarControl.CancelEditRequested += CancelEditClick;
        ComposerToolbarControl.SmartRewriteRequested += SmartRewriteClick;
        ComposerToolbarControl.DocumentsOnlyRequested += DocumentsOnlyClick;
        ComposerToolbarControl.ImageStudioRequested += ImageStudioClick;
        ComposerToolbarControl.AdvancedRequested += AdvancedClick;
        ComposerToolbarControl.AttachPathRequested += AttachLocalPathClick;
        // Sidebar clicks arrive as SidebarView events (see MainPage.Sidebar.cs).
        SidebarView.ChatModeRequested += ChatModeClick;
        SidebarView.CodingModeRequested += CodingModeClick;
        SidebarView.NewChatRequested += NewChatClick;
        SidebarView.NewProjectRequested += NewProjectClick;
        SidebarView.SearchRequested += SearchClick;
        SidebarView.ModelsRequested += ModelClick;
        SidebarView.SettingsRequested += SettingsClick;
        SidebarView.PluginsRequested += PluginStoreClick;
        SidebarView.GitHubRequested += GitHubClick;
        SidebarView.NewChatFolderRequested += NewChatFolderClick;
        SidebarView.ImportChatsRequested += ImportChatsClick;
        SidebarView.ExportChatsRequested += ExportChatsClick;
        SidebarView.ExportMarkdownFolderRequested += ExportMarkdownFolderClick;
        SidebarView.ToggleCompanionRequested += ToggleCompanionClick;
        SidebarView.CustomizeCompanionRequested += CustomizeCompanionClick;
        SidebarView.ScheduledTasksRequested += ScheduledTasksClick;
        SidebarView.ImagesRequested += ImageStudioClick;
        SidebarView.MoreRequested += MoreClick;
        SidebarView.UpdateRequested += UpdateClick;
        if (string.Equals(Environment.GetEnvironmentVariable("SERVER_ROCK_UPDATE_AVAILABLE"), "1", StringComparison.Ordinal))
            SidebarView.SetUpdateAvailable(true);
        Canvas.SetZIndex(SidebarBackdrop, 1);
        Canvas.SetZIndex(Sidebar, 2);
        ApplyResponsiveNavigation(AppRoot.ActualWidth);
        _runtimeStatusTimer.Tick += (_, _) =>
        {
            _runtimeStatusTimer.Stop();
            RuntimeStatus.Text = string.Empty;
        };
        _scheduledTaskTimer.Tick += async (_, _) => await ProcessDueScheduledTaskAsync();
        _scheduledTaskTimer.Start();
        RuntimeStatus.RegisterPropertyChangedCallback(TextBlock.TextProperty, (_, _) =>
        {
            var message = RuntimeStatus.Text;
            RuntimeStatusBanner.Visibility = string.IsNullOrWhiteSpace(message) ? Visibility.Collapsed : Visibility.Visible;
            StatusHistoryButton.Visibility = _statusHistory.Snapshot().Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            _runtimeStatusTimer.Stop();
            if (!string.IsNullOrWhiteSpace(message) && IsActionableRuntimeStatus(message)) _statusHistory.Record(message);
            StatusHistoryButton.Visibility = _statusHistory.Snapshot().Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            if (!string.IsNullOrWhiteSpace(message) && !IsActionableRuntimeStatus(message)) _runtimeStatusTimer.Start();
        });
        PromptBox.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(PromptKeyDown), handledEventsToo: true);
        _settings = _settingsStore.Load();
        _workspaceDrafts = new WorkspaceDraftStore(_settings.WorkspaceDrafts);
        _restoringWorkspaceDraft = true;
        PromptBox.Text = _workspaceDrafts.Get(_mode);
        _restoringWorkspaceDraft = false;
        _draftPersistenceTimer.Tick += (_, _) =>
        {
            _draftPersistenceTimer.Stop();
            PersistWorkspaceDrafts();
        };
        _navigationHistory.Restore(_settings.WorkspaceRouteHistory, _mode);
        UpdateNavigationControls();
        var legacyProjectInstructions = _settings.ProjectInstructions;
        var warnings = new List<string>();
        try
        {
            RestorePendingAttachments();
        }
        catch (Exception ex)
        {
            warnings.Add($"Pending attachment drafts could not be restored; the saved draft file was left untouched: {ex.Message}");
        }
        try
        {
            foreach (var queued in _messageQueueStore.Load()) _messageQueue.Enqueue(queued.Text, queued.Attachments);
            if (_messageQueue.Count > 0) warnings.Add($"Restored {_messageQueue.Count} queued message{(_messageQueue.Count == 1 ? "" : "s")}; review the Queue control before sending.");
        }
        catch (Exception ex)
        {
            warnings.Add($"Queued messages could not be restored; the saved queue was left untouched: {ex.Message}");
        }
        try
        {
            var reconciled = _operationService.ReconcileAfterRestart();
            if (reconciled.Count > 0)
                warnings.Add($"{reconciled.Count} interrupted operation{(reconciled.Count == 1 ? "" : "s")} need review in Activity & tasks.");
        }
        catch (Exception ex)
        {
            warnings.Add($"Operation history could not be reconciled: {ex.Message}");
        }
        try { _companionState = _companionStore.Load(); }
        catch (Exception ex)
        {
            _companionPersistenceAvailable = false;
            warnings.Add($"Pet preferences could not be read and were left untouched: {ex.Message}");
        }
        try
        {
            App.Log.Info("Startup", $"Loading local history mode={_mode} model={_settings.Model} endpoint={_settings.Endpoint}");
            _sessions = _conversationStore.Load();
        }
        catch (Exception ex)
        {
            _historyAvailable = false;
            warnings.Add($"Could not load local conversation history: {ex.Message}");
        }
        try
        {
            _projects = _projectStore.Load();
        }
        catch (Exception ex)
        {
            _projectsAvailable = false;
            warnings.Add($"Could not load local projects: {ex.Message}");
        }
        try
        {
            _workspaceUndoEntries = _workspaceUndoStore.Load();
            foreach (var entry in _workspaceUndoEntries) _persistedUndoIds.Add(entry.Id);
        }
        catch (Exception ex)
        {
            _undoHistoryAvailable = false;
            warnings.Add($"Could not load project undo history: {ex.Message}");
        }
        if (_projectsAvailable && _projects.Count == 0 && !string.IsNullOrWhiteSpace(legacyProjectInstructions))
        {
            var previousName = string.IsNullOrWhiteSpace(_settings.ProjectPath)
                ? "Previous project"
                : Path.GetFileName(_settings.ProjectPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) ?? "Previous project";
            var previousProject = new LocalProject { Name = previousName, Instructions = legacyProjectInstructions };
            _projects.Add(previousProject);
            try
            {
                _projectStore.Save(_projects);
            }
            catch (Exception ex)
            {
                _projects.Remove(previousProject);
                _projectsAvailable = false;
                warnings.Add($"Could not migrate previous project instructions: {ex.Message}");
            }
        }
        _settings.ProjectInstructions = null;
        RenderProjects();
        RenderRecent();
        UpdateCompanionDisplay();
        UpdateModeControls();
        UpdateModelLabel();
        UpdateWebSearchButton();
        UpdateSmartRewriteButton();
        UpdateTemporaryControls();
        UpdateQueueControl();
        RenderPendingAttachments();
        ApplySavedCustomization();
        RuntimeStatus.Text = string.Join(" · ", warnings);
    }
}
