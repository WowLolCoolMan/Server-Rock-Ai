using ServerRock.Core.Models;

namespace ServerRock.Core.Services;

public sealed record ConversationSearchHit(ChatSession Session, string Display);

/// <summary>Bounded, local-only search across saved conversation metadata and content.</summary>
public static class ConversationSearchService
{
    public const int MaxQueryCharacters = 200;
    public const int MaxResults = 100;

    public static IReadOnlyList<ConversationSearchHit> Search(IEnumerable<ChatSession> sessions, string? query)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        var value = (query ?? string.Empty).Trim();
        if (value.Length > MaxQueryCharacters || value.Any(char.IsControl))
            throw new ArgumentException($"Conversation search text must be {MaxQueryCharacters} characters or fewer and contain no control characters.", nameof(query));

        return sessions
            .Where(session => session is not null && (value.Length == 0 || Matches(session, value)))
            .OrderByDescending(session => session.UpdatedAt)
            .Take(MaxResults)
            .Select(session => new ConversationSearchHit(session, BuildDisplay(session)))
            .ToArray();
    }

    private static bool Matches(ChatSession session, string value)
    {
        var text = session.Messages ?? [];
        return Contains(session.Title, value)
            || Contains(session.ProjectName, value)
            || Contains(session.FolderName, value)
            || (session.Tags ?? []).Any(tag => Contains(tag, value))
            || text.Any(message => Contains(message.Content, value)
                || (message.Attachments ?? []).Any(attachment => Contains(attachment.FileName, value))
                || (message.Sources ?? []).Any(source => Contains(source.Title, value) || Contains(source.Url, value)));
    }

    private static bool Contains(string? text, string query) => text?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;

    private static string BuildDisplay(ChatSession session)
    {
        var labels = new List<string>();
        if (session.Archived) labels.Add("Archived");
        if (!string.IsNullOrWhiteSpace(session.FolderName)) labels.Add(session.FolderName!);
        if (session.Tags is { Count: > 0 }) labels.Add("#" + string.Join(" #", session.Tags.Take(3)));
        var suffix = labels.Count == 0 ? string.Empty : $" · {string.Join(" · ", labels)}";
        return session.Title + suffix;
    }
}
