// StartupRecoveryService loads local profile stores without starting external work.

using ServerRock.Core.Infrastructure;
using ServerRock.Core.Services;
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
        new WorkspaceUndoStore(Path.Combine(root, "undo.json")));
}
