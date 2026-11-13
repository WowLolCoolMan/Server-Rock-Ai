namespace ServerRock.Core.Models;

public static class ScheduledTaskStatuses
{
    public const string Scheduled = "scheduled";
    public const string Running = "running";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Paused = "paused";
    public const string Cancelled = "cancelled";
}

/// <summary>A durable, one-shot task request. The scheduler never replays a claimed task after restart.</summary>
public sealed class ScheduledTask
{
    public const int MinRepeatEveryMinutes = 1;
    public const int MaxRepeatEveryMinutes = 7 * 24 * 60;
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Prompt { get; set; } = string.Empty;
    public string WorkspaceMode { get; set; } = WorkspaceModes.Chat;
    public DateTimeOffset DueAtUtc { get; set; }
    public int? RepeatEveryMinutes { get; set; }
    public int RunCount { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string Status { get; set; } = ScheduledTaskStatuses.Scheduled;
    public string? Outcome { get; set; }
}
