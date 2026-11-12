namespace ServerRock.Core.Models;

public static class McpCapabilities
{
    public const string InvokeRead = "mcp.invoke.read";
    public const string InvokeWrite = "mcp.invoke.write";
}

/// <summary>Untrusted metadata received from an MCP server. It never grants permission.</summary>
public sealed class McpToolDescriptor
{
    public string ServerId { get; set; } = "";
    public string ToolName { get; set; } = "";
    public string Description { get; set; } = "";
    public string InputSchemaJson { get; set; } = "{}";
    public bool IsMutating { get; set; }
    public List<string> RequiredCapabilities { get; set; } = [];
}

/// <summary>Explicit local approval for a known MCP server and tool.</summary>
public sealed class McpToolGrant
{
    public string ServerId { get; set; } = "";
    public string ToolName { get; set; } = "";
    public string ConnectionId { get; set; } = "";
    public string ApprovalId { get; set; } = "";
    public List<string> Capabilities { get; set; } = [];
}

public sealed record McpToolCallResult(bool IsError, string Text);
