namespace ServerRock.Core.Models;

public sealed record WorkflowEventSearchHit(
    string RunId,
    string WorkflowId,
    int WorkflowVersion,
    WorkflowRunEvent Event);
