using System.Text.Json;

namespace ServerRock.AgentHost.Protocol;

public static class AgentHostLoop
{
    private const int ProtocolVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task RunAsync(Stream input, Stream output, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(input);
        await using var writer = new StreamWriter(output) { AutoFlush = true };

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null) break;
            if (string.IsNullOrWhiteSpace(line)) continue;

            AgentResponse response;
            try
            {
                var request = JsonSerializer.Deserialize<AgentRequest>(line, JsonOptions)
                    ?? throw new InvalidOperationException("Request body was empty.");
                response = Handle(request);
            }
            catch (JsonException ex)
            {
                response = new(ProtocolVersion, string.Empty, false, Error: new("INVALID_JSON", ex.Message));
            }
            catch (Exception ex)
            {
                response = new(ProtocolVersion, string.Empty, false, Error: new("REQUEST_FAILED", ex.Message));
            }

            await writer.WriteLineAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
    }

    private static AgentResponse Handle(AgentRequest request)
    {
        if (request.V != ProtocolVersion)
            return new(ProtocolVersion, request.Id, false, Error: new("UNSUPPORTED_PROTOCOL", $"Protocol version {request.V} is not supported."));

        return request.Method switch
        {
            "runtime.health" => new(ProtocolVersion, request.Id, true, new RuntimeHealth(
                "agent-host",
                ProtocolVersion,
                "ready",
                ["runtime.health"])),
            _ => new(ProtocolVersion, request.Id, false, Error: new("METHOD_NOT_FOUND", $"Unknown method '{request.Method}'.")),
        };
    }
}
