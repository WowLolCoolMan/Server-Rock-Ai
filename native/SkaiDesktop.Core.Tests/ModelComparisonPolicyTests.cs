using SkaiDesktop.Core.Models;
using SkaiDesktop.Core.Services;

internal static class ModelComparisonPolicyTests
{
    public static void Run()
    {
        var plan = ModelComparisonPolicy.Create("Compare these answers", [
            new("Local", "model-a", "http://localhost:11434/v1"),
            new("Local", "model-b", "http://127.0.0.1:1234/v1"),
            new("Duplicate", "model-a", "http://localhost:11434/v1")
        ], requireLocalEndpoints: false);
        Check(plan.Models.Count == 2 && plan.Prompt == "Compare these answers", "comparison policy deduplicates pinned models and trims the prompt");
        CheckThrows(() => ModelComparisonPolicy.Create("one", [new("Local", "only", "http://localhost:11434/v1")], false), "comparison policy requires at least two models");
        var remotePlan = ModelComparisonPolicy.Create("remote", [
            new("Local", "a", "http://localhost:11434/v1"),
            new("Remote", "b", "https://example.test/v1")
        ], false);
        Check(remotePlan.Models.Count == 2, "comparison policy accepts an explicit HTTPS cloud route when local-only mode is disabled");
        CheckThrows(() => ModelComparisonPolicy.Create(new string('x', ModelComparisonPolicy.MaxPromptCharacters + 1), [
            new("Local", "a", "http://localhost:11434/v1"),
            new("Local", "b", "http://localhost:11434/v1")
        ], false), "comparison policy bounds prompt size");
        Console.WriteLine("PASS bounded side-by-side model comparison policy");
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
