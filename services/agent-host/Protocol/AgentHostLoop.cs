using System.Text.Json;
using ServerRock.Core.Infrastructure;
using ServerRock.Core.Models;
using ServerRock.Core.Services;

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
                ["runtime.health", "models.list", "projects.list", "tasks.list", "scheduled.list", "chat.complete"])),
            "models.list" => ListModels(request),
            "projects.list" => ListProjects(request),
            "tasks.list" => ListTasks(request),
            "scheduled.list" => ListScheduledTasks(request),
            "chat.complete" => CompleteChatAsync(request).GetAwaiter().GetResult(),
            _ => new(ProtocolVersion, request.Id, false, Error: new("METHOD_NOT_FOUND", $"Unknown method '{request.Method}'.")),
        };
    }

    private static AgentResponse ListModels(AgentRequest request)
    {
        var endpoint = RequiredString(request, "endpoint");
        var provider = new OpenAiCompatibleProvider();
        var models = provider.DiscoverModelProfilesAsync(endpoint, CancellationToken.None, OptionalString(request, "apiKey"))
            .GetAwaiter().GetResult();
        return new(ProtocolVersion, request.Id, true, models);
    }

    private static AgentResponse ListProjects(AgentRequest request)
    {
        var projects = new ProjectStore().Load()
            .OrderByDescending(project => project.UpdatedAt)
            .Select(project => new
            {
                id = project.Id,
                name = project.Name,
                instructions = project.Instructions,
                folderPaths = project.FolderPaths,
                gitHubRepository = project.GitHubRepository,
                updatedAt = project.UpdatedAt,
            })
            .ToArray();
        return new(ProtocolVersion, request.Id, true, projects);
    }

    private static async Task<AgentResponse> CompleteChatAsync(AgentRequest request)
    {
        var endpoint = RequiredString(request, "endpoint");
        var model = RequiredString(request, "model");
        var messages = request.Params?.GetProperty("messages").Deserialize<List<ChatMessage>>(JsonOptions)
            ?? throw new InvalidOperationException("chat.complete requires a messages array.");
        if (messages.Count == 0) throw new InvalidOperationException("chat.complete requires at least one message.");

        var settings = new AppSettings { Endpoint = endpoint, Model = model, Temperature = OptionalDouble(request, "temperature") ?? 0.7 };
        var text = new System.Text.StringBuilder();
        await new OpenAiCompatibleProvider().StreamChatAsync(settings, messages, chunk => text.Append(chunk), CancellationToken.None,
            OptionalInt(request, "maxTokens"), OptionalString(request, "apiKey"));
        return new(ProtocolVersion, request.Id, true, new { model, content = text.ToString() });
    }

    private static AgentResponse ListTasks(AgentRequest request)
    {
        var tasks = new AgentTaskArtifactStore().Load()
            .OrderByDescending(task => task.UpdatedAtUtc)
            .Select(task => new
            {
                id = task.Id,
                title = task.Title,
                role = task.Role,
                status = task.Status,
                resultSummary = task.ResultSummary,
                dependsOn = task.DependsOn,
                updatedAt = task.UpdatedAtUtc,
            })
            .ToArray();
        return new(ProtocolVersion, request.Id, true, tasks);
    }

    private static AgentResponse ListScheduledTasks(AgentRequest request)
    {
        var tasks = new ScheduledTaskStore().Load()
            .OrderBy(task => task.DueAtUtc)
            .Select(task => new
            {
                id = task.Id,
                prompt = task.Prompt,
                workspaceMode = task.WorkspaceMode,
                dueAt = task.DueAtUtc,
                repeatEveryMinutes = task.RepeatEveryMinutes,
                status = task.Status,
                outcome = task.Outcome,
            })
            .ToArray();
        return new(ProtocolVersion, request.Id, true, tasks);
    }

    private static string RequiredString(AgentRequest request, string name)
    {
        var value = OptionalString(request, name);
        return string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException($"{name} is required.") : value;
    }

    private static string? OptionalString(AgentRequest request, string name) =>
        request.Params is { } parameters && parameters.TryGetProperty(name, out var value) ? value.GetString() : null;

    private static double? OptionalDouble(AgentRequest request, string name) =>
        request.Params is { } parameters && parameters.TryGetProperty(name, out var value) && value.TryGetDouble(out var result) ? result : null;

    private static int? OptionalInt(AgentRequest request, string name) =>
        request.Params is { } parameters && parameters.TryGetProperty(name, out var value) && value.TryGetInt32(out var result) ? result : null;
}
