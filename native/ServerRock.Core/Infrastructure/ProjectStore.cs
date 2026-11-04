using System.Text.Json;
using ServerRock.Core.Models;

namespace ServerRock.Core.Infrastructure;

/// <summary>Stores named project contexts in the user's local app data directory.</summary>
public sealed class ProjectStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _path;

    public ProjectStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ServerRock", "AI", "projects.json");
    }

    public List<LocalProject> Load()
    {
        if (!File.Exists(_path)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<LocalProject>>(BoundedFileReader.ReadText(_path), Options) ?? [];
        }
        catch (JsonException error)
        {
            throw new InvalidDataException("Saved projects could not be read. The original file was left untouched.", error);
        }
        catch (InvalidDataException error)
        {
            throw new InvalidDataException("Saved projects could not be read. The original file was left untouched.", error);
        }
    }

    public void Save(IEnumerable<LocalProject> projects)
    {
        ArgumentNullException.ThrowIfNull(projects);
        var json = BoundedFileReader.EnsureTextSize(JsonSerializer.Serialize(projects, Options), "saved projects");
        AtomicFileWriter.WriteText(_path, json);
    }
}
