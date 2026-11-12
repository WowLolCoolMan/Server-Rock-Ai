using ServerRock.Core.Models;
using ServerRock.Core.Services;
using static TestSupport;

internal static class ReadOnlyWorkspaceAdapterTests
{
    public static void Run()
    {
        var root = Path.Combine(Path.GetTempPath(), $"server-rock-read-adapter-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root, "notes.md"), "read-only evidence");
            var adapter = new ReadOnlyWorkspaceAdapter();
            var grant = new WorkflowCapabilityGrant { RunId = "run", NodeId = "read", Scope = Path.GetFullPath(root), ApprovalId = "approval", Capabilities = [WorkflowCapabilities.ReadWorkspace] };
            var result = adapter.ReadText("run", "read", root, "notes.md", grant);
            Check(result.Content == "read-only evidence" && !result.Effect.Mutated, "approved workspace read returns content and a non-mutating effect");
            var refused = false;
            try { adapter.ReadText("other", "read", root, "notes.md", grant); }
            catch (UnauthorizedAccessException) { refused = true; }
            Check(refused, "workspace read refuses a grant from another run");
            File.WriteAllText(Path.Combine(root, ".env"), "secret");
            refused = false;
            try { adapter.ReadText("run", "read", root, ".env", grant); }
            catch (InvalidOperationException) { refused = true; }
            Check(refused, "workspace read refuses protected files");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
