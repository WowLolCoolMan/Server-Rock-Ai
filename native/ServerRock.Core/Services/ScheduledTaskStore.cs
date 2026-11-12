using System.Text.Json;
using ServerRock.Core.Infrastructure;
using ServerRock.Core.Models;

namespace ServerRock.Core.Services;

/// <summary>Durable one-shot scheduler state. Claiming is persisted before execution to prevent replay.</summary>
public sealed class ScheduledTaskStore
{
    public const int MaxTasks = 256;
    public const int MaxPromptCharacters = 50_000;
    public const int MaxOutcomeCharacters = 2_000;
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true, MaxDepth = 16 };
    private readonly string _path;

    public ScheduledTaskStore(string? path = null)
    {
        _path = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ServerRock", "AI", "scheduled-tasks.json");
    }

    public IReadOnlyList<ScheduledTask> Load()
    {
        if (!File.Exists(_path)) return [];
        try
        {
            var tasks = JsonSerializer.Deserialize<List<ScheduledTask>>(BoundedFileReader.ReadText(_path), Options) ?? [];
            Validate(tasks);
            return tasks;
        }
        catch (JsonException error)
        {
            throw new InvalidDataException("Scheduled tasks could not be read. The original file was left untouched.", error);
        }
    }

    public ScheduledTask Schedule(string prompt, DateTimeOffset dueAtUtc, string workspaceMode = WorkspaceModes.Chat, DateTimeOffset? now = null)
    {
        ValidatePrompt(prompt);
        if (workspaceMode is not (WorkspaceModes.Chat or WorkspaceModes.CodingAgent)) throw new ArgumentException("The scheduled workspace mode is invalid.", nameof(workspaceMode));
        var current = now ?? DateTimeOffset.UtcNow;
        if (dueAtUtc < current.AddSeconds(-5)) throw new ArgumentException("A scheduled task must be in the future.", nameof(dueAtUtc));
        if (dueAtUtc > current.AddYears(1)) throw new ArgumentException("A scheduled task cannot be more than one year away.", nameof(dueAtUtc));
        var tasks = Load().ToList();
        if (tasks.Count >= MaxTasks) throw new InvalidOperationException($"At most {MaxTasks} scheduled tasks can be saved.");
        var task = new ScheduledTask { Prompt = prompt.Trim(), WorkspaceMode = workspaceMode, DueAtUtc = dueAtUtc, CreatedAtUtc = current };
        tasks.Add(task);
        Save(tasks);
        return task;
    }

    public ScheduledTask? TryClaimDue(DateTimeOffset? now = null)
    {
        var tasks = Load().ToList();
        var current = now ?? DateTimeOffset.UtcNow;
        var task = tasks.Where(item => item.Status == ScheduledTaskStatuses.Scheduled && item.DueAtUtc <= current)
            .OrderBy(item => item.DueAtUtc).ThenBy(item => item.CreatedAtUtc).FirstOrDefault();
        if (task is null) return null;
        task.Status = ScheduledTaskStatuses.Running;
        task.StartedAtUtc = current;
        Save(tasks);
        return task;
    }

    public void Complete(string id, bool succeeded, string outcome, DateTimeOffset? now = null)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A scheduled task ID is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(outcome) || outcome.Length > MaxOutcomeCharacters || outcome.Any(char.IsControl)) throw new ArgumentException("The task outcome is invalid.", nameof(outcome));
        var tasks = Load().ToList();
        var task = tasks.SingleOrDefault(item => item.Id == id) ?? throw new InvalidOperationException("The scheduled task no longer exists.");
        if (task.Status != ScheduledTaskStatuses.Running) throw new InvalidOperationException("Only a claimed scheduled task can be completed.");
        task.Status = succeeded ? ScheduledTaskStatuses.Completed : ScheduledTaskStatuses.Failed;
        task.CompletedAtUtc = now ?? DateTimeOffset.UtcNow;
        task.Outcome = outcome.Trim();
        Save(tasks);
    }

    public void Pause(string id)
    {
        UpdateStatus(id, ScheduledTaskStatuses.Scheduled, ScheduledTaskStatuses.Paused);
    }

    public void Resume(string id)
    {
        UpdateStatus(id, ScheduledTaskStatuses.Paused, ScheduledTaskStatuses.Scheduled);
    }

    private void UpdateStatus(string id, string expected, string next)
    {
        var tasks = Load().ToList();
        var task = tasks.SingleOrDefault(item => item.Id == id) ?? throw new InvalidOperationException("The scheduled task no longer exists.");
        if (task.Status != expected) throw new InvalidOperationException($"A task in {task.Status} state cannot be changed this way.");
        task.Status = next;
        Save(tasks);
    }

    private void Save(IEnumerable<ScheduledTask> tasks)
    {
        var list = tasks.ToList();
        Validate(list);
        AtomicFileWriter.WriteText(_path, BoundedFileReader.EnsureTextSize(JsonSerializer.Serialize(list, Options), "scheduled tasks"));
    }

    private static void ValidatePrompt(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt) || prompt.Length > MaxPromptCharacters || prompt.Any(char.IsControl)) throw new ArgumentException("The scheduled prompt is invalid.", nameof(prompt));
    }

    private static void Validate(IEnumerable<ScheduledTask> tasks)
    {
        var list = tasks.ToList();
        if (list.Count > MaxTasks || list.Any(item => item is null)) throw new InvalidDataException("The scheduled task store exceeds its safety bounds.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var task in list)
        {
            if (string.IsNullOrWhiteSpace(task.Id) || task.Id.Length > 128 || !ids.Add(task.Id)) throw new InvalidDataException("The scheduled task store contains an invalid or duplicate ID.");
            ValidatePrompt(task.Prompt);
            if (task.WorkspaceMode is not (WorkspaceModes.Chat or WorkspaceModes.CodingAgent)) throw new InvalidDataException("The scheduled task store contains an invalid workspace mode.");
            if (task.Status is not (ScheduledTaskStatuses.Scheduled or ScheduledTaskStatuses.Running or ScheduledTaskStatuses.Completed or ScheduledTaskStatuses.Failed or ScheduledTaskStatuses.Paused)) throw new InvalidDataException("The scheduled task store contains an invalid status.");
            if (task.Outcome is { Length: > MaxOutcomeCharacters } || task.Outcome?.Any(char.IsControl) == true) throw new InvalidDataException("The scheduled task store contains an invalid outcome.");
        }
    }
}
