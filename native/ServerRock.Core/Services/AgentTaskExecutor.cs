using ServerRock.Core.Models;

namespace ServerRock.Core.Services;

/// <summary>
/// Runs one explicitly supplied agent-task handler with durable claim and terminal
/// state updates. The executor itself never selects a model, invokes a tool, or
/// starts a process; those capabilities remain the caller's explicit boundary.
/// </summary>
public sealed class AgentTaskExecutor
{
    private readonly AgentTaskArtifactStore _store;

    public AgentTaskExecutor(AgentTaskArtifactStore store) => _store = store ?? throw new ArgumentNullException(nameof(store));

    public async Task<AgentTaskArtifact?> ExecuteNextAsync(
        Func<AgentTaskArtifact, CancellationToken, Task<AgentTaskExecutionResult>> handler,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);
        var task = _store.TryClaimReady();
        if (task is null) return null;
        try
        {
            var result = await handler(task, cancellationToken).ConfigureAwait(false);
            return _store.Complete(task.Id, result.Summary, result.ArtifactLabels);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return _store.Cancel(task.Id, "Execution was cancelled before the handler completed.");
        }
        catch (Exception error)
        {
            var summary = string.IsNullOrWhiteSpace(error.Message) ? "The task handler failed." : error.Message;
            return _store.Fail(task.Id, summary);
        }
    }

    /// <summary>
    /// Claims one task, creates a sibling Git worktree, persists the lease, and
    /// invokes the injected handler with that isolated target. The worktree is
    /// intentionally retained after the handler returns; callers must invoke
    /// <see cref="ReleaseTaskWorktreeAsync"/> after review and only a clean
    /// registered worktree can be removed.
    /// </summary>
    public async Task<AgentTaskArtifact?> ExecuteNextInWorktreeAsync(
        GitWorktreeCoordinator coordinator,
        string repositoryPath,
        string worktreeParent,
        Func<AgentTaskArtifact, GitWorktreeLease, CancellationToken, Task<AgentTaskExecutionResult>> handler,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(coordinator);
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(worktreeParent);
        ArgumentNullException.ThrowIfNull(handler);
        var task = _store.TryClaimReady();
        if (task is null) return null;

        GitWorktreeLease? lease = null;
        try
        {
            var target = Path.Combine(Path.GetFullPath(worktreeParent), $"agent-task-{task.Id}");
            lease = await coordinator.CreateAsync(repositoryPath, target, $"codex/agent-task/{task.Id}", cancellationToken: cancellationToken).ConfigureAwait(false);
            task = _store.AttachWorktree(task.Id, lease.RepositoryRoot, lease.WorktreePath, lease.Branch);
            var result = await handler(task, lease, cancellationToken).ConfigureAwait(false);
            return _store.Complete(task.Id, result.Summary, result.ArtifactLabels);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return _store.Cancel(task.Id, "Execution was cancelled before the handler completed; the worktree was retained for review.");
        }
        catch (Exception error)
        {
            var summary = string.IsNullOrWhiteSpace(error.Message) ? "The isolated task handler failed; the worktree was retained for review." : error.Message;
            return _store.Fail(task.Id, summary);
        }
    }

    public async Task<AgentTaskArtifact> ReleaseTaskWorktreeAsync(
        string taskId,
        GitWorktreeCoordinator coordinator,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        ArgumentNullException.ThrowIfNull(coordinator);
        var task = _store.Get(taskId);
        if (task.WorktreeRepositoryRoot is null || task.WorktreePath is null || task.WorktreeBranch is null) throw new InvalidOperationException("The task has no retained worktree.");
        await coordinator.ReleaseCleanAsync(task.WorktreeRepositoryRoot, task.WorktreePath, cancellationToken).ConfigureAwait(false);
        return _store.ClearWorktree(task.Id);
    }
}

public sealed record AgentTaskExecutionResult(string Summary, IReadOnlyList<string>? ArtifactLabels = null);
