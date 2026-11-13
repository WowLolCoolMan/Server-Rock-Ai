using ServerRock.Core.Models;
using ServerRock.Core.Services;

internal static class ModelComparisonPolicyTests
{
    public static void Run()
    {
        var plan = ModelComparisonPolicy.Create("Compare these answers", [
            new("Local", "model-a", "http://localhost:11434/v1"),
            new("Remote", "model-b", "https://example.test/v1"),
            new("Duplicate", "model-a", "http://localhost:11434/v1")
        ], requireLocalEndpoints: false);
        Check(plan.Models.Count == 2 && plan.Prompt == "Compare these answers", "comparison policy deduplicates pinned models and trims the prompt");
        CheckThrows(() => ModelComparisonPolicy.Create("one", [new("Local", "only", "http://localhost:11434/v1")], false), "comparison policy requires at least two models");
        CheckThrows(() => ModelComparisonPolicy.Create("remote", [
            new("Local", "a", "http://localhost:11434/v1"),
            new("Remote", "b", "https://example.test/v1")
        ], true), "comparison policy honors local-only routing");
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
