namespace ServerRock.Core.Models;

public static class WorkflowRunStatuses
{
    public const string Draft = "draft";
    public const string Queued = "queued";
    public const string Running = "running";
    public const string AwaitingApproval = "awaiting_approval";
    public const string Verifying = "verifying";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";
    public const string NeedsReconciliation = "needs_reconciliation";
}

public static class WorkflowStepStatuses
{
    public const string Pending = "pending";
    public const string Queued = "queued";
    public const string Executing = "executing";
    public const string WaitingApproval = "waiting_approval";
    public const string Complete = "complete";
    public const string Failed = "failed";
    public const string Skipped = "skipped";
    public const string Cancelled = "cancelled";
}

public sealed class WorkflowRun
{
    public string RunId { get; set; } = Guid.NewGuid().ToString("N");
    public string WorkflowId { get; set; } = "";
    public int WorkflowVersion { get; set; }
    public string Status { get; set; } = WorkflowRunStatuses.Draft;
    public List<WorkflowStepRun> Steps { get; set; } = [];
    public long LastEventSequence { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class WorkflowStepRun
{
    public string NodeId { get; set; } = "";
    public string NodeType { get; set; } = "";
    public string Label { get; set; } = "";
    public List<string> DependsOn { get; set; } = [];
    public Dictionary<string, string> Config { get; set; } = new(StringComparer.Ordinal);
    public string Status { get; set; } = WorkflowStepStatuses.Pending;
    public int Attempt { get; set; } = 1;
    public bool RequiresApproval { get; set; }
    public bool SafeToRetry { get; set; }
    public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString("N");
}

public sealed class WorkflowRunEvent
{
    public int SchemaVersion { get; set; } = 1;
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    public string RunId { get; set; } = "";
    public long Sequence { get; set; }
    public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;
    public string Type { get; set; } = "";
    public string Actor { get; set; } = "system";
    public Dictionary<string, string> Payload { get; set; } = new(StringComparer.Ordinal);
}
