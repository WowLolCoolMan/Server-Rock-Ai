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

public static class AgentTaskEnvironments
{
    public const string Local = "local";
    public const string Worktree = "worktree";

    public static bool IsSupported(string? value) => value is Local or Worktree;
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
    /// <summary>Execution boundary selected by the user before approval.</summary>
    public string Environment { get; set; } = AgentTaskEnvironments.Local;
    public string Status { get; set; } = AgentTaskStatuses.Proposed;
    public List<string> DependsOn { get; set; } = [];
    public string? ResultSummary { get; set; }
    public List<string> ArtifactLabels { get; set; } = [];
    /// <summary>Review-only file replacements returned by the local task model.</summary>
    public List<AgentTaskWorkspaceChange> WorkspaceChanges { get; set; } = [];
    /// <summary>Review-only commands returned by the local task model.</summary>
    public List<AgentTaskWorkspaceCommand> WorkspaceCommands { get; set; } = [];
    public string? WorktreePath { get; set; }
    public string? WorktreeBranch { get; set; }
    public string? WorktreeRepositoryRoot { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
