using ServerRock.Core.Models;
using ServerRock.Core.Services;
using static TestSupport;

internal static class WorkflowRunServiceTests
{
    public static void Run()
    {
        var definitionPath = Path.Combine(Path.GetTempPath(), $"server-rock-workflow-def-{Guid.NewGuid():N}.json");
        var runPath = Path.Combine(Path.GetTempPath(), $"server-rock-workflow-run-{Guid.NewGuid():N}.json");
        var definition = new WorkflowDefinition
        {
            Id = "safe-flow", Title = "Safe flow", Version = 1,
            Nodes = [new() { Id = "test", Type = WorkflowNodeTypes.Test, Label = "Test" }, new() { Id = "approval", Type = WorkflowNodeTypes.Approval, Label = "Approve" }],
            Edges = [new() { From = "test", To = "approval" }]
        };
        try
        {
            var definitions = new WorkflowService(definitionPath);
            var runs = new WorkflowRunService(definitions, runPath);
            var run = runs.Create(definition);
            run = runs.Queue(run.RunId);
            run = runs.Start(run.RunId);
            run = runs.BeginStep(run.RunId, "test");
            run = runs.CompleteStep(run.RunId, "test");
            run = runs.BeginStep(run.RunId, "approval");
            Check(run.Status == WorkflowRunStatuses.AwaitingApproval, "approval node pauses the run");
            run = runs.ApproveStep(run.RunId, "approval", "approval-1");
            Check(run.Status == WorkflowRunStatuses.Completed && run.Steps.Single(step => step.NodeId == "approval").Status == WorkflowStepStatuses.Complete, "approval completes durably");

            var retryDefinition = new WorkflowDefinition { Id = "retry-flow", Title = "Retry flow", Nodes = [new() { Id = "test", Type = WorkflowNodeTypes.Test, Label = "Test" }] };
            var retry = runs.Create(retryDefinition);
            runs.Queue(retry.RunId); runs.Start(retry.RunId); runs.BeginStep(retry.RunId, "test"); runs.FailStep(retry.RunId, "test");
            retry = runs.RetryStep(retry.RunId, "test");
            Check(retry.Steps.Single().Attempt == 2, "only safe steps can retry with a new attempt");

            var interrupted = runs.Create(definition); runs.Queue(interrupted.RunId); runs.Start(interrupted.RunId);
            Check(runs.ReconcileAfterRestart().Any(item => item.RunId == interrupted.RunId && item.Status == WorkflowRunStatuses.NeedsReconciliation), "in-flight workflow runs reconcile after restart");
        }
        finally
        {
            if (File.Exists(definitionPath)) File.Delete(definitionPath);
            if (File.Exists(runPath)) File.Delete(runPath);
        }
    }
}
