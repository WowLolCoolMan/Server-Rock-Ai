using ServerRock.Core.Infrastructure;
using ServerRock.Core.Models;
using static TestSupport;

internal static class OperationJournalTests
{
    public static void Run()
    {
        var path = Path.Combine(Path.GetTempPath(), "server-rock-operation-journal-" + Guid.NewGuid().ToString("N"), "operations.json");
        try
        {
            var journal = new OperationJournal(path);
            var run = journal.Create("workspace-1", "Review the selected project safely.");
            Check(run.Status == OperationRunStatuses.Draft && run.LastEventSequence == 1, "operation creation persists a draft run identity");
            Check(journal.Load().Events is [{ Sequence: 1, Type: "run.created" }], "operation creation appends the first journal event");

            journal.Transition(run.RunId, OperationRunStatuses.Validating);
            journal.Transition(run.RunId, OperationRunStatuses.Queued);
            journal.Transition(run.RunId, OperationRunStatuses.Running);
            var waiting = journal.Transition(run.RunId, OperationRunStatuses.AwaitingApproval, payload: new Dictionary<string, string> { ["action"] = "write-file" });
            Check(waiting.Status == OperationRunStatuses.AwaitingApproval && waiting.LastEventSequence == 5, "operation transitions advance a monotonic sequence");
            Check(journal.Load().Events.Last().Payload["action"] == "write-file", "operation events retain bounded action metadata");

            try
            {
                journal.Transition(run.RunId, OperationRunStatuses.Completed);
                throw new Exception("Expected invalid operation transition to be rejected.");
            }
            catch (InvalidOperationException) { Check(true, "invalid operation transitions are rejected"); }

            var recovered = journal.ReconcileAfterRestart();
            Check(recovered.Count == 1 && recovered[0].Status == OperationRunStatuses.NeedsReconciliation, "in-flight operations become needs-reconciliation after restart");
            var reloaded = new OperationJournal(path).Load();
            Check(reloaded.Runs[0].Status == OperationRunStatuses.NeedsReconciliation && reloaded.Events.Last().Type == "run.needs_reconciliation", "reconciliation is durable and does not replay side effects");
            Check(journal.ReconcileAfterRestart().Count == 0, "reconciliation is idempotent for already-reconciled runs");
            journal.Transition(run.RunId, OperationRunStatuses.Running, actor: "reviewer");
            journal.Transition(run.RunId, OperationRunStatuses.Cancelling);
            journal.Transition(run.RunId, OperationRunStatuses.Cancelled);
            Check(new OperationJournal(path).Load().Runs[0].Status == OperationRunStatuses.Cancelled, "reconciled operations can be explicitly cancelled");
        }
        finally
        {
            try { if (File.Exists(path)) Directory.Delete(Path.GetDirectoryName(path)!, recursive: true); }
            catch (IOException) { }
        }
    }
}
