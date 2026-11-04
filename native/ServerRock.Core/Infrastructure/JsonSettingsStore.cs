using System.Text.Json;
using ServerRock.Core.Models;

namespace ServerRock.Core.Infrastructure;

/// <summary>Stores non-secret preferences in the current user's app data directory.</summary>
public sealed class JsonSettingsStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _path;

    public JsonSettingsStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ServerRock", "AI", "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_path)) return JsonSerializer.Deserialize<AppSettings>(BoundedFileReader.ReadText(_path), Options) ?? new AppSettings();
        }
        catch (JsonException) { PreserveCorruptSettings(); }
        catch (InvalidDataException) { PreserveCorruptSettings(); }
        catch (IOException) { }
        return new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var json = BoundedFileReader.EnsureTextSize(JsonSerializer.Serialize(settings, Options), "saved settings");
        AtomicFileWriter.WriteText(_path, json);
    }

    private void PreserveCorruptSettings()
    {
        try
        {
            var recoveryPath = Path.GetFullPath(_path) + ".corrupt-" + Guid.NewGuid().ToString("N");
            File.Move(_path, recoveryPath);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
