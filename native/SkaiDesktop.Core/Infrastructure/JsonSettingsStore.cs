// Loads/saves AppSettings as settings.json in the user's app-data folder.
// Depends on: AppSettings, BoundedFileReader, AtomicFileWriter. Used by: MainPage (SaveSettings and startup).

using System.Text.Json;
using SkaiDesktop.Core.Models;

namespace SkaiDesktop.Core.Infrastructure;

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
        settings.ModelProvider = NormalizeProviderName(settings.ModelProvider);
        if (!ProviderRoutePolicy.IsAllowed(settings.Endpoint) || (ProviderRoutePolicy.IsRemote(settings.Endpoint) && !settings.CloudRouteEnabled))
        {
            settings.Endpoint = defaults.Endpoint;
            settings.CloudRouteEnabled = false;
            if (!settings.ModelProvider.Equals("AirLLM", StringComparison.OrdinalIgnoreCase))
                settings.ModelProvider = defaults.ModelProvider;
        }
        else if (LocalEndpoint.IsLocal(settings.Endpoint))
        {
            settings.CloudRouteEnabled = false;
        }
        settings.RequireLocalModelEndpoints = true;
        settings.VisionApiFormat ??= defaults.VisionApiFormat;
        settings.VisionCapabilityOverride = settings.VisionCapabilityOverride?.ToLowerInvariant() switch
        {
            "supported" or "unsupported" => settings.VisionCapabilityOverride.ToLowerInvariant(),
            _ => defaults.VisionCapabilityOverride
        };
        settings.EffortMode = SkaiDesktop.Core.Services.EffortModePolicy.Normalize(settings.EffortMode);
        settings.ComfyEndpoint ??= defaults.ComfyEndpoint;
        settings.ImageWorkflow ??= defaults.ImageWorkflow;
        settings.GitHubAppClientId ??= defaults.GitHubAppClientId;
        settings.PromptUserName ??= defaults.PromptUserName;
        settings.VoicePrimaryModelId = VoiceModelCatalog.Resolve(settings.VoicePrimaryModelId).Id;
        settings.VoiceLowResourceModelId = VoiceModelCatalog.Resolve(settings.VoiceLowResourceModelId).Id;
        settings.VoiceProfileId = VoiceProfileCatalog.Resolve(settings.VoiceProfiles, settings.VoiceProfileId).Id;
        settings.ElevenLabsVoiceId = NormalizeRequired(settings.ElevenLabsVoiceId, defaults.ElevenLabsVoiceId, 128);
        settings.ElevenLabsModelId = NormalizeRequired(settings.ElevenLabsModelId, defaults.ElevenLabsModelId, 128);
        settings.PetId = settings.PetId?.Trim().ToLowerInvariant() switch
        {
            "byte" or "percy" or "rocky" or "sprout" or "nova" or "ember" or "custom" => settings.PetId.Trim().ToLowerInvariant(),
            _ => defaults.PetId,
        };
        settings.PetSpritePath = NormalizeOptional(settings.PetSpritePath, 1_024) ?? defaults.PetSpritePath;
        settings.PetManifestPath = NormalizeOptional(settings.PetManifestPath, 1_024) ?? defaults.PetManifestPath;
        settings.PetPositionX = settings.PetPositionX is < -1 or > 100_000 ? defaults.PetPositionX : settings.PetPositionX;
        settings.PetPositionY = settings.PetPositionY is < -1 or > 100_000 ? defaults.PetPositionY : settings.PetPositionY;
        settings.VoiceProfiles ??= [];
        settings.VoiceProfiles = settings.VoiceProfiles
            .Where(profile => profile is not null)
            .Take(12)
            .Select(profile => VoiceProfileCatalog.Resolve([profile], profile.Id))
            .ToList();
        settings.LastProjectFileProjectId = NormalizeOptional(settings.LastProjectFileProjectId, 128);
        settings.LastProjectFilePath = NormalizeOptional(settings.LastProjectFilePath, 512);
        settings.OpenProjectFileProjectId = NormalizeOptional(settings.OpenProjectFileProjectId, 128);
        settings.OpenProjectFiles ??= [];
        settings.OpenProjectFiles = settings.OpenProjectFiles
            .Where(path => !string.IsNullOrWhiteSpace(path) && path.Length <= 512 && !path.Any(char.IsControl))
            .Select(path => path.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();
        settings.ExpandedProjectFolders ??= new(StringComparer.OrdinalIgnoreCase);
        settings.ExpandedProjectFolders = settings.ExpandedProjectFolders
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Key) && entry.Key.Length <= 128 && !entry.Key.Any(char.IsControl))
            .Take(24)
            .ToDictionary(
                entry => entry.Key.Trim(),
                entry => (entry.Value ?? [])
                    .Where(folder => !string.IsNullOrWhiteSpace(folder) && folder.Length <= 512 && !folder.Any(char.IsControl))
                    .Select(folder => folder.Trim().Replace('\\', '/'))
                    .Where(folder => folder.Length > 0 && !folder.StartsWith("/", StringComparison.Ordinal) && !folder.Split('/').Any(part => part is "" or "." or ".."))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(64)
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);
        settings.ThemeMode = settings.ThemeMode?.ToLowerInvariant() switch
        {
            "dark" => "dark",
            _ => defaults.ThemeMode
        };
        settings.PinnedModels ??= [];
        settings.PinnedModelConnections ??= [];
        settings.LocalModelPerformance ??= new(StringComparer.OrdinalIgnoreCase);
        settings.ChatFolders ??= [];
        settings.PromptSnippets ??= [];
        settings.SavedPersonas ??= [];
        settings.SavedPersonas = settings.SavedPersonas
            .Where(persona => persona is not null)
            .Take(24)
            .ToList();
        foreach (var persona in settings.SavedPersonas)
        {
            persona.Id = string.IsNullOrWhiteSpace(persona.Id) ? Guid.NewGuid().ToString("N") : persona.Id.Trim();
            persona.Name = (persona.Name ?? string.Empty).Trim();
            persona.Instructions = (persona.Instructions ?? string.Empty).Trim();
        }
        if (settings.ActivePersonaId is not null && !settings.SavedPersonas.Any(persona => persona.Id == settings.ActivePersonaId))
            settings.ActivePersonaId = null;
        settings.SavedPresets ??= [];
        settings.SavedPresets = settings.SavedPresets
            .Where(preset => preset is not null)
            .Take(12)
            .ToList();
        foreach (var preset in settings.SavedPresets)
        {
            preset.Id = string.IsNullOrWhiteSpace(preset.Id) ? Guid.NewGuid().ToString("N") : preset.Id.Trim();
            preset.Name = (preset.Name ?? string.Empty).Trim();
            preset.Instructions = (preset.Instructions ?? string.Empty).Trim();
            preset.Knowledge = (preset.Knowledge ?? string.Empty).Trim();
            preset.ToolGuidance = (preset.ToolGuidance ?? string.Empty).Trim();
        }
        if (settings.ActivePresetId is not null && !settings.SavedPresets.Any(preset => preset.Id == settings.ActivePresetId))
            settings.ActivePresetId = null;
        settings.Quicklinks ??= [];
        settings.WorkspaceDrafts ??= new(StringComparer.Ordinal);
        settings.SplitChatRatios ??= new(StringComparer.OrdinalIgnoreCase);
        return settings;
    }

    private static string NormalizeProviderName(string provider)
    {
        var trimmed = provider.Trim();
        if (trimmed.Equals("OpenAI", StringComparison.OrdinalIgnoreCase)) return "OpenAI API";
        if (trimmed.Equals("Another cloud service", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("Custom endpoint", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("Hosted compatible", StringComparison.OrdinalIgnoreCase)) return "Custom";
        return trimmed;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength || value.Any(char.IsControl)) return null;
        return value.Trim();
    }

    private static string NormalizeRequired(string? value, string fallback, int maxLength) =>
        string.IsNullOrWhiteSpace(value) || value.Length > maxLength || value.Any(char.IsControl)
            ? fallback
            : value.Trim();

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
