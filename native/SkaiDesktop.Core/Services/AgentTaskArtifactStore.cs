using System.Text.Json;
using SkaiDesktop.Core.Infrastructure;
using SkaiDesktop.Core.Models;

namespace SkaiDesktop.Core.Services;

/// <summary>Persists a bounded, reviewable local task list without dispatching agents or tools.</summary>
public sealed class AgentTaskArtifactStore
{
    public const int MaxTasks = 256;
    public const int MaxTitleCharacters = 200;
    public const int MaxInstructionCharacters = 4_000;
    public const int MaxRoleCharacters = 80;
    public const int MaxResultCharacters = 2_000;
    public const int MaxArtifactLabelCharacters = 160;
    public const int MaxDependencies = 8;
    public const int MaxArtifactLabels = 8;
    public const int MaxWorktreePathCharacters = 4_096;
    public const int MaxWorktreeBranchCharacters = 256;
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true, MaxDepth = 12 };
    private readonly string _path;

    public AgentTaskArtifactStore(string? path = null)
    {
        _path = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ServerRock", "AI", "agent-tasks.json");
    }

    public IReadOnlyList<AgentTaskArtifact> Load()
    {
        if (!File.Exists(_path)) return [];
        try
        {
            var tasks = JsonSerializer.Deserialize<List<AgentTaskArtifact>>(BoundedFileReader.ReadText(_path), Options) ?? [];
            Validate(tasks);
            return tasks.Select(Copy).OrderByDescending(task => task.UpdatedAtUtc).ToArray();
        }
        catch (JsonException error)
        {
            throw new InvalidDataException("Agent task artifacts could not be read. The original file was left untouched.", error);
        }
    }

    public AgentTaskArtifact Create(string title, string instruction, string role = "specialist", string? parentRunId = null, IEnumerable<string>? dependsOn = null)
    {
        var now = DateTimeOffset.UtcNow;
        var task = new AgentTaskArtifact
        {
            Title = Normalize(title, MaxTitleCharacters, "task title"),
            Instruction = Normalize(instruction, MaxInstructionCharacters, "task instruction"),
            Role = Normalize(role, MaxRoleCharacters, "task role"),
            ParentRunId = NormalizeOptional(parentRunId, 128, "parent run ID"),
            DependsOn = NormalizeIds(dependsOn),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var tasks = Load().ToList();
        if (tasks.Count >= MaxTasks) throw new InvalidOperationException($"At most {MaxTasks} agent tasks can be saved.");
        var knownIds = tasks.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        if (task.DependsOn.Any(id => !knownIds.Contains(id))) throw new ArgumentException("Every task dependency must reference an existing task.", nameof(dependsOn));
        if (task.DependsOn.Contains(task.Id, StringComparer.Ordinal)) throw new ArgumentException("A task cannot depend on itself.", nameof(dependsOn));
        tasks.Add(task);
        Save(tasks);
        return Copy(task);
    }

    public AgentTaskArtifact Queue(string id) => Transition(id, AgentTaskStatuses.Proposed, AgentTaskStatuses.Queued);

    /// <summary>Moves a planned task into the explicit approval inbox without claiming or executing it.</summary>
    public AgentTaskArtifact RequestApproval(string id)
    {
        var tasks = Load().ToList();
        var task = tasks.SingleOrDefault(item => item.Id == id) ?? throw new KeyNotFoundException("The agent task does not exist.");
        if (task.Status != AgentTaskStatuses.Proposed)
            throw new InvalidOperationException($"A task in {task.Status} state cannot request approval.");
        task.Status = AgentTaskStatuses.AwaitingApproval;
        task.ResultSummary = "Awaiting explicit approval before local execution.";
        task.UpdatedAtUtc = DateTimeOffset.UtcNow;
        Save(tasks);
        return Copy(task);
    }

    /// <summary>Approves a task for the bounded local executor; approval never starts execution.</summary>
    public AgentTaskArtifact Approve(string id, string approvalId)
    {
        if (string.IsNullOrWhiteSpace(approvalId) || approvalId.Length > 128 || approvalId.Any(char.IsControl))
            throw new ArgumentException("Approval identity is invalid.", nameof(approvalId));
        var tasks = Load().ToList();
        var task = tasks.SingleOrDefault(item => item.Id == id) ?? throw new KeyNotFoundException("The agent task does not exist.");
        if (task.Status != AgentTaskStatuses.AwaitingApproval)
            throw new InvalidOperationException($"A task in {task.Status} state cannot be approved.");
        task.Status = AgentTaskStatuses.Queued;
        task.ResultSummary = $"Approved by {approvalId}; waiting for an explicit local executor.";
        task.UpdatedAtUtc = DateTimeOffset.UtcNow;
        Save(tasks);
        return Copy(task);
    }

    /// <summary>Denies an awaiting task and records the bounded review reason without executing it.</summary>
    public AgentTaskArtifact Deny(string id, string reason)
    {
        var normalizedReason = Normalize(reason, MaxResultCharacters, "approval denial reason");
        var tasks = Load().ToList();
        var task = tasks.SingleOrDefault(item => item.Id == id) ?? throw new KeyNotFoundException("The agent task does not exist.");
        if (task.Status != AgentTaskStatuses.AwaitingApproval)
            throw new InvalidOperationException($"A task in {task.Status} state cannot be denied.");
        task.Status = AgentTaskStatuses.Cancelled;
        task.ResultSummary = $"Approval denied: {normalizedReason}";
        task.UpdatedAtUtc = DateTimeOffset.UtcNow;
        Save(tasks);
        return Copy(task);
    }

    public AgentTaskArtifact Get(string id) => Copy(Load().SingleOrDefault(item => item.Id == id) ?? throw new KeyNotFoundException("The agent task does not exist."));

    public AgentTaskArtifact AttachWorktree(string id, string repositoryRoot, string path, string branch)
    {
        var tasks = Load().ToList();
        var task = tasks.SingleOrDefault(item => item.Id == id) ?? throw new KeyNotFoundException("The agent task does not exist.");
        if (task.Status != AgentTaskStatuses.Running) throw new InvalidOperationException("A worktree can only be attached to a running task.");
        task.WorktreeRepositoryRoot = Normalize(repositoryRoot, MaxWorktreePathCharacters, "worktree repository root");
        task.WorktreePath = Normalize(path, MaxWorktreePathCharacters, "worktree path");
        task.WorktreeBranch = Normalize(branch, MaxWorktreeBranchCharacters, "worktree branch");
        task.UpdatedAtUtc = DateTimeOffset.UtcNow;
        Save(tasks);
        return Copy(task);
    }

    public AgentTaskArtifact ClearWorktree(string id)
    {
        var tasks = Load().ToList();
        var task = tasks.SingleOrDefault(item => item.Id == id) ?? throw new KeyNotFoundException("The agent task does not exist.");
        if (task.Status == AgentTaskStatuses.Running) throw new InvalidOperationException("A running task cannot release its worktree.");
        task.WorktreePath = null;
        task.WorktreeBranch = null;
        task.WorktreeRepositoryRoot = null;
        task.UpdatedAtUtc = DateTimeOffset.UtcNow;
        Save(tasks);
        return Copy(task);
    }

    /// <summary>Cancels a proposed or queued plan before any future executor can claim it.</summary>
    public AgentTaskArtifact CancelPlanned(string id, string reason = "Cancelled before execution.")
    {
        var tasks = Load().ToList();
        var task = tasks.SingleOrDefault(item => item.Id == id) ?? throw new KeyNotFoundException("The agent task does not exist.");
        if (task.Status is not (AgentTaskStatuses.Proposed or AgentTaskStatuses.AwaitingApproval or AgentTaskStatuses.Queued))
            throw new InvalidOperationException($"A task in {task.Status} state cannot be cancelled before execution.");
        task.Status = AgentTaskStatuses.Cancelled;
        task.ResultSummary = Normalize(reason, MaxResultCharacters, "task cancellation reason");
        task.UpdatedAtUtc = DateTimeOffset.UtcNow;
        Save(tasks);
        return Copy(task);
    }

    /// <summary>
    /// Re-queues a blocked task only after an explicit user decision. Retained
    /// worktrees must be reviewed or released first; retry never starts work.
    /// </summary>
    public AgentTaskArtifact RetryBlocked(string id)
    {
        var tasks = Load().ToList();
        var task = tasks.SingleOrDefault(item => item.Id == id) ?? throw new KeyNotFoundException("The agent task does not exist.");
        if (task.Status != AgentTaskStatuses.Blocked)
            throw new InvalidOperationException($"A task in {task.Status} state cannot be retried from the blocked state.");
        if (task.WorktreePath is not null)
            throw new InvalidOperationException("Review or release the retained worktree before retrying this task.");
        task.Status = AgentTaskStatuses.Queued;
        task.ResultSummary = "Retry approved locally; waiting for an explicit executor.";
        task.ArtifactLabels = [];
        task.UpdatedAtUtc = DateTimeOffset.UtcNow;
        Save(tasks);
        return Copy(task);
    }

    /// <summary>Returns the oldest dependency-ready queued task without changing local state or dispatching work.</summary>
    public AgentTaskArtifact? GetNextReady()
    {
        var tasks = Load();
        return tasks
            .Where(task => IsReady(task, tasks))
            .OrderBy(task => task.CreatedAtUtc)
            .Select(Copy)
            .FirstOrDefault();
    }

    /// <summary>Claims one specific queued task when all of its dependencies are complete.</summary>
    public AgentTaskArtifact TryClaim(string id, DateTimeOffset? now = null)
    {
        var tasks = Load().ToList();
        var task = tasks.SingleOrDefault(item => item.Id == id) ?? throw new KeyNotFoundException("The agent task does not exist.");
        if (!IsReady(task, tasks))
            throw new InvalidOperationException(task.Status == AgentTaskStatuses.Queued
                ? "This task is waiting for its dependencies to complete."
                : $"A task in {task.Status} state cannot be executed.");
        task.Status = AgentTaskStatuses.Running;
        task.UpdatedAtUtc = now ?? DateTimeOffset.UtcNow;
        Save(tasks);
        return Copy(task);
    }

    /// <summary>
    /// Reconciles tasks that were running when the process stopped. This is a
    /// local metadata transition only: it does not retry handlers, release
    /// worktrees, or invoke tools. Retained worktrees remain attached for an
    /// explicit review decision.
    /// </summary>
    public IReadOnlyList<AgentTaskArtifact> ReconcileAfterRestart()
    {
        var tasks = Load().ToList();
        var interrupted = tasks.Where(task => task.Status == AgentTaskStatuses.Running).ToList();
        if (interrupted.Count == 0) return [];
        var now = DateTimeOffset.UtcNow;
        foreach (var task in interrupted)
        {
            task.Status = AgentTaskStatuses.Blocked;
            task.ResultSummary = "Execution was interrupted by an application restart; review the task before retrying.";
            task.UpdatedAtUtc = now;
        }
        Save(tasks);
        return interrupted.Select(Copy).ToArray();
    }

    /// <summary>Claims one ready queued task for a future executor; this method performs no external work.</summary>
    public AgentTaskArtifact? TryClaimReady(DateTimeOffset? now = null)
    {
        var tasks = Load().ToList();
        var current = now ?? DateTimeOffset.UtcNow;
        var task = tasks.Where(item => IsReady(item, tasks))
            .OrderBy(item => item.CreatedAtUtc).FirstOrDefault();
        if (task is null) return null;
        task.Status = AgentTaskStatuses.Running;
        task.UpdatedAtUtc = current;
        Save(tasks);
        return Copy(task);
    }

    private static bool IsReady(AgentTaskArtifact task, IReadOnlyList<AgentTaskArtifact> tasks) =>
        task.Status == AgentTaskStatuses.Queued && task.DependsOn.All(dependencyId =>
            tasks.Any(dependency => dependency.Id == dependencyId && dependency.Status == AgentTaskStatuses.Completed));

    public AgentTaskArtifact Complete(string id, string resultSummary, IEnumerable<string>? artifactLabels = null) =>
        Finish(id, AgentTaskStatuses.Completed, resultSummary, artifactLabels);

    public AgentTaskArtifact Fail(string id, string resultSummary) => Finish(id, AgentTaskStatuses.Failed, resultSummary, null);

    public AgentTaskArtifact Cancel(string id, string resultSummary) => Finish(id, AgentTaskStatuses.Cancelled, resultSummary, null);

    public IReadOnlyList<AgentTaskArtifact> Search(string? query)
    {
        var normalized = query?.Trim() ?? string.Empty;
        if (normalized.Length > 512 || normalized.Any(char.IsControl)) throw new ArgumentException("The task search query is invalid.", nameof(query));
        return Load().Where(task => normalized.Length == 0 || task.Title.Contains(normalized, StringComparison.OrdinalIgnoreCase) || task.Instruction.Contains(normalized, StringComparison.OrdinalIgnoreCase) || task.Role.Contains(normalized, StringComparison.OrdinalIgnoreCase) || task.Status.Contains(normalized, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    private AgentTaskArtifact Transition(string id, string expected, string next)
    {
        var tasks = Load().ToList();
        var task = tasks.SingleOrDefault(item => item.Id == id) ?? throw new KeyNotFoundException("The agent task does not exist.");
        if (task.Status != expected) throw new InvalidOperationException($"A task in {task.Status} state cannot be changed to {next}.");
        task.Status = next;
        task.UpdatedAtUtc = DateTimeOffset.UtcNow;
        Save(tasks);
        return Copy(task);
    }

    private AgentTaskArtifact Finish(string id, string status, string resultSummary, IEnumerable<string>? artifactLabels)
    {
        var tasks = Load().ToList();
        var task = tasks.SingleOrDefault(item => item.Id == id) ?? throw new KeyNotFoundException("The agent task does not exist.");
        if (task.Status != AgentTaskStatuses.Running) throw new InvalidOperationException("Only a claimed running task can be finished.");
        task.Status = status;
        task.ResultSummary = Normalize(resultSummary, MaxResultCharacters, "task result");
        task.ArtifactLabels = NormalizeLabels(artifactLabels);
        task.UpdatedAtUtc = DateTimeOffset.UtcNow;
        Save(tasks);
        return Copy(task);
    }

    private void Save(IEnumerable<AgentTaskArtifact> tasks)
    {
        var list = tasks.Select(Copy).ToList();
        Validate(list);
        AtomicFileWriter.WriteText(_path, BoundedFileReader.EnsureTextSize(JsonSerializer.Serialize(list, Options), "agent tasks"));
    }

    private static void Validate(IReadOnlyList<AgentTaskArtifact> tasks)
    {
        if (tasks.Count > MaxTasks) throw new InvalidDataException("The agent task store exceeds its safety bound.");
        var ids = tasks.Select(task => task.Id).ToHashSet(StringComparer.Ordinal);
        if (ids.Count != tasks.Count || tasks.Any(task => string.IsNullOrWhiteSpace(task.Id))) throw new InvalidDataException("The agent task store contains invalid or duplicate IDs.");
        foreach (var task in tasks)
        {
            Normalize(task.Title, MaxTitleCharacters, "task title");
            Normalize(task.Instruction, MaxInstructionCharacters, "task instruction");
            Normalize(task.Role, MaxRoleCharacters, "task role");
            NormalizeOptional(task.ParentRunId, 128, "parent run ID");
            NormalizeIds(task.DependsOn);
            if (task.DependsOn.Any(id => !ids.Contains(id)) || task.DependsOn.Contains(task.Id, StringComparer.Ordinal)) throw new InvalidDataException("The agent task store contains an invalid dependency.");
            if (task.Status is not (AgentTaskStatuses.Proposed or AgentTaskStatuses.AwaitingApproval or AgentTaskStatuses.Queued or AgentTaskStatuses.Running or AgentTaskStatuses.Completed or AgentTaskStatuses.Failed or AgentTaskStatuses.Cancelled or AgentTaskStatuses.Blocked)) throw new InvalidDataException("The agent task store contains an invalid status.");
            if (task.ResultSummary is not null) Normalize(task.ResultSummary, MaxResultCharacters, "task result");
            NormalizeLabels(task.ArtifactLabels);
            NormalizeOptional(task.WorktreeRepositoryRoot, MaxWorktreePathCharacters, "worktree repository root");
            NormalizeOptional(task.WorktreePath, MaxWorktreePathCharacters, "worktree path");
            NormalizeOptional(task.WorktreeBranch, MaxWorktreeBranchCharacters, "worktree branch");
            var hasRepositoryRoot = task.WorktreeRepositoryRoot is not null;
            var hasWorktreePath = task.WorktreePath is not null;
            var hasWorktreeBranch = task.WorktreeBranch is not null;
            if (hasRepositoryRoot != hasWorktreePath || hasWorktreePath != hasWorktreeBranch) throw new InvalidDataException("The agent task store contains an incomplete worktree lease.");
        }
    }

    private static string Normalize(string value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength || value.Any(char.IsControl)) throw new ArgumentException($"The {label} is invalid.", label);
        return value.Trim();
    }

    private static string? NormalizeOptional(string? value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Normalize(value, maxLength, label);
    }

    private static List<string> NormalizeIds(IEnumerable<string>? values)
    {
        var result = (values ?? []).Select(value => Normalize(value, 128, "task dependency ID")).Distinct(StringComparer.Ordinal).ToList();
        if (result.Count > MaxDependencies) throw new ArgumentException($"A task can have at most {MaxDependencies} dependencies.");
        return result;
    }

    private static List<string> NormalizeLabels(IEnumerable<string>? values)
    {
        var result = (values ?? []).Select(value => Normalize(value, MaxArtifactLabelCharacters, "artifact label")).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (result.Count > MaxArtifactLabels) throw new ArgumentException($"A task can have at most {MaxArtifactLabels} artifact labels.");
        return result;
    }

    private static AgentTaskArtifact Copy(AgentTaskArtifact source) => new()
    {
        Id = source.Id,
        ParentRunId = source.ParentRunId,
        Title = source.Title,
        Instruction = source.Instruction,
        Role = source.Role,
        Status = source.Status,
        DependsOn = source.DependsOn.ToList(),
        ResultSummary = source.ResultSummary,
        ArtifactLabels = source.ArtifactLabels.ToList(),
        WorktreePath = source.WorktreePath,
        WorktreeBranch = source.WorktreeBranch,
        WorktreeRepositoryRoot = source.WorktreeRepositoryRoot,
        CreatedAtUtc = source.CreatedAtUtc,
        UpdatedAtUtc = source.UpdatedAtUtc
    };
}
