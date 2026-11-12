using System.Text.Json;
using ServerRock.Core.Models;

namespace ServerRock.Core.Services;

/// <summary>Authorizes MCP tool calls using explicit local grants. Server manifests are advisory only.</summary>
public static class McpToolPermissionGate
{
    private static readonly IReadOnlySet<string> KnownCapabilities = new HashSet<string>(StringComparer.Ordinal)
    {
        McpCapabilities.InvokeRead,
        McpCapabilities.InvokeWrite
    };

    public static void Authorize(McpToolDescriptor descriptor, McpToolGrant grant)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(grant);
        ValidateText(descriptor.ServerId, 128, "server identity");
        ValidateText(descriptor.ToolName, 128, "tool name");
        ValidateText(descriptor.Description, 4_000, "tool description");
        ValidateText(descriptor.InputSchemaJson, 32_000, "input schema");
        ValidateText(grant.ServerId, 128, "grant server identity");
        ValidateText(grant.ToolName, 128, "grant tool name");
        ValidateText(grant.ConnectionId, 128, "connection identity");
        ValidateText(grant.ApprovalId, 128, "approval identity");
        if (!string.Equals(descriptor.ServerId, grant.ServerId, StringComparison.Ordinal) || !string.Equals(descriptor.ToolName, grant.ToolName, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("The MCP grant does not match the advertised server and tool.");
        if (grant.Capabilities.Count == 0 || grant.Capabilities.Count > 4 || grant.Capabilities.Any(capability => !KnownCapabilities.Contains(capability)))
            throw new UnauthorizedAccessException("The MCP grant contains an unsupported capability.");
        if (descriptor.RequiredCapabilities.Count > 4 || descriptor.RequiredCapabilities.Any(capability => !KnownCapabilities.Contains(capability)))
            throw new UnauthorizedAccessException("The MCP manifest requests an unsupported capability.");
        if (!descriptor.RequiredCapabilities.All(grant.Capabilities.Contains))
            throw new UnauthorizedAccessException("The explicit MCP grant does not cover the tool manifest requirements.");
        if (descriptor.IsMutating && !grant.Capabilities.Contains(McpCapabilities.InvokeWrite))
            throw new UnauthorizedAccessException("Mutating MCP tools require explicit write approval.");
        if (!descriptor.IsMutating && !grant.Capabilities.Contains(McpCapabilities.InvokeRead))
            throw new UnauthorizedAccessException("Read-only MCP tools require explicit read approval.");
        ValidateSchema(descriptor.InputSchemaJson);
    }

    private static void ValidateSchema(string schemaJson)
    {
        try
        {
            using var document = JsonDocument.Parse(schemaJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new UnauthorizedAccessException("The MCP input schema must be a JSON object.");
        }
        catch (JsonException exception)
        {
            throw new UnauthorizedAccessException("The MCP input schema is not valid JSON.", exception);
        }
    }

    private static void ValidateText(string value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength || value.Any(char.IsControl))
            throw new ArgumentException($"The {label} is invalid.");
    }
}
