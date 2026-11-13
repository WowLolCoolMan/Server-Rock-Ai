using SkaiDesktop.Core.Models;
using SkaiDesktop.Core.Services;

internal static class ModelCouncilPolicyTests
{
    public static void Run()
    {
        var prompt = ModelCouncilPolicy.BuildPrompt("What should we ship?", [
            (new PinnedModelConnection("Local", "one", "http://localhost:11434/v1"), "Ship the tested path."),
            (new PinnedModelConnection("Remote", "two", "https://example.test/v1"), "Ship after review.")
        ]);
        Check(prompt.Contains("What should we ship?", StringComparison.Ordinal) && prompt.Contains("untrusted reference material", StringComparison.Ordinal), "model council prompt preserves the question and marks answers untrusted");
        CheckThrows(() => ModelCouncilPolicy.BuildPrompt("one", [(new PinnedModelConnection("Local", "one", "http://localhost:11434/v1"), "only")]), "model council requires two successful answers");
        Check(ModelCouncilPolicy.BuildPrompt("question", Enumerable.Range(0, 6).Select(index => (new PinnedModelConnection("Local", $"m{index}", "http://localhost:11434/v1"), new string('x', 20_000))).ToArray()).Length <= ModelCouncilPolicy.MaxPromptCharacters + 20, "model council prompt stays bounded");
        Console.WriteLine("PASS bounded model council synthesis policy");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("FAIL " + message);
    }

    private static void CheckThrows(Action action, string message)
    {
        try { action(); throw new Exception("FAIL " + message); }
        catch (ArgumentException) { Console.WriteLine("PASS " + message); }
    }
}
