using SkaiDesktop.Core.Models;
using SkaiDesktop.Core.Services;
using static TestSupport;

internal static class WorkflowCapabilityGateTests
{
    public static void Run()
    {
        var request = new WorkflowCapabilityRequest { RunId = "run", NodeId = "tool", NodeType = WorkflowNodeTypes.ToolCall, Scope = "project:demo", Capabilities = [WorkflowCapabilities.ReadWorkspace] };
        var grant = new WorkflowCapabilityGrant { RunId = "run", NodeId = "tool", Scope = "project:demo", ApprovalId = "approval-1", Capabilities = [WorkflowCapabilities.ReadWorkspace, WorkflowCapabilities.Network] };
        WorkflowCapabilityGate.Authorize(request, grant);
        Check(true, "exact scoped capability grant is accepted");
        var mismatch = new WorkflowCapabilityGrant { RunId = "other", NodeId = "tool", Scope = "project:demo", ApprovalId = "approval-1", Capabilities = [WorkflowCapabilities.ReadWorkspace] };
        var refused = false;
        try { WorkflowCapabilityGate.Authorize(request, mismatch); }
        catch (UnauthorizedAccessException) { refused = true; }
        Check(refused, "cross-run capability grant is refused");
        refused = false;
        try { WorkflowCapabilityGate.Authorize(new WorkflowCapabilityRequest { RunId = "run", NodeId = "condition", NodeType = WorkflowNodeTypes.Condition, Scope = "local", Capabilities = [WorkflowCapabilities.Network] }, new WorkflowCapabilityGrant { RunId = "run", NodeId = "condition", Scope = "local", ApprovalId = "approval-2", Capabilities = [WorkflowCapabilities.Network] }); }
        catch (UnauthorizedAccessException) { refused = true; }
        Check(refused, "condition nodes cannot request external capabilities");
    }
}
