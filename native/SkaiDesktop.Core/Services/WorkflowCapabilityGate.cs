using SkaiDesktop.Core.Models;

namespace SkaiDesktop.Core.Services;

/// <summary>Exact-scope permission contract for future external adapters. This
/// gate authorizes metadata only; callers still need a separately reviewed
/// executor implementation.</summary>
public static class WorkflowCapabilityGate
{
    private static readonly IReadOnlySet<string> KnownCapabilities = new HashSet<string>(StringComparer.Ordinal)
    {
        WorkflowCapabilities.ReadWorkspace, WorkflowCapabilities.WriteWorkspace,
        WorkflowCapabilities.Network, WorkflowCapabilities.Shell, WorkflowCapabilities.Git
    };

    public static void Authorize(WorkflowCapabilityRequest request, WorkflowCapabilityGrant grant)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(grant);
        ValidateText(request.RunId, 128, "run identity");
        ValidateText(request.NodeId, 128, "node identity");
        ValidateText(request.NodeType, 64, "node type");
        ValidateText(request.Scope, 4_000, "capability scope");
        ValidateText(grant.RunId, 128, "grant run identity");
        ValidateText(grant.NodeId, 128, "grant node identity");
        ValidateText(grant.Scope, 4_000, "grant scope");
        ValidateText(grant.ApprovalId, 128, "approval identity");
        if (!string.Equals(request.RunId, grant.RunId, StringComparison.Ordinal) || !string.Equals(request.NodeId, grant.NodeId, StringComparison.Ordinal) || !string.Equals(request.Scope, grant.Scope, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Capability grant scope does not match the requested run, node, and scope.");
        if (request.Capabilities.Count == 0 || request.Capabilities.Count > 16 || grant.Capabilities.Count == 0 || grant.Capabilities.Count > 16)
            throw new UnauthorizedAccessException("A capability request and grant must contain 1-16 capabilities.");
        if (request.Capabilities.Any(capability => !KnownCapabilities.Contains(capability)) || grant.Capabilities.Any(capability => !KnownCapabilities.Contains(capability)))
            throw new UnauthorizedAccessException("The capability is not in the supported allowlist.");
        if (!request.Capabilities.All(grant.Capabilities.Contains)) throw new UnauthorizedAccessException("The grant does not cover every requested capability.");
        if (string.IsNullOrWhiteSpace(grant.ApprovalId)) throw new UnauthorizedAccessException("External capabilities require an explicit approval identity.");
        if (string.Equals(request.NodeType, WorkflowNodeTypes.Condition, StringComparison.Ordinal)) throw new UnauthorizedAccessException("Pure condition nodes cannot request external capabilities.");
    }

    private static void ValidateText(string value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength || value.Any(char.IsControl)) throw new ArgumentException($"The {label} is invalid.");
    }
}
