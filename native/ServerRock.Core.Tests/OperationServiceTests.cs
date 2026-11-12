using ServerRock.Core.Infrastructure;
using ServerRock.Core.Models;
using ServerRock.Core.Services;

using static TestSupport;

internal static class OperationServiceTests
{
    public static void Run()
    {
        var path = Path.Combine(Path.GetTempPath(), $"server-rock-operation-service-{Guid.NewGuid():N}.json");
        try
        {
            var service = new OperationService(new OperationJournal(path));
            var run = service.Start("chat", "send a test message", modelProvider: "Ollama", model: "llama3.2", endpointPrivacy: "local");
            Check(run.Status == OperationRunStatuses.Running, "service starts a run in the running state");
            Check(run.ModelProvider == "Ollama" && run.Model == "llama3.2" && run.EndpointPrivacy == "local", "service persists provider provenance");
            run = service.RecordProviderAttempt(run, "started");
            run = service.RecordProviderAttempt(run, "failed", "model compatibility failure");
            var attemptEvents = new OperationJournal(path).Load().Events.Where(item => item.RunId == run.RunId).ToArray();
            Check(attemptEvents.Any(item => item.Type == "provider.attempt.started" && item.Payload["model"] == "llama3.2") &&
                  attemptEvents.Any(item => item.Type == "provider.attempt.failed" && item.Payload["reason"] == "model compatibility failure"),
                "service durably records bounded provider attempt outcomes");
            run = service.Complete(run);
            Check(run.Status == OperationRunStatuses.Completed, "service completes a streamed run");

            var cancelled = service.Start("chat", "cancel a test message");
            cancelled = service.Cancel(cancelled);
            Check(cancelled.Status == OperationRunStatuses.Cancelled, "service records cancellation");

            var interrupted = service.Start("chat", "resolve an interrupted message");
            var reloaded = new OperationJournal(path);
            reloaded.ReconcileAfterRestart();
            interrupted = new OperationJournal(path).Load().Runs.Single(item => item.RunId == interrupted.RunId);
            interrupted = service.ResolveReconciliation(interrupted, markCancelled: false);
            Check(interrupted.Status == OperationRunStatuses.Failed, "service records a reviewed reconciliation decision");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException($"Operation service test failed: {message}");
    }
}
