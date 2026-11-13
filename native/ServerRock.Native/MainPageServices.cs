// Composition root for MainPage's UI-independent services.
// The page remains the current coordinator, but construction is centralized so
// later view-model/application-service extraction does not duplicate stores or
// accidentally change profile paths.

using SkaiDesktop.Core.Services;
using SkaiDesktop.Core.Infrastructure;

namespace ServerRock.Native;

internal sealed class MainPageServices
{
    public OpenAiCompatibleProvider Provider { get; }
    public AirLlmProvider AirLlmProvider { get; }
    public ModelLibraryService ModelLibrary { get; }
    public ComfyImageProvider ComfyImageProvider { get; }
    public WebSearchProvider WebSearchProvider { get; }
    public JsonSettingsStore SettingsStore { get; }
    public ConversationStore ConversationStore { get; }
    public ProjectStore ProjectStore { get; }
    public WorkspaceFileEditor WorkspaceEditor { get; }
    public WorkspaceUndoStore WorkspaceUndoStore { get; }
    public WorkspaceCommandRunner CommandRunner { get; }
    public OperationService OperationService { get; }
    public WorkflowService WorkflowService { get; }
    public WorkflowRunService WorkflowRunService { get; }
    public LocalProjectHealthAnalyzer LocalHealthAnalyzer { get; }
    public CompanionStateStore CompanionStore { get; }
    public WorkspaceAttachmentDraftStore AttachmentDraftStore { get; }
    public ChatMessageQueueStore MessageQueueStore { get; }
    public ScheduledTaskStore ScheduledTaskStore { get; }
    public AgentTaskArtifactStore AgentTaskStore { get; }
    public AgentTaskExecutor AgentTaskExecutor { get; }
    public StartupRecoveryService StartupRecovery { get; }
    public GitWorktreeCoordinator GitWorktreeCoordinator { get; }
    public SkillPackageInstaller SkillPackageInstaller { get; }
    public MemoryStore MemoryStore { get; }
    public NativeTtsService NativeTts { get; }

    private MainPageServices(
        OpenAiCompatibleProvider provider,
        AirLlmProvider airLlmProvider,
        ModelLibraryService modelLibrary,
        ComfyImageProvider comfyImageProvider,
        WebSearchProvider webSearchProvider,
        JsonSettingsStore settingsStore,
        ConversationStore conversationStore,
        ProjectStore projectStore,
        WorkspaceFileEditor workspaceEditor,
        WorkspaceUndoStore workspaceUndoStore,
        WorkspaceCommandRunner commandRunner,
        OperationService operationService,
        WorkflowService workflowService,
        WorkflowRunService workflowRunService,
        LocalProjectHealthAnalyzer localHealthAnalyzer,
        CompanionStateStore companionStore,
        WorkspaceAttachmentDraftStore attachmentDraftStore,
        ChatMessageQueueStore messageQueueStore,
        ScheduledTaskStore scheduledTaskStore,
        AgentTaskArtifactStore agentTaskStore,
        AgentTaskExecutor agentTaskExecutor,
        StartupRecoveryService startupRecovery,
        GitWorktreeCoordinator gitWorktreeCoordinator,
        SkillPackageInstaller skillPackageInstaller,
        MemoryStore memoryStore,
        NativeTtsService nativeTts)
    {
        Provider = provider;
        AirLlmProvider = airLlmProvider;
        ModelLibrary = modelLibrary;
        ComfyImageProvider = comfyImageProvider;
        WebSearchProvider = webSearchProvider;
        SettingsStore = settingsStore;
        ConversationStore = conversationStore;
        ProjectStore = projectStore;
        WorkspaceEditor = workspaceEditor;
        WorkspaceUndoStore = workspaceUndoStore;
        CommandRunner = commandRunner;
        OperationService = operationService;
        WorkflowService = workflowService;
        WorkflowRunService = workflowRunService;
        LocalHealthAnalyzer = localHealthAnalyzer;
        CompanionStore = companionStore;
        AttachmentDraftStore = attachmentDraftStore;
        MessageQueueStore = messageQueueStore;
        ScheduledTaskStore = scheduledTaskStore;
        AgentTaskStore = agentTaskStore;
        AgentTaskExecutor = agentTaskExecutor;
        StartupRecovery = startupRecovery;
        GitWorktreeCoordinator = gitWorktreeCoordinator;
        SkillPackageInstaller = skillPackageInstaller;
        MemoryStore = memoryStore;
        NativeTts = nativeTts;
    }

    public static MainPageServices Create()
    {
        var provider = new OpenAiCompatibleProvider();
        var airLlmProvider = new AirLlmProvider();
        var modelLibrary = new ModelLibraryService();
        var comfyImageProvider = new ComfyImageProvider();
        var webSearchProvider = new WebSearchProvider();
        var settingsStore = new JsonSettingsStore(Environment.GetEnvironmentVariable("SRAI_SETTINGS_PATH"));
        var conversationStore = new ConversationStore(Environment.GetEnvironmentVariable("SRAI_CONVERSATIONS_PATH"));
        var projectStore = new ProjectStore();
        var workspaceEditor = new WorkspaceFileEditor();
        var workspaceUndoStore = new WorkspaceUndoStore();
        var commandRunner = new WorkspaceCommandRunner();
        var operationService = new OperationService();
        var workflowService = new WorkflowService();
        var messageQueueStore = new ChatMessageQueueStore();
        var scheduledTaskStore = new ScheduledTaskStore();
        var agentTaskStore = new AgentTaskArtifactStore();

        return new MainPageServices(
            provider,
            airLlmProvider,
            modelLibrary,
            comfyImageProvider,
            webSearchProvider,
            settingsStore,
            conversationStore,
            projectStore,
            workspaceEditor,
            workspaceUndoStore,
            commandRunner,
            operationService,
            workflowService,
            new WorkflowRunService(workflowService),
            new LocalProjectHealthAnalyzer(),
            new CompanionStateStore(),
            new WorkspaceAttachmentDraftStore(Environment.GetEnvironmentVariable("SRAI_ATTACHMENT_DRAFTS_PATH")),
            messageQueueStore,
            scheduledTaskStore,
            agentTaskStore,
            new AgentTaskExecutor(agentTaskStore),
            new StartupRecoveryService(messageQueueStore, operationService, conversationStore, projectStore, workspaceUndoStore, agentTaskStore),
            new GitWorktreeCoordinator(),
            new SkillPackageInstaller(),
            new MemoryStore(Environment.GetEnvironmentVariable("SRAI_MEMORY_PATH")),
            new NativeTtsService());
    }
}
