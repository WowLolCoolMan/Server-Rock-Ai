using ServerRock.Core.Models;

namespace ServerRock.Core.Services;

/// <summary>Creates independent message copies for full-chat or message-prefix forks.</summary>
public static class ConversationForkService
{
    public static IReadOnlyList<ChatMessage> CopyMessages(IReadOnlyList<ChatMessage> messages, int? throughIndex = null)
    {
        ArgumentNullException.ThrowIfNull(messages);
        var lastIndex = throughIndex ?? messages.Count - 1;
        if (messages.Count == 0) return [];
        if (lastIndex < 0 || lastIndex >= messages.Count) throw new ArgumentOutOfRangeException(nameof(throughIndex));
        return messages.Take(lastIndex + 1).Select(Clone).ToArray();
    }

    private static ChatMessage Clone(ChatMessage message) => message with
    {
        Attachments = message.Attachments is null ? null : [.. message.Attachments],
        Sources = message.Sources is null ? null : [.. message.Sources]
    };
}
