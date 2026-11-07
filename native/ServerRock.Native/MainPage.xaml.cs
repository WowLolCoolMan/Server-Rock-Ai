// MainPage – the app's only page. This file holds the shared pieces every feature uses:
// the page state (fields), the Core services the page talks to, the constructor (loads
// settings, history, projects, and undo history, then renders the sidebar), the status
// banner, RunUiActionAsync (error wrapper for async click handlers), and SaveSettings.
//
// Each feature lives in its own partial file, MainPage.<Feature>.cs (Chat, Messages,
// ModelLibrary, Settings, Projects, Recents, ChatFolders, SlashCommands, CodingAgent, …).
// See docs/ARCHITECTURE.md ("Where do I find…?") for the full map.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Input;
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
    private string _openRouterApiKey = string.Empty;
    private readonly ModelLibraryService _modelLibrary = new();
    private readonly ComfyImageProvider _comfyImageProvider = new();
    private readonly WebSearchProvider _webSearchProvider = new();
    private readonly JsonSettingsStore _settingsStore = new();
    private readonly ConversationStore _conversationStore = new(Environment.GetEnvironmentVariable("SRAI_CONVERSATIONS_PATH"));
    private readonly ProjectStore _projectStore = new();
    private readonly WorkspaceFileEditor _workspaceEditor = new();
    private readonly WorkspaceUndoStore _workspaceUndoStore = new();
    private readonly WorkspaceCommandRunner _commandRunner = new();
    private readonly CompanionStateStore _companionStore = new();
    private readonly HashSet<string> _remoteProjectConsent = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _remoteAttachmentConsent = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ChatAttachment> _pendingAttachments = [];
    private List<ChatSession> _sessions = [];
    private bool _archivedChatsExpanded;
    private List<LocalProject> _projects = [];
    private List<WorkspaceUndoEntry> _workspaceUndoEntries = [];
    private readonly HashSet<string> _persistedUndoIds = new(StringComparer.Ordinal);
    private AppSettings _settings;
    private CompanionState _companionState = new();
    private string _mode = "ChatBot";
    private string? _project;
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
    private bool _compactNavigation;
    private bool _compactComposerToolbar;
    private bool _compactCodingWorkspaceToolbar;
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
    private bool _connectionCheckRunning;
    private int _selectedSlashSuggestion = -1;
    private Button? _lastUndoButton;
    private WorkspaceEditReceipt? _lastEditReceipt;
    private CancellationTokenSource? _requestCancellation;
    private CancellationTokenSource? _modelBenchmarkCancellation;
#if WINDOWS
    private Windows.Media.Playback.MediaPlayer? _voicePlayer;
#endif
    private readonly DispatcherTimer _runtimeStatusTimer = new() { Interval = TimeSpan.FromSeconds(7) };

    public MainPage()
    {
        InitializeComponent();
        Canvas.SetZIndex(SidebarBackdrop, 1);
        Canvas.SetZIndex(Sidebar, 2);
        ApplyResponsiveNavigation(AppRoot.ActualWidth);
        _runtimeStatusTimer.Tick += (_, _) =>
        {
            _runtimeStatusTimer.Stop();
            RuntimeStatus.Text = string.Empty;
        };
        RuntimeStatus.RegisterPropertyChangedCallback(TextBlock.TextProperty, (_, _) =>
        {
            var message = RuntimeStatus.Text;
            RuntimeStatusBanner.Visibility = string.IsNullOrWhiteSpace(message) ? Visibility.Collapsed : Visibility.Visible;
            _runtimeStatusTimer.Stop();
            if (!string.IsNullOrWhiteSpace(message) && !IsActionableRuntimeStatus(message)) _runtimeStatusTimer.Start();
        });
        PromptBox.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(PromptKeyDown), handledEventsToo: true);
        _settings = _settingsStore.Load();
        var legacyProjectInstructions = _settings.ProjectInstructions;
        var warnings = new List<string>();
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
        ApplySavedCustomization();
        RuntimeStatus.Text = string.Join(" · ", warnings);
    }

    private void DismissRuntimeStatusClick(object sender, RoutedEventArgs e) => RuntimeStatus.Text = string.Empty;

    private static bool IsActionableRuntimeStatus(string message) =>
        new[] { "could not", "cannot", "failed", "error", "unavailable", "wait for", "timed out", "timeout", "expired", "no readable", "no results", "not available", "not saved", "refused", "rejected", "requires", "enter ", "choose ", "add a project", "not connected", "check settings", "not enabled", "stopped", "preserve newer", "protected" }
            .Any(marker => message.Contains(marker, StringComparison.OrdinalIgnoreCase));

    private async Task RunUiActionAsync(Func<Task> action)
    {
        try { await action(); }
        catch (Exception ex)
        {
            App.Log.Error("UI", ex, "UI action failed");
            RuntimeStatus.Text = $"Action failed: {ex.Message}";
        }
    }

    private bool SaveSettings()
    {
        try
        {
            _settingsStore.Save(_settings);
            return true;
        }
        catch (Exception ex)
        {
            RuntimeStatus.Text = $"Could not save settings: {ex.Message}";
            return false;
        }
    }
}
