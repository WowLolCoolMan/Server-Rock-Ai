using System.Text.Json;
using ServerRock.Core.Infrastructure;
using ServerRock.Core.Models;

namespace ServerRock.Core.Services;

/// <summary>Durable workflow state machine. It records decisions and step state;
/// tool/provider dispatch is intentionally a later capability-gated adapter.</summary>
public sealed class WorkflowRunService
{
    private const int MaxRuns = 500;
    private const int MaxEvents = 20_000;
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true, MaxDepth = 24 };
    private readonly string _path;
    private readonly WorkflowService _definitions;

    public WorkflowRunService(WorkflowService definitions, string? path = null)
    {
        _definitions = definitions;
        _path = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ServerRock", "AI", "workflow-runs.json");
    }

    public WorkflowRun Create(WorkflowDefinition definition, string actor = "user")
    {
        var errors = _definitions.Validate(definition);
        if (errors.Count > 0) throw new InvalidDataException(string.Join(" ", errors));
        var snapshot = Load();
        var now = DateTimeOffset.UtcNow;
        var run = new WorkflowRun
        {
            WorkflowId = definition.Id,
            WorkflowVersion = definition.Version,
            CreatedAt = now,
            UpdatedAt = now,
            LastEventSequence = 1,
            Steps = _definitions.BuildExecutionPlan(definition).Select(step => new WorkflowStepRun
            {
                NodeId = step.NodeId,
                NodeType = step.NodeType,
                Label = step.Label,
                DependsOn = step.DependsOn,
                Config = definition.Nodes.Single(node => node.Id == step.NodeId).Config,
                RequiresApproval = step.NodeType == WorkflowNodeTypes.Approval,
                SafeToRetry = step.NodeType is WorkflowNodeTypes.Condition or WorkflowNodeTypes.Test or WorkflowNodeTypes.Join,
                IdempotencyKey = $"{definition.Id}:{definition.Version}:{step.NodeId}"
            }).ToList()
        };
        snapshot.Runs.Add(run);
        AddEvent(snapshot, run, "run.created", actor, new() { ["workflowId"] = run.WorkflowId, ["workflowVersion"] = run.WorkflowVersion.ToString() });
        Save(snapshot);
        return run;
    }

    public WorkflowRun Queue(string runId, string actor = "system") => Transition(runId, WorkflowRunStatuses.Queued, actor);

    public WorkflowRun Start(string runId, string actor = "system") => Transition(runId, WorkflowRunStatuses.Running, actor);

    public WorkflowRun BeginStep(string runId, string nodeId, string actor = "system")
    {
        var snapshot = Load();
        var run = Find(snapshot, runId);
        if (run.Status != WorkflowRunStatuses.Running) throw new InvalidOperationException("A workflow step can only begin while its run is running.");
        var step = FindStep(run, nodeId);
        if (step.Status is not (WorkflowStepStatuses.Pending or WorkflowStepStatuses.Queued)) throw new InvalidOperationException("The workflow step is not ready to begin.");
        step.Status = step.RequiresApproval ? WorkflowStepStatuses.WaitingApproval : WorkflowStepStatuses.Executing;
        run.Status = step.RequiresApproval ? WorkflowRunStatuses.AwaitingApproval : run.Status;
        Touch(run);
        AddEvent(snapshot, run, step.RequiresApproval ? "step.waiting_approval" : "step.executing", actor, new() { ["nodeId"] = nodeId, ["attempt"] = step.Attempt.ToString() });
        Save(snapshot);
        return run;
    }

    public WorkflowRun ApproveStep(string runId, string nodeId, string approvalId, string actor = "user")
    {
        if (string.IsNullOrWhiteSpace(approvalId) || approvalId.Length > 128 || approvalId.Any(char.IsControl)) throw new ArgumentException("Approval identity is invalid.", nameof(approvalId));
        var snapshot = Load();
        var run = Find(snapshot, runId);
        var step = FindStep(run, nodeId);
        if (run.Status != WorkflowRunStatuses.AwaitingApproval || step.Status != WorkflowStepStatuses.WaitingApproval) throw new InvalidOperationException("That step is not awaiting approval.");
        step.Status = WorkflowStepStatuses.Complete;
        run.Status = WorkflowRunStatuses.Running;
        Touch(run);
        AddEvent(snapshot, run, "approval.granted", actor, new() { ["nodeId"] = nodeId, ["approvalId"] = approvalId });
        if (run.Steps.All(item => item.Status is WorkflowStepStatuses.Complete or WorkflowStepStatuses.Skipped))
        {
            run.Status = WorkflowRunStatuses.Verifying;
            AddEvent(snapshot, run, "run.verifying", actor, new());
            run.Status = WorkflowRunStatuses.Completed;
            AddEvent(snapshot, run, "run.completed", actor, new());
        }
        Save(snapshot);
        return run;
    }

    public WorkflowRun CompleteStep(string runId, string nodeId, string actor = "system")
    {
        var snapshot = Load();
        var run = Find(snapshot, runId);
        var step = FindStep(run, nodeId);
        if (step.Status != WorkflowStepStatuses.Executing) throw new InvalidOperationException("Only an executing step can complete.");
        step.Status = WorkflowStepStatuses.Complete;
        Touch(run);
        AddEvent(snapshot, run, "step.completed", actor, new() { ["nodeId"] = nodeId, ["attempt"] = step.Attempt.ToString() });
        if (run.Steps.All(item => item.Status is WorkflowStepStatuses.Complete or WorkflowStepStatuses.Skipped))
        {
            run.Status = WorkflowRunStatuses.Verifying;
            AddEvent(snapshot, run, "run.verifying", actor, new());
            run.Status = WorkflowRunStatuses.Completed;
            AddEvent(snapshot, run, "run.completed", actor, new());
        }
        Save(snapshot);
        return run;
    }

    public WorkflowRun EvaluateCondition(string runId, string nodeId, IReadOnlyDictionary<string, string> inputs, string actor = "system")
    {
        var snapshot = Load();
        var run = Find(snapshot, runId);
        var step = FindStep(run, nodeId);
        if (step.NodeType != WorkflowNodeTypes.Condition) throw new InvalidOperationException("Only condition nodes are available to the read-only executor.");
        if (step.Status is WorkflowStepStatuses.Pending or WorkflowStepStatuses.Queued) run = BeginStep(runId, nodeId, actor);
        snapshot = Load();
        run = Find(snapshot, runId);
        step = FindStep(run, nodeId);
        if (step.Status != WorkflowStepStatuses.Executing) throw new InvalidOperationException("The condition node is not executable in its current state.");
        var result = ReadOnlyConditionExecutor.Evaluate(step.Config, inputs);
        step.Result = result ? "true" : "false";
        step.Status = WorkflowStepStatuses.Complete;
        Touch(run);
        AddEvent(snapshot, run, "condition.evaluated", actor, new() { ["nodeId"] = nodeId, ["result"] = result ? "true" : "false" });
        if (run.Steps.All(item => item.Status is WorkflowStepStatuses.Complete or WorkflowStepStatuses.Skipped))
        {
            run.Status = WorkflowRunStatuses.Verifying;
            AddEvent(snapshot, run, "run.verifying", actor, new());
            run.Status = WorkflowRunStatuses.Completed;
            AddEvent(snapshot, run, "run.completed", actor, new());
        }
        Save(snapshot);
        return run;
    }

    public WorkflowRun FailStep(string runId, string nodeId, string actor = "system")
    {
        var snapshot = Load();
        var run = Find(snapshot, runId);
        var step = FindStep(run, nodeId);
        if (step.Status != WorkflowStepStatuses.Executing) throw new InvalidOperationException("Only an executing step can fail.");
        step.Status = WorkflowStepStatuses.Failed;
        run.Status = WorkflowRunStatuses.Failed;
        Touch(run);
        AddEvent(snapshot, run, "step.failed", actor, new() { ["nodeId"] = nodeId, ["attempt"] = step.Attempt.ToString() });
        Save(snapshot);
        return run;
    }

    public WorkflowRun RetryStep(string runId, string nodeId, string actor = "user")
    {
        var snapshot = Load();
        var run = Find(snapshot, runId);
        var step = FindStep(run, nodeId);
        if (run.Status != WorkflowRunStatuses.Failed || step.Status != WorkflowStepStatuses.Failed || !step.SafeToRetry) throw new InvalidOperationException("Only failed idempotent steps can be retried safely.");
        step.Attempt++;
        step.Status = WorkflowStepStatuses.Pending;
        run.Status = WorkflowRunStatuses.Running;
        Touch(run);
        AddEvent(snapshot, run, "step.retry_queued", actor, new() { ["nodeId"] = nodeId, ["attempt"] = step.Attempt.ToString(), ["idempotencyKey"] = step.IdempotencyKey });
        Save(snapshot);
        return run;
    }

    public WorkflowRun Cancel(string runId, string actor = "user")
    {
        var snapshot = Load();
        var run = Find(snapshot, runId);
        if (run.Status is WorkflowRunStatuses.Completed or WorkflowRunStatuses.Cancelled) throw new InvalidOperationException("The workflow run is already terminal.");
        foreach (var step in run.Steps.Where(item => item.Status is WorkflowStepStatuses.Pending or WorkflowStepStatuses.Queued or WorkflowStepStatuses.Executing or WorkflowStepStatuses.WaitingApproval)) step.Status = WorkflowStepStatuses.Cancelled;
        run.Status = WorkflowRunStatuses.Cancelled;
        Touch(run);
        AddEvent(snapshot, run, "run.cancelled", actor, new());
        Save(snapshot);
        return run;
    }

    public IReadOnlyList<WorkflowRun> ReconcileAfterRestart(string actor = "recovery")
    {
        var snapshot = Load();
        var changed = new List<WorkflowRun>();
        foreach (var run in snapshot.Runs.Where(item => item.Status is WorkflowRunStatuses.Queued or WorkflowRunStatuses.Running or WorkflowRunStatuses.AwaitingApproval or WorkflowRunStatuses.Verifying))
        {
            run.Status = WorkflowRunStatuses.NeedsReconciliation;
            Touch(run);
            AddEvent(snapshot, run, "run.needs_reconciliation", actor, new() { ["reason"] = "The process restarted while the workflow was in flight; inspect effects before resuming." });
            changed.Add(run);
        }
        if (changed.Count > 0) Save(snapshot);
        return changed;
    }

    public WorkflowRunSnapshot Load() => ReadSnapshot();

    private WorkflowRun Transition(string runId, string next, string actor)
    {
        var snapshot = Load();
        var run = Find(snapshot, runId);
        var allowed = run.Status == WorkflowRunStatuses.Draft && next == WorkflowRunStatuses.Queued || run.Status == WorkflowRunStatuses.Queued && next == WorkflowRunStatuses.Running;
        if (!allowed) throw new InvalidOperationException($"Workflow run cannot transition from '{run.Status}' to '{next}'.");
        run.Status = next;
        Touch(run);
        AddEvent(snapshot, run, "run.status_changed", actor, new() { ["to"] = next });
        Save(snapshot);
        return run;
    }

    private WorkflowRunSnapshot ReadSnapshot()
    {
        if (!File.Exists(_path)) return new([], []);
        var snapshot = JsonSerializer.Deserialize<WorkflowRunSnapshot>(BoundedFileReader.ReadText(_path), Options) ?? throw new InvalidDataException("Workflow run store is empty.");
        if (snapshot.Runs.Count > MaxRuns || snapshot.Events.Count > MaxEvents) throw new InvalidDataException("Workflow run store exceeds its safety limit.");
        return snapshot;
    }

    private void Save(WorkflowRunSnapshot snapshot) => AtomicFileWriter.WriteText(_path, BoundedFileReader.EnsureTextSize(JsonSerializer.Serialize(snapshot, Options), "workflow runs"));
    private static WorkflowRun Find(WorkflowRunSnapshot snapshot, string id) => snapshot.Runs.SingleOrDefault(item => item.RunId == id) ?? throw new InvalidOperationException("Workflow run no longer exists.");
    private static WorkflowStepRun FindStep(WorkflowRun run, string nodeId) => run.Steps.SingleOrDefault(item => item.NodeId == nodeId) ?? throw new InvalidOperationException("Workflow step no longer exists.");
    private static void Touch(WorkflowRun run) { run.UpdatedAt = DateTimeOffset.UtcNow; }
    private static void AddEvent(WorkflowRunSnapshot snapshot, WorkflowRun run, string type, string actor, Dictionary<string, string> payload)
    {
        var sequence = ++run.LastEventSequence;
        snapshot.Events.Add(new WorkflowRunEvent { RunId = run.RunId, Sequence = sequence, Type = type, Actor = actor, Payload = payload });
    }
}

public sealed class WorkflowRunSnapshot(List<WorkflowRun> runs, List<WorkflowRunEvent> events)
{
    public List<WorkflowRun> Runs { get; } = runs;
    public List<WorkflowRunEvent> Events { get; } = events;
}
