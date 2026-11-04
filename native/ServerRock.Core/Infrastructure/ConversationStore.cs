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
            return JsonSerializer.Deserialize<List<ChatSession>>(BoundedFileReader.ReadText(_path), Options) ?? [];
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
        AtomicFileWriter.WriteText(_path, json);
    }
}
