using ServerRock.Core.Models;
using ServerRock.Core.Services;
using static TestSupport;

internal static class AgentTaskArtifactTests
{
    public static void Run()
    {
        var root = Path.Combine(Path.GetTempPath(), "server-rock-agent-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var store = new AgentTaskArtifactStore(Path.Combine(root, "agent-tasks.json"));
            var first = store.Create("Inspect provider routing", "Review capability preflight evidence.", "researcher", "run-1");
            var second = store.Create("Verify the routing change", "Check the result against the first task.", "verifier", dependsOn: [first.Id]);
            Check(first.Status == AgentTaskStatuses.Proposed && second.DependsOn.SequenceEqual([first.Id]), "agent task artifacts preserve roles and dependencies");
            Check(store.TryClaimReady() is null, "queued dependent agent task is not claimable before its prerequisite is queued and completed");
            store.Queue(first.Id);
            var claimedFirst = store.TryClaimReady();
            Check(claimedFirst?.Id == first.Id && claimedFirst.Status == AgentTaskStatuses.Running, "queued root agent task can be claimed without dispatching external work");
            store.Complete(first.Id, "Routing evidence reviewed.", ["local-review"]);
            store.Queue(second.Id);
            var claimedSecond = store.TryClaimReady();
            Check(claimedSecond?.Id == second.Id, "dependent agent task becomes claimable only after its prerequisite completes");
            store.Fail(second.Id, "Verifier found an unresolved compatibility gap.");
            var reloaded = new AgentTaskArtifactStore(Path.Combine(root, "agent-tasks.json")).Load();
            Check(reloaded.Any(task => task.Id == first.Id && task.Status == AgentTaskStatuses.Completed && task.ArtifactLabels.Contains("local-review")), "completed agent task artifacts persist bounded result labels");
            Check(reloaded.Any(task => task.Id == second.Id && task.Status == AgentTaskStatuses.Failed), "failed agent task artifacts persist their terminal status");
            Check(store.Search("verifier").Count == 1, "agent task search matches role metadata");
            try
            {
                store.Create("bad", "missing dependency", dependsOn: ["missing"]);
                throw new Exception("Expected an unknown dependency to be rejected.");
            }
            catch (ArgumentException)
            {
                Check(true, "agent task artifacts reject dependencies outside the local task set");
            }
        }
        finally { DeleteDirectoryTree(root); }
    }
}
