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
            var run = service.Start("chat", "send a test message");
            Check(run.Status == OperationRunStatuses.Running, "service starts a run in the running state");
            run = service.Complete(run);
            Check(run.Status == OperationRunStatuses.Completed, "service completes a streamed run");

            var cancelled = service.Start("chat", "cancel a test message");
            cancelled = service.Cancel(cancelled);
            Check(cancelled.Status == OperationRunStatuses.Cancelled, "service records cancellation");
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
