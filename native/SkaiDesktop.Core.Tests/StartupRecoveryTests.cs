// StartupRecoveryService loads local profile stores without starting external work.

using SkaiDesktop.Core.Infrastructure;
using SkaiDesktop.Core.Models;
using SkaiDesktop.Core.Services;
using static TestSupport;

internal static class StartupRecoveryTests
{
    public static void Run()
    {
        var root = Path.Combine(Path.GetTempPath(), "skai-startup-recovery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var recovery = Create(root).Load();
            Check(recovery.HistoryAvailable && recovery.ProjectsAvailable && recovery.UndoHistoryAvailable &&
                  recovery.QueuedMessages.Count == 0 && recovery.ReconciledOperations.Count == 0 && recovery.Warnings.Count == 0,
                "startup recovery loads an empty local profile without external work");

            var taskStore = new AgentTaskArtifactStore(Path.Combine(root, "agent-tasks.json"));
            var interrupted = taskStore.Create("Interrupted local task", "Review recovery behavior.");
            taskStore.Queue(interrupted.Id);
            taskStore.TryClaimReady();
            var reconciled = Create(root).Load();
            var blocked = taskStore.Get(interrupted.Id);
            Check(reconciled.ReconciledAgentTasks.Count == 1 && blocked.Status == AgentTaskStatuses.Blocked &&
                  blocked.ResultSummary?.Contains("interrupted", StringComparison.OrdinalIgnoreCase) == true &&
                  reconciled.Warnings.Any(message => message.Contains("agent task", StringComparison.OrdinalIgnoreCase)),
                "startup recovery blocks interrupted agent tasks without dispatching or deleting their local metadata");
            var secondRecovery = Create(root).Load();
            Check(secondRecovery.ReconciledAgentTasks.Count == 0 && taskStore.Get(interrupted.Id).Status == AgentTaskStatuses.Blocked,
                "agent task restart reconciliation is idempotent");

            File.WriteAllText(Path.Combine(root, "conversations.json"), "{ malformed");
            var damaged = Create(root).Load();
            Check(!damaged.HistoryAvailable && damaged.Sessions.Count == 0 &&
                  damaged.Warnings.Any(message => message.Contains("local conversation history", StringComparison.Ordinal)),
                "startup recovery preserves a malformed history file and reports a bounded warning");
        }
        finally
        {
            DeleteDirectoryTree(root);
        }
    }

    private static StartupRecoveryService Create(string root) => new(
        new ChatMessageQueueStore(Path.Combine(root, "queue.json")),
        new OperationService(new OperationJournal(Path.Combine(root, "operations.json"))),
        new ConversationStore(Path.Combine(root, "conversations.json")),
        new ProjectStore(Path.Combine(root, "projects.json")),
        new WorkspaceUndoStore(Path.Combine(root, "undo.json")),
        new AgentTaskArtifactStore(Path.Combine(root, "agent-tasks.json")));
}
