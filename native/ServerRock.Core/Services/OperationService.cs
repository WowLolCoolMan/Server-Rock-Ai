using ServerRock.Core.Infrastructure;
using ServerRock.Core.Models;

namespace ServerRock.Core.Services;

/// <summary>
/// Application-facing operation lifecycle. UI clients use this service rather
/// than writing journal events or deciding state-transition rules themselves.
/// </summary>
public sealed class OperationService
{
    private readonly OperationJournal _journal;

    public OperationService(OperationJournal? journal = null) => _journal = journal ?? new OperationJournal();

    public OperationRun Start(string workspaceId, string instruction, string actor = "user",
        string modelProvider = "unknown", string model = "unknown", string endpointPrivacy = "unknown")
    {
        var run = _journal.Create(workspaceId, instruction, actor, modelProvider, model, endpointPrivacy);
        run = _journal.Transition(run.RunId, OperationRunStatuses.Validating, actor);
        run = _journal.Transition(run.RunId, OperationRunStatuses.Queued, actor);
        return _journal.Transition(run.RunId, OperationRunStatuses.Running, actor);
    }

    public OperationRun Complete(OperationRun run, string actor = "system")
    {
        if (run.Status == OperationRunStatuses.Running)
            run = _journal.Transition(run.RunId, OperationRunStatuses.Verifying, actor);
        if (run.Status == OperationRunStatuses.Verifying || run.Status == OperationRunStatuses.ReviewReady)
            return _journal.Transition(run.RunId, OperationRunStatuses.Completed, actor);
        throw new InvalidOperationException($"Operation run cannot complete from '{run.Status}'.");
    }

    public OperationRun Fail(OperationRun run, string reason, string actor = "system")
    {
        if (!OperationJournal.CanTransition(run.Status, OperationRunStatuses.Failed))
            throw new InvalidOperationException($"Operation run cannot fail from '{run.Status}'.");
        return _journal.Transition(run.RunId, OperationRunStatuses.Failed, actor,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["reason"] = reason });
    }

    public OperationRun Cancel(OperationRun run, string actor = "user")
    {
        if (run.Status != OperationRunStatuses.Cancelling)
            run = _journal.Transition(run.RunId, OperationRunStatuses.Cancelling, actor);
        return _journal.Transition(run.RunId, OperationRunStatuses.Cancelled, actor);
    }

    public OperationRun ResolveReconciliation(OperationRun run, bool markCancelled, string actor = "user")
    {
        if (run.Status != OperationRunStatuses.NeedsReconciliation)
            throw new InvalidOperationException($"Only operations needing reconciliation can be resolved from this surface; current status is '{run.Status}'.");
        var status = markCancelled ? OperationRunStatuses.Cancelled : OperationRunStatuses.Failed;
        return _journal.Transition(run.RunId, status, actor,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["reason"] = "User reviewed the interrupted operation without replaying side effects."
            });
    }

    public IReadOnlyList<OperationRun> ReconcileAfterRestart() => _journal.ReconcileAfterRestart();

    public OperationJournalSnapshot Load() => _journal.Load();

    public string ExportJson() => _journal.ExportJson();

    public IReadOnlyList<OperationRun> Search(string? query) => _journal.Search(query);
}
