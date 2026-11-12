using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ServerRock.Core.Models;
using ServerRock.Core.Services;
using static TestSupport;

internal static class McpStreamableHttpClientTests
{
    public static async Task RunAsync()
    {
        var sessionHeaders = new List<(string? Session, string? Protocol)>();
        var registry = new McpConnectionRegistry();
        registry.Configure("http-connection", "http-server", "streamable-http");
        registry.MarkHealthy("http-connection");
        var handler = new StubHandler(async (request, _) =>
        {
            using var body = await request.Content!.ReadAsStreamAsync();
            using var message = await JsonDocument.ParseAsync(body);
            var method = message.RootElement.GetProperty("method").GetString();
            var id = message.RootElement.GetProperty("id").GetInt32();
            if (method != "initialize") sessionHeaders.Add((request.Headers.GetValues("Mcp-Session-Id").SingleOrDefault(), request.Headers.GetValues("MCP-Protocol-Version").SingleOrDefault()));
            var responseJson = method switch
            {
                "initialize" => $"{{\"jsonrpc\":\"2.0\",\"id\":{id},\"result\":{{\"protocolVersion\":\"2025-06-18\"}}}}",
                "tools/list" => $"{{\"jsonrpc\":\"2.0\",\"id\":{id},\"result\":{{\"tools\":[{{\"name\":\"list-files\",\"description\":\"Lists files.\",\"inputSchema\":{{\"type\":\"object\"}},\"annotations\":{{\"readOnlyHint\":true}}}}]}}}}",
                _ => $"{{\"jsonrpc\":\"2.0\",\"id\":{id},\"result\":{{\"content\":[{{\"type\":\"text\",\"text\":\"http result\"}}]}}}}"
            };
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(method == "tools/call" ? $"event: message\n\ndata: {responseJson}\n\n" : responseJson, System.Text.Encoding.UTF8, method == "tools/call" ? "text/event-stream" : "application/json") };
            if (method == "initialize") httpResponse.Headers.TryAddWithoutValidation("Mcp-Session-Id", "session-1");
            return httpResponse;
        });
        using var http = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        await using var client = new McpStreamableHttpClient("http-server", "https://example.com/mcp", http, null, registry);
        await client.InitializeAsync();
        var tools = await client.ListToolsAsync();
        using var arguments = JsonDocument.Parse("{}");
        var result = await client.CallToolAsync(tools[0], new McpToolGrant { ServerId = "http-server", ToolName = "list-files", ConnectionId = "http-connection", ApprovalId = "approval-http", Capabilities = [McpCapabilities.InvokeRead] }, arguments.RootElement);
        Check(!result.IsError && result.Text == "http result", "MCP Streamable HTTP invokes an approved read-only tool from SSE");
        Check(tools.Count == 1 && !tools[0].IsMutating, "MCP Streamable HTTP classifies explicit read-only metadata");
        Check(sessionHeaders.Count == 2 && sessionHeaders.All(item => item.Session == "session-1" && item.Protocol == "2025-06-18"), "MCP Streamable HTTP preserves the negotiated session and protocol headers");

        registry.Revoke("http-connection");
        var refused = false;
        try { await client.CallToolAsync(tools[0], new McpToolGrant { ServerId = "http-server", ToolName = "list-files", ConnectionId = "http-connection", ApprovalId = "approval-http-2", Capabilities = [McpCapabilities.InvokeRead] }, arguments.RootElement); }
        catch (UnauthorizedAccessException) { refused = true; }
        Check(refused, "MCP Streamable HTTP blocks a revoked connection before request");

        var redirectClient = new McpStreamableHttpClient("redirect-server", "https://example.com/mcp", new HttpClient(new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Redirect) { Headers = { Location = new Uri("https://other.example/mcp") } }))));
        var redirectRefused = false;
        try { await redirectClient.InitializeAsync(); }
        catch (HttpRequestException) { redirectRefused = true; }
        await redirectClient.DisposeAsync();
        Check(redirectRefused, "MCP Streamable HTTP refuses endpoint redirects");
    }
}
