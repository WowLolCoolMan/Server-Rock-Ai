// Produces a bounded, human-readable Markdown export of validated conversations.
// Depends on: ConversationArchive validation and ChatSession models. Used by the
// native chat export picker alongside the portable JSON archive.

using System.Text;
using ServerRock.Core.Models;

namespace ServerRock.Core.Infrastructure;

public static class ConversationMarkdownExporter
{
    public static string FileNameFor(ChatSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var title = SanitizeFileName(session.Title, "conversation");
        var id = SanitizeFileName(session.Id, "chat");
        return $"{title}--{id}.md";
    }

    public static string Serialize(IEnumerable<ChatSession> sessions)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        var items = sessions.ToList();
        // Reuse the canonical archive validator so Markdown export cannot bypass
        // role, attachment, source, count, or content bounds.
        _ = ConversationArchive.Parse(ConversationArchive.Serialize(items));

        var output = new StringBuilder();
        output.AppendLine("# Chat export");
        output.AppendLine();
        output.AppendLine($"Exported at (UTC): {DateTimeOffset.UtcNow:O}");
        output.AppendLine($"Conversations: {items.Count}");
        output.AppendLine();

        foreach (var session in items)
        {
            output.Append("## ").AppendLine(EscapeHeading(session.Title));
            output.Append("- ID: `").Append(session.Id).AppendLine("`");
            output.Append("- Updated: ").AppendLine(session.UpdatedAt.ToString("O"));
            output.Append("- Mode: ").AppendLine(session.Mode);
            if (!string.IsNullOrWhiteSpace(session.ProjectName)) output.Append("- Project: ").AppendLine(session.ProjectName);
            if (session.Pinned) output.AppendLine("- Pinned: yes");
            if (session.Archived) output.AppendLine("- Archived: yes");
            if (session.Tags.Count > 0) output.Append("- Tags: ").AppendLine(string.Join(", ", session.Tags));
            output.AppendLine();

            foreach (var message in session.Messages)
            {
                output.Append("### ").AppendLine(message.Role.Equals("user", StringComparison.OrdinalIgnoreCase) ? "User" : "Assistant");
                output.AppendLine(message.Content);
                if (message.Attachments is { Count: > 0 })
                {
                    output.AppendLine();
                    output.AppendLine("Attachments:");
                    foreach (var attachment in message.Attachments)
                        output.Append("- `").Append(attachment.FileName).AppendLine("` (content included in the JSON archive)");
                }
                if (message.Sources is { Count: > 0 })
                {
                    output.AppendLine();
                    output.AppendLine("Sources:");
                    foreach (var source in message.Sources)
                        output.Append("- [").Append(EscapeLinkText(source.Title)).Append("](").Append(source.Url).Append(") — ").AppendLine(source.Snippet);
                }
                output.AppendLine();
            }
        }

        return output.ToString();
    }

    private static string EscapeHeading(string value) => value.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal).Trim();
    private static string EscapeLinkText(string value) => value.Replace("[", "\\[", StringComparison.Ordinal).Replace("]", "\\]", StringComparison.Ordinal);

    private static string SanitizeFileName(string value, string fallback)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);
        foreach (var character in value.Trim())
            builder.Append(invalid.Contains(character) || char.IsControl(character) ? '_' : character);
        var safe = builder.ToString().Trim().TrimEnd('.');
        if (string.IsNullOrWhiteSpace(safe)) safe = fallback;
        return safe.Length <= 80 ? safe : safe[..80].TrimEnd();
    }
}
