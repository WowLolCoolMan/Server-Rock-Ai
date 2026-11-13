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
}

public sealed record AgentTaskExecutionResult(string Summary, IReadOnlyList<string>? ArtifactLabels = null);
