using SkaiDesktop.Core.Infrastructure;
using SkaiDesktop.Core.Models;

namespace SkaiDesktop.Core.Services;

/// <summary>Read-only workspace adapter behind the exact capability gate.</summary>
public sealed class ReadOnlyWorkspaceAdapter
{
    private readonly WorkspaceFileEditor _editor = new();

    public (string Content, WorkspaceReadEffect Effect) ReadText(
        string runId,
        string nodeId,
        string selectedRoot,
        string relativePath,
        WorkflowCapabilityGrant grant)
    {
        var root = Path.GetFullPath(selectedRoot);
        WorkflowCapabilityGate.Authorize(new WorkflowCapabilityRequest
        {
            RunId = runId,
            NodeId = nodeId,
            NodeType = WorkflowNodeTypes.ToolCall,
            Scope = root,
            Capabilities = [WorkflowCapabilities.ReadWorkspace]
        }, grant);
        var content = _editor.ReadText(root, relativePath);
        return (content, new WorkspaceReadEffect(nodeId, relativePath, content.Length, Mutated: false));
    }
}
