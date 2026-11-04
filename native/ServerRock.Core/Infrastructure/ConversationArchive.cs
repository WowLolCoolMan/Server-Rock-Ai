using System.Text;
using System.Text.Json;
using ServerRock.Core.Models;

namespace ServerRock.Core.Infrastructure;

/// <summary>Validates portable conversation archives before they are merged into local history.</summary>
public static class ConversationArchive
{
    public const int MaxArchiveCharacters = 25_000_000;
    public const int MaxArchiveBytes = 25 * 1024 * 1024;
    public const int MaxConversations = 2_000;
    public const int MaxMessagesPerConversation = 2_000;
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true, MaxDepth = 48 };

    public static string Serialize(IEnumerable<ChatSession> sessions)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        var items = sessions.Take(MaxConversations + 1).ToArray();
        if (items.Length > MaxConversations) throw new InvalidDataException($"An archive can contain at most {MaxConversations} conversations.");
        var json = JsonSerializer.Serialize(items, Options);
        EnsureArchiveSize(json);
        return json;
    }

    public static List<ChatSession> Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        if (json.Length > MaxArchiveCharacters) throw new InvalidDataException("This archive is larger than the 25 MB text limit.");
        EnsureArchiveSize(json);
        List<ChatSession> sessions;
        try { sessions = JsonSerializer.Deserialize<List<ChatSession>>(json, Options) ?? throw new InvalidDataException("The archive contains no conversation list."); }
        catch (JsonException error) { throw new InvalidDataException("This file is not a valid conversation archive.", error); }
        if (sessions.Count > MaxConversations) throw new InvalidDataException($"An archive can contain at most {MaxConversations} conversations.");
        foreach (var session in sessions)
        {
            if (session is null) throw new InvalidDataException("The archive contains an empty conversation entry.");
            if (session.Messages is null || session.Messages.Count > MaxMessagesPerConversation)
                throw new InvalidDataException($"A conversation cannot contain more than {MaxMessagesPerConversation} messages.");
            if (string.IsNullOrWhiteSpace(session.Id) || session.Id.Length > 128) session.Id = Guid.NewGuid().ToString("N");
            if (string.IsNullOrWhiteSpace(session.Title)) session.Title = "Imported chat";
            if (session.Title.Length > 200) session.Title = session.Title[..200];
            if (session.Mode is not ("ChatBot" or "Coding Agent")) session.Mode = "ChatBot";
            if (session.FolderName?.Length > 80) session.FolderName = session.FolderName[..80];
            long contentCharacters = (session.ProjectName?.Length ?? 0) + (session.ProjectInstructions?.Length ?? 0);
            foreach (var message in session.Messages)
            {
                if (message is null || message.Role is not ("user" or "assistant") || message.Content is null)
                    throw new InvalidDataException("Archives may contain only user and assistant messages.");
                contentCharacters += message.Content.Length;
                if (message.Attachments is { Count: > 5 }) throw new InvalidDataException("An archived message contains more than five attachments.");
                long imageBytes = 0;
                if (message.Attachments is not null)
                    foreach (var attachment in message.Attachments)
                    {
                        if (attachment is null || string.IsNullOrWhiteSpace(attachment.FileName) || attachment.FileName.Length > 260 ||
                            !ChatAttachmentPolicy.IsAllowedName(attachment.FileName))
                            throw new InvalidDataException("The archive contains an unsupported or sensitive attachment name.");
                        if (attachment.MediaType is null)
                        {
                            if (attachment.Content is null || attachment.Content.Length > ChatAttachmentPolicy.MaxCharactersPerFile)
                                throw new InvalidDataException("An archived text or PDF attachment exceeds the supported size limit.");
                        }
                        else
                        {
                            if (!ChatAttachmentPolicy.IsAllowedImageMediaType(attachment.MediaType) ||
                                ChatAttachmentPolicy.GetImageMediaType(attachment.FileName) != attachment.MediaType || attachment.Content is null)
                                throw new InvalidDataException("The archive contains an unsupported image attachment.");
                            byte[] bytes;
                            try { bytes = Convert.FromBase64String(attachment.Content); }
                            catch (FormatException error) { throw new InvalidDataException("The archive contains invalid image data.", error); }
                            imageBytes += bytes.LongLength;
                            if (imageBytes > ChatAttachmentPolicy.MaxTotalImageBytes || !ChatAttachmentPolicy.HasValidImageSignature(attachment.MediaType, bytes))
                                throw new InvalidDataException("An archived image exceeds its size limit or has invalid file contents.");
                        }
                        contentCharacters += attachment.Content.Length;
                    }
                if (message.Sources is { Count: > 20 }) throw new InvalidDataException("An archived message contains too many web sources.");
                if (message.Sources is not null)
                    foreach (var source in message.Sources)
                        if (source is null || source.Title is null || source.Snippet is null || source.Title.Length > 500 || source.Snippet.Length > 20_000 ||
                            !Uri.TryCreate(source.Url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
                            throw new InvalidDataException("An archived web source contains an unsafe address.");
            }
            if (contentCharacters > 5_000_000) throw new InvalidDataException("A conversation's saved content exceeds the 5 MB limit.");
        }
        return sessions;
    }

    private static void EnsureArchiveSize(string json)
    {
        if (Encoding.UTF8.GetByteCount(json) > MaxArchiveBytes)
            throw new InvalidDataException("The conversation archive is larger than the 25 MB byte limit.");
    }
}
