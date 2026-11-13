using ServerRock.Core.Models;
using ServerRock.Core.Services;
using static TestSupport;

internal static class MemoryPromptContextTests
{
    public static void Run()
    {
        var now = DateTimeOffset.UtcNow;
        var entries = new[]
        {
            new MemoryEntry { Scope = "global", Content = "User prefers concise answers.", Enabled = true, UpdatedAtUtc = now },
            new MemoryEntry { Scope = "project:Demo", Content = "The fixture uses local-only tests.", Enabled = true, UpdatedAtUtc = now.AddMinutes(1) },
            new MemoryEntry { Scope = "project:Other", Content = "Do not include this note.", Enabled = true, UpdatedAtUtc = now.AddMinutes(2) },
            new MemoryEntry { Scope = "global", Content = "Disabled note.", Enabled = false, UpdatedAtUtc = now.AddMinutes(3) }
        };
        Check(MemoryPromptContext.Build(entries, false, "Demo") is null, "memory injection requires explicit global enablement");
        var context = MemoryPromptContext.Build(entries, true, "Demo")!;
        Check(context.Contains("User prefers concise answers.", StringComparison.Ordinal) && context.Contains("local-only tests", StringComparison.Ordinal), "memory injection includes global and active-project notes");
        Check(!context.Contains("Do not include this note.", StringComparison.Ordinal) && !context.Contains("Disabled note.", StringComparison.Ordinal), "memory injection excludes unrelated and disabled notes");
        Check(context.Contains("untrusted reference material", StringComparison.Ordinal), "memory injection marks notes as reference material");

        var oversized = Enumerable.Range(0, 100).Select(index => new MemoryEntry { Scope = "global", Content = new string('x', 200), UpdatedAtUtc = now.AddSeconds(index) });
        var bounded = MemoryPromptContext.Build(oversized, true, null)!;
        Check(bounded.Length <= MemoryPromptContext.MaxCharacters, "memory injection stays within the prompt bound");
    }
}
