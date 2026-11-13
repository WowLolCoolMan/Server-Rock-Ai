// MainPage – the app's only page. This file holds the shared page state and Core services.
// Lifecycle wiring is in MainPage.Lifecycle.cs; cross-cutting UI infrastructure is in
// MainPage.Infrastructure.cs.
//
// Each feature lives in its own partial file, MainPage.<Feature>.cs (Chat, Messages,
// ModelLibrary, Settings, Projects, Recents, ChatFolders, SlashCommands, CodingAgent, …).
// See docs/ARCHITECTURE.md ("Where do I find…?") for the full map.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using ServerRock.Core.Models;
using ServerRock.Core.Services;
using ServerRock.Core.Infrastructure;

namespace ServerRock.Native;

public sealed partial class MainPage : Page
{
    private readonly List<ChatMessage> _messages = [];
    private readonly OpenAiCompatibleProvider _provider = new();
    // OpenRouter keys are session-only; they are never written to settings.json.
    private string _providerApiKey = string.Empty;
    private readonly ModelLibraryService _modelLibrary = new();
    private readonly ComfyImageProvider _comfyImageProvider = new();
    private readonly WebSearchProvider _webSearchProvider = new();
    // A bounded explicit path is useful for disposable release/accessibility
    // journeys; without the variable, settings stay in the normal user profile.
    private readonly JsonSettingsStore _settingsStore = new(Environment.GetEnvironmentVariable("SRAI_SETTINGS_PATH"));
    private readonly ConversationStore _conversationStore = new(Environment.GetEnvironmentVariable("SRAI_CONVERSATIONS_PATH"));
    private readonly ProjectStore _projectStore = new();
    private readonly WorkspaceFileEditor _workspaceEditor = new();
    private readonly WorkspaceUndoStore _workspaceUndoStore = new();
    private readonly WorkspaceCommandRunner _commandRunner = new();
    private readonly OperationService _operationService = new();
    private readonly WorkflowService _workflowService = new();
    private readonly WorkflowRunService _workflowRunService;
    private readonly LocalProjectHealthAnalyzer _localHealthAnalyzer = new();
    private readonly CompanionStateStore _companionStore = new();
    private readonly HashSet<string> _remoteProjectConsent = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _remoteAttachmentConsent = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _remoteModelComparisonConsent = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ChatAttachment> _pendingAttachments = [];
    private readonly WorkspaceAttachmentDraftStore _attachmentDraftStore = new(Environment.GetEnvironmentVariable("SRAI_ATTACHMENT_DRAFTS_PATH"));
    private readonly ChatMessageQueue _messageQueue = new();
    private readonly ChatMessageQueueStore _messageQueueStore = new();
    private WorkspaceDraftStore _workspaceDrafts = new();
    private readonly ScheduledTaskStore _scheduledTaskStore = new();
    private readonly AgentTaskArtifactStore _agentTaskStore = new();
    private readonly AgentTaskExecutor _agentTaskExecutor;
    private readonly GitWorktreeCoordinator _gitWorktreeCoordinator = new();
    private readonly SkillPackageInstaller _skillPackageInstaller = new();
    // Keep the normal profile as the default; an explicit path supports
    // disposable live acceptance without reading or writing real memory.
    private readonly MemoryStore _memoryStore = new(Environment.GetEnvironmentVariable("SRAI_MEMORY_PATH"));
    private bool _restoringWorkspaceDraft;
    private List<ChatSession> _sessions = [];
    private bool _archivedChatsExpanded;
    private List<LocalProject> _projects = [];
    private List<WorkspaceUndoEntry> _workspaceUndoEntries = [];
    private readonly HashSet<string> _persistedUndoIds = new(StringComparer.Ordinal);
    private AppSettings _settings;
    private CompanionState _companionState = new();
    private string _mode = WorkspaceModes.Chat;
    private string? _activeProjectName;
    private string? _activeProjectId;
    private string? _activeSessionId;
    private string? _activeChatFolder;
    private int? _editingMessageIndex;
    private readonly HashSet<string> _collapsedChatFolders = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Button> _recentActionButtons = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Button> _chatFolderActionButtons = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Button> _chatFolderToggleButtons = new(StringComparer.OrdinalIgnoreCase);
    private Button? _archivedChatsToggleButton;
    private bool _isTemporaryChat;
    private bool _temporaryTransitionInProgress;
    private bool _includeProjectFilesInChat;
    private bool _documentsOnlyMode;
    private bool _compactNavigation;
    private bool _companionOverlayBlocked;
    private bool _compactComposerToolbar;
    private bool _compactCodingWorkspaceToolbar;
    private string? _openProjectFileProjectId;
    private readonly List<string> _openProjectFilePaths = [];
    private bool _companionPersistenceAvailable = true;
    private bool _isCompanionDragging;
    private Point _companionDragOffset;
    private bool _historyAvailable = true;
    private bool _projectsAvailable = true;
    private bool _undoHistoryAvailable = true;
    private int _lastContextFileCount;
    private string? _pendingWebContext;
    private List<ChatSource>? _pendingWebSources;
    private bool _providerConnected;
    private string[] _advertisedModels = [];
    private bool _connectionCheckRunning;
    private int _selectedSlashSuggestion = -1;
    private Button? _lastUndoButton;
    private WorkspaceEditReceipt? _lastEditReceipt;
    private CancellationTokenSource? _requestCancellation;
    private OperationRun? _activeOperationRun;
    private CancellationTokenSource? _modelBenchmarkCancellation;
#if WINDOWS
    private Windows.Media.Playback.MediaPlayer? _voicePlayer;
#endif
    private readonly DispatcherTimer _runtimeStatusTimer = new() { Interval = TimeSpan.FromSeconds(7) };
    private readonly DispatcherTimer _scheduledTaskTimer = new() { Interval = TimeSpan.FromSeconds(20) };
    private readonly DispatcherTimer _draftPersistenceTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private readonly StatusMessageHistory _statusHistory = new();
    private readonly NavigationHistory _navigationHistory = new(WorkspaceModes.Chat);
    private bool _restoringNavigation;

    // Header controls remain available under their original names while the
    // visual header is owned by MainPageHeader.
    private Grid Header => MainPageHeaderControl.HeaderControl;
    private Button ToggleSidebarButton => MainPageHeaderControl.ToggleSidebarButtonControl;
    private Button BackButton => MainPageHeaderControl.BackButtonControl;
    private Button ForwardButton => MainPageHeaderControl.ForwardButtonControl;
    private TextBlock PageTitle => MainPageHeaderControl.PageTitleControl;
    private Border ProjectBadge => MainPageHeaderControl.ProjectBadgeControl;
    private TextBlock ProjectBadgeText => MainPageHeaderControl.ProjectBadgeTextControl;
    private Border ChatFolderBadge => MainPageHeaderControl.ChatFolderBadgeControl;
    private TextBlock ChatFolderBadgeText => MainPageHeaderControl.ChatFolderBadgeTextControl;
    private Border TemporaryBadge => MainPageHeaderControl.TemporaryBadgeControl;
    private StackPanel BranchControls => MainPageHeaderControl.BranchControlsControl;
    private Button PreviousBranchButton => MainPageHeaderControl.PreviousBranchButtonControl;
    private TextBlock BranchPositionText => MainPageHeaderControl.BranchPositionTextControl;
    private Button NextBranchButton => MainPageHeaderControl.NextBranchButtonControl;
    private Ellipse HeaderConnectionDot => MainPageHeaderControl.HeaderConnectionDotControl;
    private TextBlock HeaderConnectionLabel => MainPageHeaderControl.HeaderConnectionLabelControl;
    private Border CodingWorkspaceBar => CodingWorkspaceBarControl.BarControl;
    private Grid CodingWorkspaceToolbar => CodingWorkspaceBarControl.ToolbarControl;
    private StackPanel CodingWorkspaceSummary => CodingWorkspaceBarControl.SummaryControl;
    private Grid CodingWorkspaceActions => CodingWorkspaceBarControl.ActionsControl;
    private Grid CodingWorkspaceProjectActions => CodingWorkspaceBarControl.ProjectActionsControl;
    private Grid CodingWorkspaceTools => CodingWorkspaceBarControl.ToolsControl;
    private TextBlock WorkspaceProjectName => CodingWorkspaceBarControl.WorkspaceProjectNameControl;
    private Border WorkspaceFolderBadge => CodingWorkspaceBarControl.WorkspaceFolderBadgeControl;
    private TextBlock WorkspaceFolderCount => CodingWorkspaceBarControl.WorkspaceFolderCountControl;
    private TextBlock WorkspaceProjectPath => CodingWorkspaceBarControl.WorkspaceProjectPathControl;
    private TextBlock WorkspaceSessionState => CodingWorkspaceBarControl.WorkspaceSessionStateControl;
    private Button ProjectFoldersButton => CodingWorkspaceBarControl.ProjectFoldersButtonControl;
    private Button GitHubActionsButton => CodingWorkspaceBarControl.GitHubActionsButtonControl;
    private Button TerminalButton => CodingWorkspaceBarControl.TerminalButtonControl;
    private Button ResumeFileButton => CodingWorkspaceBarControl.ResumeFileButtonControl;
    private StackPanel OpenFileTabs => CodingWorkspaceBarControl.OpenFileTabsControl;
    private ScrollViewer OpenFileTabsScroll => CodingWorkspaceBarControl.OpenFileTabsScrollControl;
    private Button UndoHistoryButton => CodingWorkspaceBarControl.UndoHistoryButtonControl;
    private StackPanel WelcomePanel => ConversationSurfaceControl.WelcomePanelControl;
    private TextBlock WelcomeTitle => ConversationSurfaceControl.WelcomeTitleControl;
    private TextBlock WelcomeSubtitle => ConversationSurfaceControl.WelcomeSubtitleControl;
    private Button ConnectionSetupButton => ConversationSurfaceControl.ConnectionSetupButtonControl;
    private Button SuggestionOne => ConversationSurfaceControl.SuggestionOneControl;
    private Button SuggestionTwo => ConversationSurfaceControl.SuggestionTwoControl;
    private Button SuggestionThree => ConversationSurfaceControl.SuggestionThreeControl;
    private ScrollViewer ConversationScroll => ConversationSurfaceControl.ConversationScrollControl;
    private StackPanel ConversationMessages => ConversationSurfaceControl.ConversationMessagesControl;
    private Grid ComposerToolbar => ComposerToolbarControl.ToolbarControl;
    private Grid PrimaryComposerToolbar => ComposerToolbarControl.PrimaryToolbarControl;
    private ScrollViewer SecondaryComposerToolbar => ComposerToolbarControl.SecondaryToolbarControl;
    private Button ModelButton => ComposerToolbarControl.ModelButtonControl;
    private Button ComposerToolsButton => ComposerToolbarControl.ComposerToolsButtonControl;
    private Button WebSearchButton => ComposerToolbarControl.WebSearchButtonControl;
    private Button TemporaryButton => ComposerToolbarControl.TemporaryButtonControl;
    private Button ProjectFilesButton => ComposerToolbarControl.ProjectFilesButtonControl;
    private Button CancelEditButton => ComposerToolbarControl.CancelEditButtonControl;
    private Button SmartRewriteButton => ComposerToolbarControl.SmartRewriteButtonControl;
    private Button DocumentsOnlyButton => ComposerToolbarControl.DocumentsOnlyButtonControl;
    private StackPanel AttachmentList => ComposerToolbarControl.AttachmentListControl;
    private Border RuntimeStatusBanner => ComposerToolbarControl.RuntimeStatusBannerControl;
    private TextBlock RuntimeStatus => ComposerToolbarControl.RuntimeStatusControl;
    private Button ManageQueueButton => ComposerToolbarControl.ManageQueueButtonControl;
    private Button StatusHistoryButton => ComposerToolbarControl.StatusHistoryButtonControl;
    private Border CommandHintPanel => ComposerToolbarControl.CommandHintPanelControl;
    private StackPanel CommandSuggestions => ComposerToolbarControl.CommandSuggestionsControl;
    private Border ComposerBorder => ComposerToolbarControl.ComposerBorderControl;
    private TextBox PromptBox => ComposerToolbarControl.PromptBoxControl;
    private Button VoiceButton => ComposerToolbarControl.VoiceButtonControl;
    private FontIcon VoiceButtonIcon => ComposerToolbarControl.VoiceButtonIconControl;
    private Button VoiceOutputButton => ComposerToolbarControl.VoiceOutputButtonControl;
    private FontIcon VoiceOutputIcon => ComposerToolbarControl.VoiceOutputIconControl;
    private TextBlock ProcessingLabel => ComposerToolbarControl.ProcessingLabelControl;
    private Button StopButton => ComposerToolbarControl.StopButtonControl;
    private Button SendButton => ComposerToolbarControl.SendButtonControl;
    private TextBlock PrivacyFootnote => ComposerToolbarControl.PrivacyFootnoteControl;
    private Button SplitChatButton => MainPageHeaderControl.SplitChatButtonControl;
    private bool _splitChatVisible;
    private string? _splitComparisonSessionId;
    private SplitConversationView SplitConversationView => SplitConversationViewControl;
    private string SplitLayoutKey => $"{_mode}:{_activeProjectId ?? _activeProjectName ?? "default"}";

}
