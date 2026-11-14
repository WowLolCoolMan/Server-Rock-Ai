namespace SkaiDesktop.Core.Models;

public static class AgentTaskStatuses
{
    public const string Proposed = "proposed";
    public const string AwaitingApproval = "awaiting_approval";
    public const string Queued = "queued";
    public const string Running = "running";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";
    public const string Blocked = "blocked";
}

/// <summary>Bounded local metadata for a planned agent task; it does not authorize execution.</summary>
public sealed class AgentTaskArtifact
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string? ParentRunId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Instruction { get; set; } = string.Empty;
    public string Role { get; set; } = "specialist";
    /// <summary>Optional selected project identity whose bounded read-only context may be supplied at execution time.</summary>
    public string? ProjectId { get; set; }
    public string Status { get; set; } = AgentTaskStatuses.Proposed;
    public List<string> DependsOn { get; set; } = [];
    public string? ResultSummary { get; set; }
    public List<string> ArtifactLabels { get; set; } = [];
    public string? WorktreePath { get; set; }
    public string? WorktreeBranch { get; set; }
    public string? WorktreeRepositoryRoot { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
