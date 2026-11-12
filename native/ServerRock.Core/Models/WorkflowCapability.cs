namespace ServerRock.Core.Models;

public static class WorkflowCapabilities
{
    public const string ReadWorkspace = "workspace.read";
    public const string WriteWorkspace = "workspace.write";
    public const string Network = "network";
    public const string Shell = "shell";
    public const string Git = "git";
}

public sealed class WorkflowCapabilityRequest
{
    public string RunId { get; set; } = "";
    public string NodeId { get; set; } = "";
    public string NodeType { get; set; } = "";
    public List<string> Capabilities { get; set; } = [];
    public string Scope { get; set; } = "";
}

public sealed class WorkflowCapabilityGrant
{
    public string GrantId { get; set; } = Guid.NewGuid().ToString("N");
    public string RunId { get; set; } = "";
    public string NodeId { get; set; } = "";
    public List<string> Capabilities { get; set; } = [];
    public string Scope { get; set; } = "";
    public string ApprovalId { get; set; } = "";
}
