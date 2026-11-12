namespace ServerRock.Core.Models;

/// <summary>Durable states for a supervised operation or agent run.</summary>
public static class OperationRunStatuses
{
    public const string Draft = "draft";
    public const string Validating = "validating";
    public const string Queued = "queued";
    public const string Running = "running";
    public const string AwaitingApproval = "awaiting_approval";
    public const string Verifying = "verifying";
    public const string ReviewReady = "review_ready";
    public const string Completed = "completed";
    public const string Cancelling = "cancelling";
    public const string Cancelled = "cancelled";
    public const string Failed = "failed";
    public const string NeedsReconciliation = "needs_reconciliation";

    public static bool IsTerminal(string status) => status is Completed or Cancelled or Failed;
}

/// <summary>Persisted identity and state for one user-visible operation.</summary>
public sealed class OperationRun
{
    public string RunId { get; set; } = Guid.NewGuid().ToString("N");
    public string WorkspaceId { get; set; } = "default";
    public string UserInstruction { get; set; } = "";
    public string ModelProvider { get; set; } = "unknown";
    public string Model { get; set; } = "unknown";
    public string EndpointPrivacy { get; set; } = "unknown";
    public string Status { get; set; } = OperationRunStatuses.Draft;
    public long LastEventSequence { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Append-only journal envelope. Sequence is monotonic per RunId.</summary>
public sealed class OperationEvent
{
    public int SchemaVersion { get; set; } = 1;
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    public string RunId { get; set; } = "";
    public long Sequence { get; set; }
    public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;
    public string Type { get; set; } = "";
    public string Actor { get; set; } = "system";
    public string? CorrelationId { get; set; }
    public Dictionary<string, string> Payload { get; set; } = new(StringComparer.Ordinal);
}
