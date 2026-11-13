using ServerRock.Core.Models;

namespace ServerRock.Core.Services;

public sealed record ConversationMessageSearchHit(int MessageIndex, string Role, string Preview);

/// <summary>Bounds local search within the active conversation without reading external data.</summary>
public static class ConversationMessageSearchService
{
    public const int MaxQueryCharacters = 256;
    public const int MaxHits = 64;
    public const int MaxPreviewCharacters = 240;

    public static IReadOnlyList<ConversationMessageSearchHit> Search(IReadOnlyList<ChatMessage> messages, string? query)
    {
        ArgumentNullException.ThrowIfNull(messages);
        var normalized = (query ?? string.Empty).Trim();
        if (normalized.Length > MaxQueryCharacters) throw new ArgumentException("The message search query is too long.", nameof(query));
        if (normalized.Length == 0) return [];
        var hits = new List<ConversationMessageSearchHit>();
        for (var index = 0; index < messages.Count && hits.Count < MaxHits; index++)
        {
            var message = messages[index];
            if (!message.Content.Contains(normalized, StringComparison.OrdinalIgnoreCase)) continue;
            var preview = message.Content.ReplaceLineEndings(" ").Trim();
            if (preview.Length > MaxPreviewCharacters) preview = preview[..MaxPreviewCharacters].TrimEnd() + "…";
            hits.Add(new ConversationMessageSearchHit(index, message.Role, preview));
        }
        return hits;
    }
}
