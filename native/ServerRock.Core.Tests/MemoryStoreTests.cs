using ServerRock.Core.Services;
using static TestSupport;

internal static class MemoryStoreTests
{
    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "server-rock-memory-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "memory.json");
        try
        {
            var store = new MemoryStore(path);
            var entry = store.Add("The project uses a local-first release policy.", "project:demo", "user-note", "approval-1", ["release", "local"]);
            Check(store.Search("local", "project:demo").Single().Id == entry.Id, "memory search finds an approved project entry");
            store.SetEnabled(entry.Id, false);
            Check(store.Search("local").Count == 0 && store.Search("local", enabledOnly: false).Count == 1, "memory enable state controls retrieval");
            store.Update(entry.Id, "The project requires signed releases.", "project:demo", "user-edit", "approval-2", ["release"]);
            var reloaded = new MemoryStore(path);
            Check(reloaded.Load().Single().Content.Contains("signed releases", StringComparison.Ordinal), "memory edits survive reload");
            var backup = reloaded.ExportJson();
            Check(backup.Contains("server-rock-memory-v1", StringComparison.Ordinal) && backup.Contains("signed releases", StringComparison.Ordinal) && backup.Contains("\"enabled\": false", StringComparison.Ordinal), "memory export is a validated local backup");
            var importPath = Path.Combine(directory, "imported-memory.json");
            var importedStore = new MemoryStore(importPath);
            var plan = importedStore.PlanImport(backup);
            Check(plan.Entries.Count == 1 && plan.SkippedDuplicates == 0, "memory backup produces an explicit import plan");
            Check(importedStore.ApplyImport(plan) == 1 && importedStore.Load().Single().Id == entry.Id, "memory import merges approved entries without overwriting");
            Check(importedStore.PlanImport(backup).Entries.Count == 0 && importedStore.PlanImport(backup).SkippedDuplicates == 1, "memory import skips duplicate identities");
            store.Remove(entry.Id);
            Check(new MemoryStore(path).Load().Count == 0, "memory deletion is explicit and durable");
            var refused = false;
            try { store.Add("bad\u0001memory", "global", "user", "approval-3"); }
            catch (ArgumentException) { refused = true; }
            Check(refused, "invalid memory content is rejected before persistence");
        }
        finally { if (Directory.Exists(directory)) DeleteDirectoryTree(directory); }
    }
}
