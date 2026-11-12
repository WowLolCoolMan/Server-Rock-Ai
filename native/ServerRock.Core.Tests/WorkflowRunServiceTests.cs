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
            Check(run.Steps.Select(step => step.NodeId).SequenceEqual(["test", "approval"]), "durable run stores the graph execution plan order");
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

            var conditionDefinition = new WorkflowDefinition { Id = "condition-flow", Title = "Condition flow", Nodes = [new() { Id = "condition", Type = WorkflowNodeTypes.Condition, Label = "Is ready", Config = new() { ["field"] = "ready", ["equals"] = "yes" } }] };
            var condition = runs.Create(conditionDefinition);
            runs.Queue(condition.RunId); runs.Start(condition.RunId);
            condition = runs.EvaluateCondition(condition.RunId, "condition", new Dictionary<string, string> { ["ready"] = "yes" });
            Check(condition.Steps.Single().Status == WorkflowStepStatuses.Complete && condition.Steps.Single().Result == "true" && condition.Status == WorkflowRunStatuses.Completed, "safe condition executor records a local result without external dispatch");
            var denied = runs.Create(new WorkflowDefinition { Id = "tool-flow", Title = "Tool flow", Nodes = [new() { Id = "tool", Type = WorkflowNodeTypes.ToolCall, Label = "Tool" }] });
            runs.Queue(denied.RunId); runs.Start(denied.RunId);
            var refused = false;
            try { runs.EvaluateCondition(denied.RunId, "tool", new Dictionary<string, string>()); }
            catch (InvalidOperationException error) { refused = error.Message.Contains("condition", StringComparison.OrdinalIgnoreCase); }
            Check(refused, "unsupported workflow node execution is capability-gated");

            var workspaceRoot = Path.Combine(Path.GetTempPath(), $"server-rock-workflow-read-{Guid.NewGuid():N}");
            Directory.CreateDirectory(workspaceRoot);
            try
            {
                File.WriteAllText(Path.Combine(workspaceRoot, "readme.md"), "workflow read");
                var workspaceDefinition = new WorkflowDefinition { Id = "workspace-read-flow", Title = "Workspace read", Nodes = [new() { Id = "read", Type = WorkflowNodeTypes.ToolCall, Label = "Read README" }] };
                var workspaceRun = runs.Create(workspaceDefinition);
                runs.Queue(workspaceRun.RunId); runs.Start(workspaceRun.RunId); runs.BeginStep(workspaceRun.RunId, "read");
                var workspaceGrant = new WorkflowCapabilityGrant { RunId = workspaceRun.RunId, NodeId = "read", Scope = Path.GetFullPath(workspaceRoot), ApprovalId = "approval-read", Capabilities = [WorkflowCapabilities.ReadWorkspace] };
                workspaceRun = runs.ReadWorkspaceStep(workspaceRun.RunId, "read", workspaceRoot, "readme.md", workspaceGrant);
                Check(workspaceRun.Status == WorkflowRunStatuses.Completed && workspaceRun.Steps.Single().Result == "read 13 characters" && runs.Load().Events.Any(item => item.Type == "workspace.read_completed" && item.Payload["mutated"] == "false"), "workflow workspace read records a non-mutating effect event");
            }

            finally
            {
                if (Directory.Exists(workspaceRoot)) Directory.Delete(workspaceRoot, recursive: true);
            }

            var approvalDefinition = new WorkflowDefinition { Id = "approval-inbox-flow", Title = "Approval inbox", Nodes = [new() { Id = "approval", Type = WorkflowNodeTypes.Approval, Label = "Review action" }] };
            var approvalOne = runs.Create(approvalDefinition); runs.Queue(approvalOne.RunId); runs.Start(approvalOne.RunId); runs.BeginStep(approvalOne.RunId, "approval");
            var approvalTwo = runs.Create(new WorkflowDefinition { Id = "approval-inbox-flow-2", Title = "Approval inbox 2", Nodes = [new() { Id = "approval", Type = WorkflowNodeTypes.Approval, Label = "Review second action" }] });
            runs.Queue(approvalTwo.RunId); runs.Start(approvalTwo.RunId); runs.BeginStep(approvalTwo.RunId, "approval");
            Check(runs.LoadPendingApprovals().Count == 2, "concurrent approvals are surfaced as one durable inbox");
        }
        finally
        {
            if (File.Exists(definitionPath)) File.Delete(definitionPath);
            if (File.Exists(runPath)) File.Delete(runPath);
        }
    }
}
