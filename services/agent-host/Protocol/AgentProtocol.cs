using System.Text.Json;

namespace ServerRock.AgentHost.Protocol;

public sealed record AgentRequest(int V, string Id, string Method, JsonElement? Params = null);

public sealed record AgentError(string Code, string Message);

public sealed record AgentResponse(int V, string Id, bool Ok, object? Result = null, AgentError? Error = null);

public sealed record RuntimeHealth(
    string Service,
    int ProtocolVersion,
    string Status,
    IReadOnlyList<string> Capabilities);
