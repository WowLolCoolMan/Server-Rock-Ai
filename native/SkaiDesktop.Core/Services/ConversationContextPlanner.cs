using SkaiDesktop.Core.Models;

namespace SkaiDesktop.Core.Services;

public sealed record ContextPlan(IReadOnlyList<ChatMessage> Messages, int OriginalMessageCount, int OmittedMessageCount, int CharacterCount);

/// <summary>Deterministic local context-window selection. It never invents a model summary.</summary>
public static class ConversationContextPlanner
{
    private const int MarkerCharacters = 126;

    public static ContextPlan Plan(IReadOnlyList<ChatMessage> messages, int maxCharacters)
    {
        ArgumentNullException.ThrowIfNull(messages);
        if (maxCharacters < 512 || maxCharacters > 8_000_000) throw new ArgumentOutOfRangeException(nameof(maxCharacters));
        if (messages.Count == 0) return new([], 0, 0, 0);
        var systems = messages.Where(message => string.Equals(message.Role, "system", StringComparison.OrdinalIgnoreCase)).ToArray();
        var turns = messages.Where(message => !string.Equals(message.Role, "system", StringComparison.OrdinalIgnoreCase)).ToArray();
        var selected = new List<ChatMessage>();
        var used = 0;
        foreach (var system in systems)
        {
            var cost = Cost(system);
            if (cost > maxCharacters - used) break;
            selected.Add(system);
            used += cost;
        }

        var chosenTurns = new List<ChatMessage>();
        for (var index = turns.Length - 1; index >= 0; index--)
        {
            var cost = Cost(turns[index]);
            if (cost + used + (index > 0 ? MarkerCharacters : 0) > maxCharacters) break;
            chosenTurns.Add(turns[index]);
            used += cost;
        }
        chosenTurns.Reverse();
        var omitted = turns.Length - chosenTurns.Count;
        if (omitted > 0)
        {
            var marker = new ChatMessage("system", $"[Earlier conversation omitted: {omitted} message(s) exceeded the selected context budget. Do not assume omitted details; ask the user if needed.]");
            while (Cost(marker) + used > maxCharacters && chosenTurns.Count > 0)
            {
                used -= Cost(chosenTurns[0]);
                chosenTurns.RemoveAt(0);
                omitted++;
            }
            if (Cost(marker) + used <= maxCharacters) { selected.Add(marker); used += Cost(marker); }
        }
        selected.AddRange(chosenTurns);
        return new ContextPlan(selected, messages.Count, omitted, used);
    }

    private static int Cost(ChatMessage message) => checked(message.Content.Length + message.Role.Length + 8 + (message.Attachments?.Sum(attachment => attachment.Content.Length) ?? 0));
}
