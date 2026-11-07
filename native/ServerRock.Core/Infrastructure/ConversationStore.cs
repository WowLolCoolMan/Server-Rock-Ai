// Loads/saves the conversation list (conversations.json). A corrupt file is reported
// and left untouched, never overwritten.
// Depends on: ConversationArchive, BoundedFileReader, AtomicFileWriter.
// Used by: MainPage (history, recents, folders, import).

using System.Text.Json;
using ServerRock.Core.Models;

namespace ServerRock.Core.Infrastructure;

/// <summary>Stores conversations in the user's local app data directory.</summary>
public sealed class ConversationStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _path;

    public ConversationStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ServerRock", "AI", "conversations.json");
    }

    public List<ChatSession> Load()
    {
        if (!File.Exists(_path)) return [];
        try
        {
            var sessions = JsonSerializer.Deserialize<List<ChatSession>>(BoundedFileReader.ReadText(_path), Options) ?? [];
            foreach (var session in sessions)
            {
                if (session is null) throw new InvalidDataException("Saved conversations contain an empty entry. The original file was left untouched.");
                // Older native files and manually recovered JSON can contain
                // explicit nulls despite the model defaults. Restore only the
                // fields required by the native UI; import markers stay intact
                // so Electron migration retries remain idempotent.
                session.Id ??= Guid.NewGuid().ToString("N");
                session.Title ??= "New chat";
                session.Messages ??= [];
                session.BaseMessages ??= [];
                session.Branches ??= [];
                session.Mode ??= WorkspaceModes.Chat;
            }
            return sessions;
        }
        catch (JsonException error)
        {
            throw new InvalidDataException("Saved conversations could not be read. The original file was left untouched.", error);
        }
        catch (InvalidDataException error)
        {
            throw new InvalidDataException("Saved conversations could not be read. The original file was left untouched.", error);
        }
    }

    public void Save(IEnumerable<ChatSession> sessions)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        var json = BoundedFileReader.EnsureTextSize(JsonSerializer.Serialize(sessions, Options), "saved conversations");
        // Local history is trusted only after it meets the same bounds and content
        // policy enforced for an imported archive. Validate before the atomic move
        // so malformed in-memory data cannot replace a previously healthy history.
        _ = ConversationArchive.Parse(json);
        AtomicFileWriter.WriteText(_path, json);
    }
}
