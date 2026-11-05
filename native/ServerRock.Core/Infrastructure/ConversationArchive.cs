using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using ServerRock.Core.Models;

namespace ServerRock.Core.Infrastructure;

/// <summary>Validates portable conversation archives before they are merged into local history.</summary>
public static class ConversationArchive
{
    public const int MaxArchiveCharacters = 25_000_000;
    public const int MaxArchiveBytes = 25 * 1024 * 1024;
    public const int MaxConversations = 2_000;
    public const int MaxMessagesPerConversation = 2_000;
    public const int MaxBranchesPerConversation = 32;
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true, MaxDepth = 48 };

    public sealed record ImportPlan(IReadOnlyList<ChatSession> Conversations, int SkippedDuplicates, int ReassignedIds);

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
        try
        {
            // The Electron client stored its history inside a localStorage object
            // (`{ chats: [...], projects: [...] }`). Accept that shape during the
            // migration window so users can export/hand off legacy data without a
            // separate conversion utility. Native archives remain the canonical
            // array format produced by Serialize.
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var isElectronState = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("chats", out _);
            var payload = isElectronState ? root.GetProperty("chats").GetRawText() : json;
            sessions = JsonSerializer.Deserialize<List<ChatSession>>(payload, Options)
                ?? throw new InvalidDataException("The archive contains no conversation list.");
            var source = isElectronState ? "electron-localstorage" : "conversation-archive";
            var sourceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var session in sessions)
            {
                if (session is null) continue;
                var hasStableId = !string.IsNullOrWhiteSpace(session.Id) && session.Id.Length <= 128;
                if (hasStableId && !sourceIds.Add(session.Id))
                    throw new InvalidDataException("The archive contains duplicate conversation IDs and cannot be imported safely.");
                // These internal markers are derived from the selected source, never trusted from the file.
                session.ImportSource = source;
                session.ImportSourceId = hasStableId ? session.Id : "content-" + GetContentFingerprint(session);
                if (isElectronState)
                {
                    // Electron's project IDs and folder paths belong to a different
                    // project store. Never attach imported chats to a coincidentally
                    // matching native project or transfer filesystem permissions.
                    session.ProjectId = null;
                    session.ProjectName = null;
                    session.ProjectInstructions = null;
                    session.IncludeProjectFilesInChat = false;
                    session.FolderName = null;
                }
            }
        }
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
            if (session.ForkedFrom is { Length: > 128 } || session.ForkedFrom?.Any(char.IsControl) == true)
                throw new InvalidDataException("A conversation fork reference is invalid.");
            session.BaseMessages ??= [];
            session.Branches ??= [];
            if (session.BaseMessages.Count > MaxMessagesPerConversation || session.Branches.Count > MaxBranchesPerConversation ||
                session.Branches.Any(branch => branch is null || branch.Count > MaxMessagesPerConversation) ||
                session.BranchIndex < 0 || session.BranchIndex > session.Branches.Count)
                throw new InvalidDataException("A conversation branch exceeds the supported limits.");
            long contentCharacters = (session.ProjectName?.Length ?? 0) + (session.ProjectInstructions?.Length ?? 0);
            var messageGroups = new List<IReadOnlyList<ChatMessage>> { session.Messages, session.BaseMessages };
            messageGroups.AddRange(session.Branches);
            foreach (var messages in messageGroups)
            foreach (var message in messages)
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

    public static async Task<List<ChatSession>> ParseAsync(Stream source, CancellationToken cancellationToken = default)
    {
        var json = await BoundedFileReader.ReadTextAsync(source, MaxArchiveBytes, "conversation archive", cancellationToken).ConfigureAwait(false);
        return Parse(json);
    }

    public static ImportPlan PlanImport(IEnumerable<ChatSession> existing, IEnumerable<ChatSession> incoming)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(incoming);

        var existingSessions = existing.ToArray();
        var existingIds = existingSessions.Select(session => session.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sourceKeys = existingSessions
            .Where(session => !string.IsNullOrWhiteSpace(session.ImportSource) && !string.IsNullOrWhiteSpace(session.ImportSourceId))
            .Select(GetSourceKey)
            .ToHashSet(StringComparer.Ordinal);
        var fingerprints = existingSessions.Select(GetContentFingerprint).ToHashSet(StringComparer.Ordinal);
        var planned = new List<ChatSession>();
        var skipped = 0;
        var reassigned = 0;

        foreach (var session in incoming)
        {
            if (session is null) throw new InvalidDataException("The import contains an empty conversation entry.");
            session.ImportSource ??= "conversation-archive";
            session.ImportSourceId ??= session.Id;
            var fingerprint = GetContentFingerprint(session);
            var sourceKey = GetSourceKey(session);
            if (!sourceKeys.Add(sourceKey) || !fingerprints.Add(fingerprint))
            {
                skipped++;
                continue;
            }

            if (!existingIds.Add(session.Id))
            {
                session.Id = Guid.NewGuid().ToString("N");
                while (!existingIds.Add(session.Id)) session.Id = Guid.NewGuid().ToString("N");
                reassigned++;
            }
            planned.Add(session);
        }

        return new ImportPlan(planned, skipped, reassigned);
    }

    private static string GetSourceKey(ChatSession session) => $"{session.ImportSource}\0{session.ImportSourceId}";

    private static string GetContentFingerprint(ChatSession session)
    {
        var identity = new
        {
            session.Title,
            session.UpdatedAt,
            session.Messages,
            session.ProjectName,
            session.ProjectInstructions,
            session.Mode,
            session.IncludeProjectFilesInChat,
            session.FolderName
        };
        return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(identity, Options)));
    }

    private static void EnsureArchiveSize(string json)
    {
        if (Encoding.UTF8.GetByteCount(json) > MaxArchiveBytes)
            throw new InvalidDataException("The conversation archive is larger than the 25 MB byte limit.");
    }
}
