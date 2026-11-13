using System.Text.Json;
using SkaiDesktop.Core.Models;

namespace SkaiDesktop.Core.Infrastructure;

/// <summary>
/// Small durable operation journal for task identity, state transitions and
/// crash reconciliation. It intentionally records uncertainty instead of
/// replaying side effects after a restart.
/// </summary>
public sealed class OperationJournal
{
    public const int MaxEvents = 10_000;
    public const int MaxInstructionCharacters = 50_000;
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true, MaxDepth = 24 };
    private readonly string _path;

    public OperationJournal(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ServerRock", "AI", "operations.json");
    }

    public OperationJournalSnapshot Load()
    {
        if (!File.Exists(_path)) return new([], []);
        var store = JsonSerializer.Deserialize<JournalStore>(
            BoundedFileReader.ReadText(_path), Options)
            ?? throw new InvalidDataException("The operation journal is empty.");
        Validate(store);
        return new(store.Runs, store.Events);
    }

    /// <summary>Returns a versioned, validated local export without changing operation state.</summary>
    public string ExportJson()
    {
        var snapshot = Load();
        Validate(new JournalStore(snapshot.Runs, snapshot.Events));
        return JsonSerializer.Serialize(new JournalExport
        {
            Format = "server-rock-operations-v1",
            ExportedAtUtc = DateTimeOffset.UtcNow,
            Runs = snapshot.Runs,
            Events = snapshot.Events
        }, Options);
    }

    public IReadOnlyList<OperationRun> Search(string? query)
    {
        var normalized = query?.Trim() ?? string.Empty;
        if (normalized.Length > 512 || normalized.Any(char.IsControl))
            throw new ArgumentException("The operation search query is invalid.", nameof(query));
        var runs = Load().Runs;
        if (normalized.Length == 0) return runs.OrderByDescending(item => item.UpdatedAt).Take(200).ToArray();
        return runs.Where(run =>
                run.UserInstruction.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                run.Status.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                run.ModelProvider.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                run.Model.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                run.WorkspaceId.Contains(normalized, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.UpdatedAt).Take(200).ToArray();
    }

    public OperationRun Create(string workspaceId, string userInstruction, string actor = "user",
        string modelProvider = "unknown", string model = "unknown", string endpointPrivacy = "unknown")
    {
        if (string.IsNullOrWhiteSpace(workspaceId) || workspaceId.Length > 128 || workspaceId.Any(char.IsControl))
            throw new ArgumentException("Workspace identity is invalid.", nameof(workspaceId));
        if (string.IsNullOrWhiteSpace(userInstruction) || userInstruction.Length > MaxInstructionCharacters)
            throw new ArgumentException($"An operation instruction must contain 1-{MaxInstructionCharacters} characters.", nameof(userInstruction));
        ValidateProvenance(modelProvider, model, endpointPrivacy);

        var snapshot = Load();
        var now = DateTimeOffset.UtcNow;
        var run = new OperationRun
        {
            WorkspaceId = workspaceId,
            UserInstruction = userInstruction,
            ModelProvider = modelProvider.Trim(),
            Model = model.Trim(),
            EndpointPrivacy = endpointPrivacy.Trim().ToLowerInvariant(),
            Status = OperationRunStatuses.Draft,
            CreatedAt = now,
            UpdatedAt = now,
            LastEventSequence = 1
        };
        snapshot.Runs.Add(run);
        snapshot.Events.Add(new OperationEvent
        {
            RunId = run.RunId,
            Sequence = 1,
            TimestampUtc = now,
            Type = "run.created",
            Actor = actor,
            Payload = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["workspaceId"] = workspaceId,
                ["status"] = run.Status,
                ["modelProvider"] = run.ModelProvider,
                ["model"] = run.Model,
                ["endpointPrivacy"] = run.EndpointPrivacy
            }
        });
        Save(snapshot);
        return run;
    }

    public OperationRun Transition(string runId, string nextStatus, string actor = "system",
        IReadOnlyDictionary<string, string>? payload = null, string? correlationId = null)
    {
        var snapshot = Load();
        var run = snapshot.Runs.SingleOrDefault(item => item.RunId == runId)
            ?? throw new InvalidOperationException("The operation run no longer exists.");
        if (!CanTransition(run.Status, nextStatus))
            throw new InvalidOperationException($"Operation run cannot transition from '{run.Status}' to '{nextStatus}'.");

        var now = DateTimeOffset.UtcNow;
        var previousStatus = run.Status;
        var sequence = run.LastEventSequence + 1;
        run.Status = nextStatus;
        run.LastEventSequence = sequence;
        run.UpdatedAt = now;
        snapshot.Events.Add(new OperationEvent
        {
            RunId = run.RunId,
            Sequence = sequence,
            TimestampUtc = now,
            Type = "run.status_changed",
            Actor = actor,
            CorrelationId = correlationId,
            Payload = BuildTransitionPayload(payload, previousStatus, nextStatus)
        });
        Save(snapshot);
        return run;
    }

    /// <summary>Appends a bounded evidence event without changing the operation status.</summary>
    public OperationRun AppendEvent(string runId, string type, string actor = "system",
        IReadOnlyDictionary<string, string>? payload = null, string? correlationId = null)
    {
        ValidateEventText(runId, 128, nameof(runId));
        ValidateEventText(type, 128, nameof(type));
        ValidateEventText(actor, 128, nameof(actor));
        if (correlationId is not null) ValidateEventText(correlationId, 128, nameof(correlationId));
        var safePayload = new Dictionary<string, string>(StringComparer.Ordinal);
        if (payload is not null)
        {
            if (payload.Count > 32) throw new ArgumentException("An operation event cannot contain more than 32 metadata fields.", nameof(payload));
            foreach (var item in payload)
            {
                ValidateEventText(item.Key, 128, "payload key");
                ValidateEventText(item.Value, 4_096, "payload value");
                safePayload[item.Key] = item.Value;
            }
        }

        var snapshot = Load();
        var run = snapshot.Runs.SingleOrDefault(item => item.RunId == runId)
            ?? throw new InvalidOperationException("The operation run no longer exists.");
        var now = DateTimeOffset.UtcNow;
        var sequence = run.LastEventSequence + 1;
        run.LastEventSequence = sequence;
        run.UpdatedAt = now;
        snapshot.Events.Add(new OperationEvent
        {
            RunId = run.RunId,
            Sequence = sequence,
            TimestampUtc = now,
            Type = type,
            Actor = actor,
            CorrelationId = correlationId,
            Payload = safePayload
        });
        Save(snapshot);
        return run;
    }

    /// <summary>
    /// Marks in-flight runs as uncertain after a process restart. This records
    /// a new event and never retries a tool, Git, or provider side effect.
    /// </summary>
    public IReadOnlyList<OperationRun> ReconcileAfterRestart(string actor = "recovery")
    {
        var snapshot = Load();
        var changed = new List<OperationRun>();
        foreach (var run in snapshot.Runs.Where(item => item.Status is
            OperationRunStatuses.Validating or OperationRunStatuses.Queued or OperationRunStatuses.Running or
            OperationRunStatuses.AwaitingApproval or OperationRunStatuses.Verifying or OperationRunStatuses.Cancelling))
        {
            var now = DateTimeOffset.UtcNow;
            var previousStatus = run.Status;
            var sequence = run.LastEventSequence + 1;
            run.Status = OperationRunStatuses.NeedsReconciliation;
            run.LastEventSequence = sequence;
            run.UpdatedAt = now;
            snapshot.Events.Add(new OperationEvent
            {
                RunId = run.RunId,
                Sequence = sequence,
                TimestampUtc = now,
                Type = "run.needs_reconciliation",
                Actor = actor,
                Payload = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["previousStatus"] = previousStatus,
                    ["reason"] = "The process restarted while the operation was in flight. Inspect effects before resuming."
                }
            });
            changed.Add(run);
        }
        if (changed.Count > 0) Save(snapshot);
        return changed;
    }

    public static bool CanTransition(string current, string next) => current switch
    {
        OperationRunStatuses.Draft => next is OperationRunStatuses.Validating or OperationRunStatuses.Cancelling,
        OperationRunStatuses.Validating => next is OperationRunStatuses.Queued or OperationRunStatuses.Failed or OperationRunStatuses.Cancelling,
        OperationRunStatuses.Queued => next is OperationRunStatuses.Running or OperationRunStatuses.Failed or OperationRunStatuses.Cancelling,
        OperationRunStatuses.Running => next is OperationRunStatuses.AwaitingApproval or OperationRunStatuses.Verifying or OperationRunStatuses.Failed or OperationRunStatuses.Cancelling,
        OperationRunStatuses.AwaitingApproval => next is OperationRunStatuses.Running or OperationRunStatuses.Failed or OperationRunStatuses.Cancelling,
        OperationRunStatuses.Verifying => next is OperationRunStatuses.ReviewReady or OperationRunStatuses.Completed or OperationRunStatuses.Failed or OperationRunStatuses.Cancelling,
        OperationRunStatuses.ReviewReady => next is OperationRunStatuses.Completed or OperationRunStatuses.Failed or OperationRunStatuses.Cancelling,
        OperationRunStatuses.Cancelling => next is OperationRunStatuses.Cancelled or OperationRunStatuses.NeedsReconciliation,
        OperationRunStatuses.NeedsReconciliation => next is OperationRunStatuses.Running or OperationRunStatuses.Cancelled or OperationRunStatuses.Failed,
        _ => false
    };

    private void Save(OperationJournalSnapshot snapshot)
    {
        if (snapshot.Events.Count > MaxEvents) throw new InvalidDataException($"The operation journal cannot contain more than {MaxEvents} events.");
        var json = BoundedFileReader.EnsureTextSize(JsonSerializer.Serialize(new JournalStore(snapshot.Runs, snapshot.Events), Options), "operation journal");
        AtomicFileWriter.WriteText(_path, json);
    }

    private static Dictionary<string, string> BuildTransitionPayload(IReadOnlyDictionary<string, string>? payload, string previousStatus, string nextStatus)
    {
        var result = new Dictionary<string, string>(payload ?? new Dictionary<string, string>(), StringComparer.Ordinal)
        {
            ["from"] = previousStatus,
            ["to"] = nextStatus
        };
        return result;
    }

    private static void Validate(JournalStore store)
    {
        if (store.SchemaVersion != 1 || store.Runs.Count > MaxEvents || store.Events.Count > MaxEvents)
            throw new InvalidDataException("The operation journal schema or size is invalid.");
        var runs = store.Runs.ToDictionary(item => item.RunId, StringComparer.Ordinal);
        foreach (var run in runs.Values)
        {
            run.ModelProvider ??= "unknown";
            run.Model ??= "unknown";
            run.EndpointPrivacy ??= "unknown";
            if (string.IsNullOrWhiteSpace(run.RunId) || string.IsNullOrWhiteSpace(run.WorkspaceId) ||
                run.UserInstruction.Length > MaxInstructionCharacters || run.LastEventSequence < 1)
                throw new InvalidDataException("The operation journal contains an invalid run.");
            ValidateProvenance(run.ModelProvider, run.Model, run.EndpointPrivacy);
        }
        foreach (var group in store.Events.GroupBy(item => item.RunId, StringComparer.Ordinal))
        {
            if (!runs.ContainsKey(group.Key)) throw new InvalidDataException("The operation journal contains an event for an unknown run.");
            var expected = 1L;
            foreach (var item in group.OrderBy(item => item.Sequence))
            {
                if (item.SchemaVersion != 1 || item.Sequence != expected++ || string.IsNullOrWhiteSpace(item.Type))
                    throw new InvalidDataException("The operation journal contains a non-monotonic event sequence.");
            }
            if (runs[group.Key].LastEventSequence != expected - 1)
                throw new InvalidDataException("The operation run sequence does not match its event journal.");
        }
    }

    private static void ValidateProvenance(string modelProvider, string model, string endpointPrivacy)
    {
        if (string.IsNullOrWhiteSpace(modelProvider) || modelProvider.Length > 128 || modelProvider.Any(char.IsControl) ||
            string.IsNullOrWhiteSpace(model) || model.Length > 256 || model.Any(char.IsControl) ||
            endpointPrivacy is not ("local" or "remote" or "unknown"))
            throw new ArgumentException("Operation provider provenance is invalid.");
    }

    private static void ValidateEventText(string value, int maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength || value.Any(char.IsControl))
            throw new ArgumentException($"Operation event {parameterName} is invalid.", parameterName);
    }

    private sealed class JournalStore
    {
        public int SchemaVersion { get; set; } = 1;
        public List<OperationRun> Runs { get; set; } = [];
        public List<OperationEvent> Events { get; set; } = [];

        public JournalStore() { }
        public JournalStore(List<OperationRun> runs, List<OperationEvent> events) { Runs = runs; Events = events; }
    }

    private sealed class JournalExport
    {
        public string Format { get; set; } = "server-rock-operations-v1";
        public DateTimeOffset ExportedAtUtc { get; set; }
        public List<OperationRun> Runs { get; set; } = [];
        public List<OperationEvent> Events { get; set; } = [];
    }
}

public sealed class OperationJournalSnapshot(List<OperationRun> runs, List<OperationEvent> events)
{
    public List<OperationRun> Runs { get; } = runs;
    public List<OperationEvent> Events { get; } = events;
}
