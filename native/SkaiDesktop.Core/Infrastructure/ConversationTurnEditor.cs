// "Edit an earlier message": replaces that user turn and drops everything after it.
// Depends on: ChatMessage. Used by: MainPage message edit/retry.

using SkaiDesktop.Core.Models;

namespace SkaiDesktop.Core.Infrastructure;

/// <summary>Replaces a user turn and discards the conversation branch that followed it.</summary>
public static class ConversationTurnEditor
{
    public static List<ChatMessage> ReplaceUserTurn(
        IReadOnlyList<ChatMessage> messages,
        int userMessageIndex,
        string replacement,
        IReadOnlyList<ChatAttachment>? attachments = null)
    {
        ArgumentNullException.ThrowIfNull(messages);
        ArgumentNullException.ThrowIfNull(replacement);
        if (userMessageIndex < 0 || userMessageIndex >= messages.Count)
            throw new ArgumentOutOfRangeException(nameof(userMessageIndex));
        if (!string.Equals(messages[userMessageIndex].Role, "user", StringComparison.Ordinal))
            throw new ArgumentException("Only a user turn can be edited.", nameof(userMessageIndex));

        var branch = messages.Take(userMessageIndex).ToList();
        branch.Add(new ChatMessage(
            "user",
            replacement,
            attachments is { Count: > 0 } ? attachments.ToList() : null));
        return branch;
    }
}
