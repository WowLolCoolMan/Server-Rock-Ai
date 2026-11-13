using SkaiDesktop.Core.Models;
using SkaiDesktop.Core.Services;
using static TestSupport;

internal static class ConversationContextPlannerTests
{
    public static void Run()
    {
        var messages = new List<ChatMessage>
        {
            new("system", "Rules"),
            new("user", new string('u', 300)),
            new("assistant", new string('a', 300)),
            new("user", "latest request")
        };
        var plan = ConversationContextPlanner.Plan(messages, 512);
        Check(plan.Messages[0].Role == "system" && plan.Messages[^1].Content == "latest request", "context planner preserves system context and newest turn");
        Check(plan.OmittedMessageCount > 0 && plan.Messages.Any(message => message.Content.Contains("Earlier conversation omitted", StringComparison.Ordinal)), "context planner marks omitted history without inventing a summary");
        Check(messages.Count == 4 && messages[1].Content.Length == 300 && messages[1].Content.All(character => character == 'u'), "context planner does not mutate the source history");
        var empty = ConversationContextPlanner.Plan([], 512);
        Check(empty.OriginalMessageCount == 0 && empty.Messages.Count == 0, "context planner handles an empty conversation");
    }
}
