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
                ["runtime.health", "models.list", "projects.list", "project.save", "tasks.list", "tasks.queue", "tasks.cancel", "tasks.retry", "scheduled.list", "conversations.list", "conversation.save", "settings.get", "settings.save", "image.generate", "chat.complete"])),
            "models.list" => ListModels(request),
            "projects.list" => ListProjects(request),
            "project.save" => SaveProject(request),
            "tasks.list" => ListTasks(request),
            "tasks.queue" => TransitionTask(request, "queue"),
            "tasks.cancel" => TransitionTask(request, "cancel"),
            "tasks.retry" => TransitionTask(request, "retry"),
            "scheduled.list" => ListScheduledTasks(request),
            "conversations.list" => ListConversations(request),
            "conversation.save" => SaveConversation(request),
            "settings.get" => GetSettings(request),
            "settings.save" => SaveSettings(request),
            "image.generate" => GenerateImageAsync(request).GetAwaiter().GetResult(),
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
        var projects = CreateProjectStore().Load()
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

    private static AgentResponse SaveProject(AgentRequest request)
    {
        var name = RequiredBoundedString(request, "name", 200);
        var instructions = OptionalString(request, "instructions") ?? string.Empty;
        if (instructions.Length > 500_000 || instructions.Any(char.IsControl)) throw new InvalidOperationException("Project instructions exceed the safety limit.");
        var store = CreateProjectStore();
        var projects = store.Load();
        var id = OptionalString(request, "id");
        var project = string.IsNullOrWhiteSpace(id) ? new LocalProject { Name = name } : projects.SingleOrDefault(item => item.Id == id)
            ?? throw new KeyNotFoundException("The project does not exist.");
        project.Name = name;
        project.Instructions = instructions;
        project.UpdatedAt = DateTimeOffset.UtcNow;
        if (projects.All(item => item.Id != project.Id)) projects.Add(project);
        store.Save(projects);
        return new(ProtocolVersion, request.Id, true, new { id = project.Id, name = project.Name, updatedAt = project.UpdatedAt });
    }

    private static AgentResponse ListTasks(AgentRequest request)
    {
        var tasks = CreateTaskStore().Load()
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

    private static AgentResponse TransitionTask(AgentRequest request, string operation)
    {
        var id = RequiredBoundedString(request, "id", 128);
        var store = CreateTaskStore();
        var task = operation switch
        {
            "queue" => store.Queue(id),
            "cancel" => store.CancelPlanned(id),
            "retry" => store.RetryBlocked(id),
            _ => throw new InvalidOperationException("Unknown task operation."),
        };
        return new(ProtocolVersion, request.Id, true, new { id = task.Id, status = task.Status, updatedAt = task.UpdatedAtUtc, resultSummary = task.ResultSummary });
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

    private static AgentResponse ListConversations(AgentRequest request)
    {
        var conversations = CreateConversationStore().Load()
            .Where(session => !session.Archived)
            .OrderByDescending(session => session.Pinned)
            .ThenByDescending(session => session.UpdatedAt)
            .Select(session => new
            {
                id = session.Id,
                title = session.Title,
                messages = session.Messages,
                updatedAt = session.UpdatedAt,
                pinned = session.Pinned,
                tags = session.Tags,
            })
            .ToArray();
        return new(ProtocolVersion, request.Id, true, conversations);
    }

    private static AgentResponse SaveConversation(AgentRequest request)
    {
        var title = RequiredBoundedString(request, "title", 200);
        var messages = request.Params?.TryGetProperty("messages", out var messageValue) == true
            ? messageValue.Deserialize<List<ChatMessage>>(JsonOptions)
            : throw new InvalidOperationException("conversation.save requires a messages array.");
        if (messages is null) throw new InvalidOperationException("conversation.save requires a messages array.");
        var store = CreateConversationStore();
        var sessions = store.Load();
        var requestedId = OptionalString(request, "id");
        var session = string.IsNullOrWhiteSpace(requestedId)
            ? new ChatSession { Title = title, Messages = messages }
            : sessions.SingleOrDefault(item => item.Id == requestedId)
              ?? throw new InvalidOperationException("The requested conversation does not exist.");
        session.Title = title;
        session.Messages = messages;
        session.UpdatedAt = DateTimeOffset.UtcNow;
        if (!sessions.Contains(session)) sessions.Add(session);
        store.Save(sessions);
        return new(ProtocolVersion, request.Id, true, new { id = session.Id, title = session.Title, updatedAt = session.UpdatedAt });
    }

    private static ConversationStore CreateConversationStore() =>
        new(Environment.GetEnvironmentVariable("SRAI_CONVERSATIONS_PATH"));

    private static AgentResponse GetSettings(AgentRequest request)
    {
        var settings = CreateSettingsStore().Load();
        return new(ProtocolVersion, request.Id, true, new
        {
            endpoint = settings.Endpoint,
            model = settings.Model,
            themeMode = settings.ThemeMode,
            accentColor = settings.AccentColor,
            interfaceDensity = settings.InterfaceDensity,
            backdropPreset = settings.BackdropPreset,
            reduceMotion = settings.ReduceMotion,
            developerMode = settings.DeveloperMode,
        });
    }

    private static AgentResponse SaveSettings(AgentRequest request)
    {
        var store = CreateSettingsStore();
        var settings = store.Load();
        settings.Endpoint = RequiredBoundedString(request, "endpoint", 2_000);
        settings.Model = RequiredBoundedString(request, "model", 256);
        if (request.Params is { } parameters)
        {
            if (parameters.TryGetProperty("themeMode", out var theme) && theme.ValueKind == JsonValueKind.String)
                settings.ThemeMode = theme.GetString() ?? settings.ThemeMode;
            if (parameters.TryGetProperty("reduceMotion", out var reduceMotion) && reduceMotion.ValueKind is JsonValueKind.True or JsonValueKind.False)
                settings.ReduceMotion = reduceMotion.GetBoolean();
        }
        store.Save(settings);
        return new(ProtocolVersion, request.Id, true, new { endpoint = settings.Endpoint, model = settings.Model });
    }

    private static async Task<AgentResponse> GenerateImageAsync(AgentRequest request)
    {
        var endpoint = RequiredBoundedString(request, "endpoint", 512);
        var workflow = RequiredBoundedString(request, "workflow", 1_000_000);
        var prompt = RequiredBoundedString(request, "prompt", 8_000);
        var progress = string.Empty;
        var images = await new ComfyImageProvider().GenerateAsync(endpoint, workflow, prompt, message => progress = message, CancellationToken.None);
        return new(ProtocolVersion, request.Id, true, new
        {
            prompt,
            progress,
            images = images.Select(image => new { url = image.Url.ToString(), fileName = image.FileName }).ToArray(),
        });
    }

    private static JsonSettingsStore CreateSettingsStore() =>
        new(Environment.GetEnvironmentVariable("SRAI_SETTINGS_PATH"));

    private static ProjectStore CreateProjectStore() =>
        new(Environment.GetEnvironmentVariable("SRAI_PROJECTS_PATH"));

    private static AgentTaskArtifactStore CreateTaskStore() =>
        new(Environment.GetEnvironmentVariable("SRAI_TASKS_PATH"));

    private static string RequiredString(AgentRequest request, string name)
    {
        var value = OptionalString(request, name);
        return string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException($"{name} is required.") : value;
    }

    private static string RequiredBoundedString(AgentRequest request, string name, int maxLength)
    {
        var value = RequiredString(request, name);
        if (value.Length > maxLength || value.Any(char.IsControl)) throw new InvalidOperationException($"{name} exceeds its safety limit.");
        return value.Trim();
    }

    private static string? OptionalString(AgentRequest request, string name) =>
        request.Params is { } parameters && parameters.TryGetProperty(name, out var value) ? value.GetString() : null;

    private static double? OptionalDouble(AgentRequest request, string name) =>
        request.Params is { } parameters && parameters.TryGetProperty(name, out var value) && value.TryGetDouble(out var result) ? result : null;

    private static int? OptionalInt(AgentRequest request, string name) =>
        request.Params is { } parameters && parameters.TryGetProperty(name, out var value) && value.TryGetInt32(out var result) ? result : null;
}
