using SkaiDesktop.Core.Models;
using SkaiDesktop.Core.Services;
using static TestSupport;

internal static class WorkflowServiceTests
{
    public static void Run()
    {
        var workflow = new WorkflowDefinition
        {
            Id = "audit",
            Title = "Audit project",
            TemplateOnly = true,
            Nodes =
            [
                new() { Id = "prompt", Type = WorkflowNodeTypes.AgentPrompt, Label = "Inspect" },
                new() { Id = "approval", Type = WorkflowNodeTypes.Approval, Label = "Review" },
                new() { Id = "test", Type = WorkflowNodeTypes.Test, Label = "Verify" }
            ],
            Edges = [new() { From = "prompt", To = "approval" }, new() { From = "approval", To = "test" }]
        };
        var path = Path.Combine(Path.GetTempPath(), $"server-rock-workflow-{Guid.NewGuid():N}.json");
        var service = new WorkflowService(path);
        try
        {
            Check(service.Validate(workflow).Count == 0, "valid workflow DAG passes validation");
            Check(service.BuildExecutionPlan(workflow).Select(step => step.NodeId).SequenceEqual(["prompt", "approval", "test"]), "execution plan follows dependency order");
            service.Save(workflow);
            Check(service.Load().Single().Version == 1, "workflow version persists atomically");
            Check(service.Validate(new WorkflowDefinition { Id = "cycle", Title = "Cycle", Nodes = [new() { Id = "a", Type = WorkflowNodeTypes.Test, Label = "A" }, new() { Id = "b", Type = WorkflowNodeTypes.Test, Label = "B" }], Edges = [new() { From = "a", To = "b" }, new() { From = "b", To = "a" }]}).Any(error => error.Contains("cycle", StringComparison.OrdinalIgnoreCase)), "cycles are rejected");
            Check(service.Validate(new WorkflowDefinition { Id = "secret", Title = "Secret", Nodes = [new() { Id = "a", Type = WorkflowNodeTypes.ToolCall, Label = "Call", Config = new() { ["api_token"] = "inline" } }]}).Any(error => error.Contains("Secrets", StringComparison.Ordinal)), "inline secret configuration is rejected");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
