using ServerRock.Core.Models;
using ServerRock.Core.Services;
using static TestSupport;

internal static class McpToolPermissionGateTests
{
    public static void Run()
    {
        var readOnly = new McpToolDescriptor
        {
            ServerId = "test-server",
            ToolName = "list-files",
            Description = "Lists files without changing them.",
            InputSchemaJson = "{\"type\":\"object\"}",
            RequiredCapabilities = [McpCapabilities.InvokeRead]
        };
        McpToolPermissionGate.Authorize(readOnly, new McpToolGrant
        {
            ServerId = "test-server",
            ToolName = "list-files",
            ConnectionId = "connection-1",
            ApprovalId = "approval-1",
            Capabilities = [McpCapabilities.InvokeRead]
        });
        Check(true, "explicit read-only MCP grant is accepted");

        var write = new McpToolDescriptor
        {
            ServerId = "test-server",
            ToolName = "delete-file",
            Description = "Deletes a file.",
            InputSchemaJson = "{\"type\":\"object\"}",
            IsMutating = true
        };
        var refused = false;
        try
        {
            McpToolPermissionGate.Authorize(write, new McpToolGrant
            {
                ServerId = "test-server",
                ToolName = "delete-file",
                ConnectionId = "connection-1",
                ApprovalId = "approval-2",
                Capabilities = [McpCapabilities.InvokeRead]
            });
        }
        catch (UnauthorizedAccessException) { refused = true; }
        Check(refused, "manifest cannot self-authorize a mutating MCP tool");

        refused = false;
        try
        {
            McpToolPermissionGate.Authorize(new McpToolDescriptor
            {
                ServerId = "test-server",
                ToolName = "list-files",
                Description = "Lists files without changing them.",
                InputSchemaJson = "[]",
                RequiredCapabilities = [McpCapabilities.InvokeRead]
            }, new McpToolGrant
            {
                ServerId = "test-server",
                ToolName = "list-files",
                ConnectionId = "connection-1",
                ApprovalId = "approval-3",
                Capabilities = [McpCapabilities.InvokeRead]
            });
        }
        catch (UnauthorizedAccessException) { refused = true; }
        Check(refused, "invalid MCP schema is refused before execution");
    }
}
