// Deterministic local-profile recovery for app startup.
// This service loads existing JSON stores and reconciles in-flight operations;
// it never starts providers, tools, shells, network requests, or UI work.

using ServerRock.Core.Infrastructure;
using ServerRock.Core.Models;

namespace ServerRock.Core.Services;

public sealed record StartupRecoveryResult(
    IReadOnlyList<QueuedChatRequest> QueuedMessages,
    IReadOnlyList<OperationRun> ReconciledOperations,
    IReadOnlyList<ChatSession> Sessions,
    IReadOnlyList<LocalProject> Projects,
    IReadOnlyList<WorkspaceUndoEntry> UndoEntries,
    IReadOnlyList<string> Warnings,
    bool HistoryAvailable,
    bool ProjectsAvailable,
    bool UndoHistoryAvailable);

public sealed class StartupRecoveryService(
    ChatMessageQueueStore queueStore,
    OperationService operationService,
    ConversationStore conversationStore,
    ProjectStore projectStore,
    WorkspaceUndoStore undoStore)
{
    public StartupRecoveryResult Load()
    {
        ArgumentNullException.ThrowIfNull(queueStore);
        ArgumentNullException.ThrowIfNull(operationService);
        ArgumentNullException.ThrowIfNull(conversationStore);
        ArgumentNullException.ThrowIfNull(projectStore);
        ArgumentNullException.ThrowIfNull(undoStore);

        var warnings = new List<string>();
        var queuedMessages = LoadQueue(warnings);
        var reconciledOperations = ReconcileOperations(warnings);
        var sessions = LoadConversations(warnings, out var historyAvailable);
        var projects = LoadProjects(warnings, out var projectsAvailable);
        var undoEntries = LoadUndo(warnings, out var undoHistoryAvailable);
        return new(queuedMessages, reconciledOperations, sessions, projects, undoEntries,
            warnings, historyAvailable, projectsAvailable, undoHistoryAvailable);
    }

    private IReadOnlyList<QueuedChatRequest> LoadQueue(List<string> warnings)
    {
        try
        {
            var queued = queueStore.Load();
            return queued;
        }
        catch (Exception ex)
        {
            warnings.Add($"Queued messages could not be restored; the saved queue was left untouched: {ex.Message}");
            return [];
        }
    }

    private IReadOnlyList<OperationRun> ReconcileOperations(List<string> warnings)
    {
        try
        {
            var reconciled = operationService.ReconcileAfterRestart();
            if (reconciled.Count > 0)
                warnings.Add($"{reconciled.Count} interrupted operation{(reconciled.Count == 1 ? "" : "s")} need review in Activity & tasks.");
            return reconciled;
        }
        catch (Exception ex)
        {
            warnings.Add($"Operation history could not be reconciled: {ex.Message}");
            return [];
        }
    }

    private List<ChatSession> LoadConversations(List<string> warnings, out bool available)
    {
        try
        {
            available = true;
            return conversationStore.Load();
        }
        catch (Exception ex)
        {
            available = false;
            warnings.Add($"Could not load local conversation history: {ex.Message}");
            return [];
        }
    }

    private List<LocalProject> LoadProjects(List<string> warnings, out bool available)
    {
        try
        {
            available = true;
            return projectStore.Load();
        }
        catch (Exception ex)
        {
            available = false;
            warnings.Add($"Could not load local projects: {ex.Message}");
            return [];
        }
    }

    private List<WorkspaceUndoEntry> LoadUndo(List<string> warnings, out bool available)
    {
        try
        {
            available = true;
            return undoStore.Load();
        }
        catch (Exception ex)
        {
            available = false;
            warnings.Add($"Could not load project undo history: {ex.Message}");
            return [];
        }
    }
}
