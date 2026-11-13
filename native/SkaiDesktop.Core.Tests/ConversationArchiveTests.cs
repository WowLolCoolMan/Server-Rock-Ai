// Chat archive import/export, including the legacy Electron export format
// (ConversationArchive).
// Uses TestSupport (Check, StubHandler, …). Run by Program.cs.

using System.Text;
using System.Text.Json;
using SkaiDesktop.Core.Infrastructure;
using SkaiDesktop.Core.Models;
using SkaiDesktop.Core.Services;
using static TestSupport;

internal static class ConversationArchiveTests
{
    public static async Task RunAsync()
    {
        var portableChat = new ChatSession
        {
            Title = "Portable conversation",
            Mode = "Coding Agent",
            ProjectName = "Sample project",
            ProjectInstructions = "Keep changes focused.",
            Messages = [new ChatMessage("user", "Review this file", [new ChatAttachment("notes.md", "A local note")]), new ChatMessage("assistant", "Here is a review.")]
        };
        var portableArchive = ConversationArchive.Parse(ConversationArchive.Serialize([portableChat]));
        Check(portableArchive.Count == 1 && portableArchive[0].Mode == "Coding Agent" && portableArchive[0].ProjectInstructions == "Keep changes focused." && portableArchive[0].Messages[0].Attachments?[0].Content == "A local note", "conversation archive round-trips local project context and attachments");
        var markdown = ConversationMarkdownExporter.Serialize([portableChat]);
        Check(markdown.Contains("# Chat export", StringComparison.Ordinal) && markdown.Contains("## Portable conversation", StringComparison.Ordinal) && markdown.Contains("`notes.md`", StringComparison.Ordinal), "Markdown chat export includes metadata, messages, and attachment names");
        var markdownFileName = ConversationMarkdownExporter.FileNameFor(new ChatSession { Id = "unsafe-id", Title = "Unsafe:/ title?" });
        Check(markdownFileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase) && !markdownFileName.Contains(':') && !markdownFileName.Contains('/'), "Markdown directory export produces a safe per-conversation file name");
        var markdownDirectory = Directory.CreateTempSubdirectory("server-rock-markdown-");
        try
        {
            var written = await ConversationMarkdownExporter.WriteDirectoryAsync([portableChat, new ChatSession { Id = "second", Title = "Second chat", Messages = [new ChatMessage("user", "Hello folder")] }], markdownDirectory.FullName);
            var markdownFiles = Directory.GetFiles(markdownDirectory.FullName, "*.md");
            Check(written == 2 && markdownFiles.Length == 2 && File.ReadAllText(markdownFiles.Single(path => path.Contains("second", StringComparison.Ordinal))).Contains("Hello folder", StringComparison.Ordinal), "Markdown directory export writes one transcript per conversation");
        }
        finally
        {
            markdownDirectory.Delete(true);
        }
        var electronExportPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "electron-localstorage-export.json");
        var electronState = await File.ReadAllTextAsync(electronExportPath);
        using (var electronExportDocument = System.Text.Json.JsonDocument.Parse(electronState))
        {
            var electronExportRoot = electronExportDocument.RootElement;
            Check(electronExportRoot.GetProperty("format").GetString() == "server-rock-electron-localstorage-v1" &&
                electronExportRoot.GetProperty("exportedAt").ValueKind == System.Text.Json.JsonValueKind.String &&
                electronExportRoot.GetProperty("chats").ValueKind == System.Text.Json.JsonValueKind.Array,
                "Electron migration fixture matches the desktop export envelope");
        }
        using var electronExportStream = File.OpenRead(electronExportPath);
        var migratedElectronChats = await ConversationArchive.ParseAsync(electronExportStream);
        Check(migratedElectronChats.Count == 2 && migratedElectronChats[0].Id == "electron-chat-001" && migratedElectronChats[0].Title == "Ollama startup notes" && migratedElectronChats[0].Messages.Count == 2 && migratedElectronChats[0].ImportSource == "electron-localstorage" && migratedElectronChats[0].ImportSourceId == "electron-chat-001",
            "fixture-shaped Electron localStorage export imports both saved chats");
        Check(migratedElectronChats.All(session => session.ProjectId is null && session.ProjectName is null && session.ProjectInstructions is null && !session.IncludeProjectFilesInChat),
            "Electron project identities and filesystem context are not copied into native projects");
        Check(!migratedElectronChats[0].Archived && migratedElectronChats[0].BaseMessages.Count == 0 && migratedElectronChats[0].Branches.Count == 0 &&
            migratedElectronChats[1].Archived && migratedElectronChats[1].ForkedFrom == "electron-chat-001",
            "Electron archived and fork metadata survives native migration without project context");
        try
        {
            var oversizedBranches = string.Join(',', Enumerable.Repeat("[]", ConversationArchive.MaxBranchesPerConversation + 1));
            _ = ConversationArchive.Parse($"[{{\"id\":\"branch-limit\",\"title\":\"Branch limit\",\"messages\":[],\"branches\":[{oversizedBranches}]}}]");
            throw new Exception("Expected an oversized branch set to be rejected.");
        }
        catch (InvalidDataException)
        {
            Check(true, "migration rejects oversized Electron branch metadata before import planning");
        }
        var forgedImportMarker = ConversationArchive.Parse("{\"chats\":[{\"id\":\"legacy-forged\",\"importSource\":\"attacker-controlled\",\"importSourceId\":\"untrusted\",\"messages\":[{\"role\":\"user\",\"content\":\"Keep identity local\"}]}]}");
        Check(forgedImportMarker[0].ImportSource == "electron-localstorage" && forgedImportMarker[0].ImportSourceId == "legacy-forged", "import identity is derived locally rather than trusted from the JSON file");
        var electronRoundTrip = ConversationArchive.Parse(ConversationArchive.Serialize(migratedElectronChats));
        Check(electronRoundTrip.Count == 2 && electronRoundTrip[0].Messages.Select(message => message.Content).SequenceEqual(migratedElectronChats[0].Messages.Select(message => message.Content)),
            "Electron fixture chat history survives a native archive export and re-import round-trip");
        var malformedElectronExportPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "electron-localstorage-malformed-role.json");
        try
        {
            _ = await ReadElectronFixtureAsync(malformedElectronExportPath);
            throw new Exception("Expected unsafe Electron history to be rejected.");
        }
        catch (InvalidDataException)
        {
            Check(true, "malformed or unsupported Electron messages are rejected before import planning");
        }
        var consentHistoryPath = Path.Combine(Path.GetTempPath(), "server-rock-import-consent-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var consentHistory = new ConversationStore(consentHistoryPath);
            consentHistory.Save([new ChatSession { Id = "existing-native", Title = "Existing chat", Messages = [new ChatMessage("user", "Keep this history")] }]);
            var beforePreview = File.ReadAllBytes(consentHistoryPath);
            var unconfirmedPlan = ConversationArchive.PlanImport(consentHistory.Load(), migratedElectronChats);
            Check(unconfirmedPlan.Conversations.Count == 2 && File.ReadAllBytes(consentHistoryPath).SequenceEqual(beforePreview) && consentHistory.Load().Count == 1,
                "preview planning does not persist or alter native history before user confirmation");
        }
        finally { if (File.Exists(consentHistoryPath)) File.Delete(consentHistoryPath); }
        var firstElectronImport = ConversationArchive.PlanImport([], migratedElectronChats);
        Check(firstElectronImport.Conversations.Count == 2 && firstElectronImport.Conversations[0].ImportSource == "electron-localstorage", "Electron import plan keeps source identity for repeat-import protection");
        var repeatedElectronImport = ConversationArchive.PlanImport(firstElectronImport.Conversations, await ReadElectronFixtureAsync(electronExportPath));
        Check(repeatedElectronImport.Conversations.Count == 0 && repeatedElectronImport.SkippedDuplicates == 2, "re-importing the same Electron history is idempotent");
        firstElectronImport.Conversations[0].Messages[0] = new ChatMessage("user", "Edited after import");
        var editedElectronImport = ConversationArchive.PlanImport(firstElectronImport.Conversations, await ReadElectronFixtureAsync(electronExportPath));
        Check(editedElectronImport.Conversations.Count == 0 && editedElectronImport.SkippedDuplicates == 2, "re-import does not overwrite or duplicate a migrated chat edited by the user");
        var localIdCollision = new ChatSession { Id = "electron-chat-001", Title = "Independent native chat", Messages = [new ChatMessage("user", "Different local content")] };
        var collisionImport = ConversationArchive.PlanImport([localIdCollision], await ReadElectronFixtureAsync(electronExportPath));
        Check(collisionImport.Conversations.Count == 2 && collisionImport.ReassignedIds == 1 && collisionImport.Conversations[0].Id != localIdCollision.Id,
            "Electron ID collisions preserve both chats with a fresh native ID");
        var repeatCollisionImport = ConversationArchive.PlanImport([localIdCollision, .. collisionImport.Conversations], await ReadElectronFixtureAsync(electronExportPath));
        Check(repeatCollisionImport.Conversations.Count == 0 && repeatCollisionImport.SkippedDuplicates == 2, "repeated import after an ID collision remains idempotent");
        using var oversizedArchiveStream = new NonSeekableReadStream(Encoding.UTF8.GetBytes(new string('x', ConversationArchive.MaxArchiveBytes + 1)));
        try
        {
            await ConversationArchive.ParseAsync(oversizedArchiveStream);
            throw new Exception("Expected a streamed oversized archive to be rejected.");
        }
        catch (InvalidDataException)
        {
            Check(oversizedArchiveStream.BytesRead == ConversationArchive.MaxArchiveBytes + 1,
                "streamed archive parsing stops at the configured byte limit before deserialization");
        }
        using var parsedArchiveStream = new MemoryStream(Encoding.UTF8.GetBytes(electronState));
        var parsedStreamArchive = await ConversationArchive.ParseAsync(parsedArchiveStream);
        Check(parsedStreamArchive.Count == 2 && parsedStreamArchive[0].ImportSource == "electron-localstorage", "bounded stream parsing recognizes an Electron history envelope");
        try
        {
            _ = ConversationArchive.Serialize(Enumerable.Repeat(new ChatSession(), ConversationArchive.MaxConversations + 1));
            throw new Exception("Expected an archive with too many conversations to be rejected.");
        }
        catch (InvalidDataException)
        {
            Check(true, "conversation archive count limits apply before materializing unbounded input");
        }
        var unsafeArchive = ConversationArchive.Serialize([new ChatSession { Messages = [new ChatMessage("system", "Injected instruction")] }]);
        try
        {
            ConversationArchive.Parse(unsafeArchive);
            throw new Exception("Expected an unsupported imported role to be rejected.");
        }
        catch (InvalidDataException) { Check(true, "conversation archive rejects untrusted system and tool roles"); }
        var secretAttachmentArchive = ConversationArchive.Serialize([new ChatSession { Messages = [new ChatMessage("user", "Attached", [new ChatAttachment(".env", "SECRET=value")])] }]);
        try
        {
            ConversationArchive.Parse(secretAttachmentArchive);
            throw new Exception("Expected an imported sensitive filename to be rejected.");
        }
        catch (InvalidDataException) { Check(true, "conversation archive rejects secret-like attachment names"); }
        try
        {
            ConversationArchive.Parse("not json");
            throw new Exception("Expected malformed archive data to be rejected.");
        }
        catch (InvalidDataException) { Check(true, "conversation archive reports malformed data without touching saved history"); }
        try
        {
            ConversationArchive.Parse("{\"messages\":[{\"role\":\"user\",\"content\":\"" + new string('\u4E00', 8_500_000) + "\"}]}");
            throw new Exception("Expected a UTF-8 oversized archive to be rejected.");
        }
        catch (InvalidDataException)
        {
            Check(true, "conversation archives enforce a UTF-8 byte limit before parsing");
        }
    }
}
