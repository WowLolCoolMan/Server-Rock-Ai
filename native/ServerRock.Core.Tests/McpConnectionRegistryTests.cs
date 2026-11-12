using ServerRock.Core.Models;
using ServerRock.Core.Services;
using static TestSupport;

internal static class McpConnectionRegistryTests
{
    public static void Run()
    {
        var registry = new McpConnectionRegistry();
        var configured = registry.Configure("connection-1", "test-server", "stdio", "sha256:manifest");
        Check(configured.State == McpConnectionStates.Configured, "MCP connection starts configured");
        registry.MarkHealthy("connection-1");
        var descriptor = new McpToolDescriptor { ServerId = "test-server", ToolName = "list-files", Description = "Lists files.", InputSchemaJson = "{}", RequiredCapabilities = [McpCapabilities.InvokeRead] };
        registry.Authorize(descriptor, new McpToolGrant { ServerId = "test-server", ToolName = "list-files", ConnectionId = "connection-1", ApprovalId = "approval-1", Capabilities = [McpCapabilities.InvokeRead] });
        Check(true, "healthy MCP connection permits an explicitly granted tool");
        registry.Revoke("connection-1");
        var refused = false;
        try { registry.Authorize(descriptor, new McpToolGrant { ServerId = "test-server", ToolName = "list-files", ConnectionId = "connection-1", ApprovalId = "approval-1", Capabilities = [McpCapabilities.InvokeRead] }); }
        catch (UnauthorizedAccessException) { refused = true; }
        Check(refused && registry.Get("connection-1").State == McpConnectionStates.Revoked, "revoked MCP connection denies new calls");
        refused = false;
        try { registry.MarkHealthy("connection-1"); }
        catch (InvalidOperationException) { refused = true; }
        Check(refused, "revoked MCP connection cannot be silently reactivated");
    }
}
