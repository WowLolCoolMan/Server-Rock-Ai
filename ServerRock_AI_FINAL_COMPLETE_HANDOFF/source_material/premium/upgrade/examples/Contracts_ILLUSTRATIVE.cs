// ILLUSTRATIVE ONLY: refine against existing SkaiDesktop.Core and verify compilation.
// Never paste as a replacement for already working services.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServerRock.SpecExamples;

public readonly record struct RunId(Guid Value);
public readonly record struct WorkspaceId(Guid Value);
public enum ExecutionStatus { Created, Preflight, Queued, Running, WaitingApproval, Verifying, AwaitingReview, Completed, Failed, Cancelling, Cancelled, Interrupted }
public sealed record OperationEvent(
    int SchemaVersion, Guid EventId, RunId RunId, long Sequence,
    DateTimeOffset TimestampUtc, string Type, string Actor,
    string CorrelationId, IReadOnlyDictionary<string, object?> Payload);
public sealed record ApprovalRequest(
    Guid ApprovalId, RunId RunId, WorkspaceId WorkspaceId,
    string ToolId, string CanonicalOperationHash,
    string HumanReadableImpact, DateTimeOffset ExpiresAtUtc);
public enum ApprovalDecision { GrantedOnce, Denied, Expired }

public interface IOperationJournal
{
    Task AppendAsync(OperationEvent evt, CancellationToken ct);
    IAsyncEnumerable<OperationEvent> ReplayAsync(RunId run, long afterSequence, CancellationToken ct);
}
public interface IApprovalService
{
    Task<ApprovalDecision> RequestAsync(ApprovalRequest request, CancellationToken ct);
    Task<bool> ValidateAtExecutionAsync(Guid approvalId, string operationHash, WorkspaceId root, CancellationToken ct);
}
public interface IAgentRunService
{
    Task<RunId> StartAsync(WorkspaceId workspace, string taskText, string profileId, CancellationToken ct);
    Task RequestCancelAsync(RunId run, CancellationToken ct);
    IAsyncEnumerable<OperationEvent> ObserveAsync(RunId run, long afterSequence, CancellationToken ct);
}
public interface IPlatformTerminalService
{
    Task<string> StartAsync(WorkspaceId authorizedWorkspace, string shellId, CancellationToken ct);
    Task ResizeAsync(string terminalId, int columns, int rows, CancellationToken ct);
    Task StopProcessTreeAsync(string terminalId, CancellationToken ct);
}
