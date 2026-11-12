namespace ServerRock.Core.Models;

public sealed record WorkflowApprovalRequest(
    string RunId,
    string WorkflowId,
    int WorkflowVersion,
    string NodeId,
    string Label,
    DateTimeOffset RequestedAt,
    DateTimeOffset ExpiresAt);
