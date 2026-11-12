using ServerRock.Core.Models;
using ServerRock.Core.Services;

internal static class ScheduledTaskTests
{
    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "server-rock-scheduled-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var store = new ScheduledTaskStore(Path.Combine(directory, "scheduled.json"));
        var now = DateTimeOffset.UtcNow;
        var task = store.Schedule("Review the release checklist", now.AddMinutes(5), WorkspaceModes.Chat, now);
        Check(store.Load().Count == 1 && store.Load()[0].Status == ScheduledTaskStatuses.Scheduled, "scheduled task persists as pending");
        Check(store.TryClaimDue(now) is null, "future scheduled task is not claimed early");
        var claimed = store.TryClaimDue(now.AddMinutes(6));
        Check(claimed is not null && claimed.Status == ScheduledTaskStatuses.Running, "due scheduled task is claimed durably");
        Check(store.TryClaimDue(now.AddMinutes(7)) is null, "claimed scheduled task is not replayed");
        store.Complete(claimed!.Id, succeeded: true, "queued for reviewed chat execution", now.AddMinutes(6));
        var completed = store.Load().Single(item => item.Id == claimed.Id);
        Check(completed.Status == ScheduledTaskStatuses.Completed && completed.Outcome!.Contains("reviewed", StringComparison.Ordinal), "scheduled task records its outcome");

        var paused = store.Schedule("Paused task", now.AddMinutes(8), WorkspaceModes.CodingAgent, now);
        store.Pause(paused.Id);
        Check(store.TryClaimDue(now.AddMinutes(9)) is null, "paused scheduled task is not claimed");
        store.Resume(paused.Id);
        Check(store.TryClaimDue(now.AddMinutes(9))?.Id == paused.Id, "paused task resumes explicitly");
        Console.WriteLine("PASS scheduled task persistence, claim, no-replay, outcome, pause, and resume");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("FAIL " + message);
    }
}
