namespace ServerRock.Core.Models;

public static class WorkflowNodeTypes
{
    public const string AgentPrompt = "agent_prompt";
    public const string ToolCall = "tool_call";
    public const string Condition = "condition";
    public const string Approval = "approval";
    public const string Test = "test";
    public const string ParallelGroup = "parallel_group";
    public const string Join = "join";
    public const string Artifact = "artifact";
    public const string Notification = "notification";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        AgentPrompt, ToolCall, Condition, Approval, Test, ParallelGroup, Join, Artifact, Notification
    };
}

public sealed class WorkflowDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "New workflow";
    public int Version { get; set; } = 1;
    public bool TemplateOnly { get; set; } = true;
    public string Status { get; set; } = "draft";
    public List<WorkflowNode> Nodes { get; set; } = [];
    public List<WorkflowEdge> Edges { get; set; } = [];
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class WorkflowNode
{
    public string Id { get; set; } = "node";
    public string Type { get; set; } = WorkflowNodeTypes.AgentPrompt;
    public string Label { get; set; } = "Step";
    public Dictionary<string, string> Config { get; set; } = new(StringComparer.Ordinal);
}

public sealed class WorkflowEdge
{
    public string From { get; set; } = "";
    public string To { get; set; } = "";
}

public sealed class WorkflowPlanStep
{
    public string NodeId { get; set; } = "";
    public string NodeType { get; set; } = "";
    public string Label { get; set; } = "";
    public List<string> DependsOn { get; set; } = [];
}
