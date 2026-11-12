using System.Text.Json;
using ServerRock.Core.Infrastructure;
using ServerRock.Core.Models;

namespace ServerRock.Core.Services;

public sealed class WorkflowService
{
    public const int MaxWorkflows = 500;
    public const int MaxNodes = 200;
    private const int MaxIdLength = 128;
    private const int MaxTitleLength = 200;
    private const int MaxLabelLength = 200;
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true, MaxDepth = 24 };
    private readonly string _path;

    public WorkflowService(string? path = null) => _path = path ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ServerRock", "AI", "workflows.json");

    public IReadOnlyList<string> Validate(WorkflowDefinition workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(workflow.Id) || workflow.Id.Length > MaxIdLength || workflow.Id.Any(char.IsControl)) errors.Add("Workflow ID is invalid.");
        if (string.IsNullOrWhiteSpace(workflow.Title) || workflow.Title.Length > MaxTitleLength || workflow.Title.Any(char.IsControl)) errors.Add("Workflow title is invalid.");
        if (workflow.Version < 1) errors.Add("Workflow version must be at least 1.");
        if (workflow.Nodes is null || workflow.Nodes.Count == 0 || workflow.Nodes.Count > MaxNodes) errors.Add($"Workflow must contain 1-{MaxNodes} nodes.");
        if (workflow.Edges is null || workflow.Edges.Count > MaxNodes * 2) errors.Add("Workflow has too many edges.");
        if (workflow.Nodes is null || workflow.Edges is null) return errors;

        var nodeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in workflow.Nodes)
        {
            if (node is null || string.IsNullOrWhiteSpace(node.Id) || node.Id.Length > MaxIdLength || node.Id.Any(char.IsControl) || !nodeIds.Add(node.Id)) errors.Add("Nodes must have unique bounded IDs.");
            if (node is null || !WorkflowNodeTypes.All.Contains(node.Type)) errors.Add("Workflow contains an unsupported node type.");
            if (node is null || string.IsNullOrWhiteSpace(node.Label) || node.Label.Length > MaxLabelLength || node.Label.Any(char.IsControl)) errors.Add("Workflow node labels are invalid.");
            if (node?.Config is null || node.Config.Count > 32 || node.Config.Any(pair => pair.Key.Length > 64 || pair.Value.Length > 4_000 || pair.Key.Any(char.IsControl) || pair.Value.Any(char.IsControl))) errors.Add("Workflow node configuration is too large or malformed.");
            if (node?.Config is not null && node.Config.Keys.Any(IsSecretLike)) errors.Add("Secrets must be referenced indirectly, never embedded in workflow configuration.");
        }

        var adjacency = nodeIds.ToDictionary(id => id, _ => new List<string>(), StringComparer.Ordinal);
        var incoming = nodeIds.ToDictionary(id => id, _ => 0, StringComparer.Ordinal);
        foreach (var edge in workflow.Edges)
        {
            if (edge is null || !nodeIds.Contains(edge.From) || !nodeIds.Contains(edge.To) || edge.From == edge.To)
            {
                errors.Add("Workflow edges must connect two distinct existing nodes.");
                continue;
            }
            adjacency[edge.From].Add(edge.To);
            incoming[edge.To]++;
        }
        if (errors.Count == 0)
        {
            var roots = incoming.Where(item => item.Value == 0).Select(item => item.Key).ToArray();
            if (roots.Length != 1) errors.Add("Workflow must have exactly one root node.");
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var queue = new Queue<string>(roots);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!visited.Add(current)) continue;
                foreach (var next in adjacency[current]) queue.Enqueue(next);
            }
            if (visited.Count != nodeIds.Count) errors.Add("Workflow must be connected and acyclic.");
            var indegrees = incoming.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
            var topo = new Queue<string>(indegrees.Where(item => item.Value == 0).Select(item => item.Key));
            var processed = 0;
            while (topo.Count > 0)
            {
                var current = topo.Dequeue();
                processed++;
                foreach (var next in adjacency[current]) if (--indegrees[next] == 0) topo.Enqueue(next);
            }
            if (processed != nodeIds.Count) errors.Add("Workflow contains a cycle; bounded retry nodes are not yet supported.");
        }
        return errors.Distinct(StringComparer.Ordinal).ToArray();
    }

    public IReadOnlyList<WorkflowDefinition> Load()
    {
        if (!File.Exists(_path)) return [];
        try
        {
            var workflows = JsonSerializer.Deserialize<List<WorkflowDefinition>>(BoundedFileReader.ReadText(_path), Options) ?? [];
            ValidateCollection(workflows);
            return workflows;
        }
        catch (JsonException ex) { throw new InvalidDataException("Saved workflows could not be read; the original file was left untouched.", ex); }
    }

    public void Save(WorkflowDefinition workflow)
    {
        var errors = Validate(workflow);
        if (errors.Count > 0) throw new InvalidDataException(string.Join(" ", errors));
        var workflows = Load().ToList();
        if (workflows.Any(item => string.Equals(item.Id, workflow.Id, StringComparison.Ordinal) && item.Version == workflow.Version))
            throw new InvalidOperationException("That workflow version already exists. Save edits as a new version.");
        workflows.Add(workflow);
        ValidateCollection(workflows);
        AtomicFileWriter.WriteText(_path, BoundedFileReader.EnsureTextSize(JsonSerializer.Serialize(workflows, Options), "saved workflows"));
    }

    private static bool IsSecretLike(string key) => key.Contains("secret", StringComparison.OrdinalIgnoreCase) || key.Contains("token", StringComparison.OrdinalIgnoreCase) || key.Contains("password", StringComparison.OrdinalIgnoreCase) || key.Contains("api_key", StringComparison.OrdinalIgnoreCase);

    private static void ValidateCollection(IReadOnlyList<WorkflowDefinition> workflows)
    {
        if (workflows.Count > MaxWorkflows) throw new InvalidDataException($"Saved workflows can contain at most {MaxWorkflows} entries.");
        var identities = new HashSet<string>(StringComparer.Ordinal);
        foreach (var workflow in workflows)
        {
            var errors = new WorkflowService().Validate(workflow);
            if (errors.Count > 0) throw new InvalidDataException(string.Join(" ", errors));
            if (!identities.Add($"{workflow.Id}:{workflow.Version}")) throw new InvalidDataException("Saved workflows contain a duplicate version.");
        }
    }
}
