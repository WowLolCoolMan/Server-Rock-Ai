// Loads/saves AppSettings as settings.json in the user's app-data folder.
// Depends on: AppSettings, BoundedFileReader, AtomicFileWriter. Used by: MainPage (SaveSettings and startup).

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
            if (File.Exists(_path)) return Normalize(JsonSerializer.Deserialize<AppSettings>(BoundedFileReader.ReadText(_path), Options) ?? new AppSettings());
        }
        catch (JsonException) { PreserveCorruptSettings(); }
        catch (InvalidDataException) { PreserveCorruptSettings(); }
        catch (IOException) { }
        return new AppSettings();
    }

    private static AppSettings Normalize(AppSettings settings)
    {
        // Settings are user-local JSON and older versions or manual recovery can
        // legitimately contain explicit nulls. Keep optional project fields null,
        // but restore required strings and collections before UI code reads them.
        var defaults = new AppSettings();
        settings.Endpoint ??= defaults.Endpoint;
        settings.Model ??= defaults.Model;
        settings.ModelProvider ??= defaults.ModelProvider;
        settings.VisionApiFormat ??= defaults.VisionApiFormat;
        settings.ComfyEndpoint ??= defaults.ComfyEndpoint;
        settings.ImageWorkflow ??= defaults.ImageWorkflow;
        settings.GitHubAppClientId ??= defaults.GitHubAppClientId;
        settings.ThemeMode = settings.ThemeMode?.ToLowerInvariant() switch
        {
            "dark" => "dark",
            _ => defaults.ThemeMode
        };
        settings.PinnedModels ??= [];
        settings.PinnedModelConnections ??= [];
        settings.LocalModelPerformance ??= new(StringComparer.OrdinalIgnoreCase);
        settings.ChatFolders ??= [];
        return settings;
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
