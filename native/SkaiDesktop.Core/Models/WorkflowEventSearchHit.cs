namespace SkaiDesktop.Core.Models;

public sealed record WorkflowEventSearchHit(
    string RunId,
    string WorkflowId,
    int WorkflowVersion,
    WorkflowRunEvent Event);
