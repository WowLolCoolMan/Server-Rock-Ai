// Every non-secret user preference, saved as settings.json.
// Depends on: nothing (plain data).
// Used by: JsonSettingsStore (load/save), OpenAiCompatibleProvider (endpoint, model, temperature),
// and MainPage everywhere settings are read or changed. Secrets (API keys, tokens) never go here.

namespace ServerRock.Core.Models;

public sealed class AppSettings
{
    public string Endpoint { get; set; } = "http://localhost:11434/v1";
    public string Model { get; set; } = "llama3.2";
    public string ModelProvider { get; set; } = "Ollama";
    public List<string> PinnedModels { get; set; } = [];
    public List<PinnedModelConnection> PinnedModelConnections { get; set; } = [];
    public Dictionary<string, LocalModelPerformance> LocalModelPerformance { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string VisionApiFormat { get; set; } = "auto";
    public string VisionCapabilityOverride { get; set; } = "auto";
    public double Temperature { get; set; } = 0.7;
    public bool WebSearchEnabled { get; set; }
    public bool SmartPromptRewritingEnabled { get; set; }
    public bool MemoryInjectionEnabled { get; set; }
    public bool VoiceOutputEnabled { get; set; }
    public string AccentColor { get; set; } = "cyan";
    public string ThemeMode { get; set; } = "light";
    public string InterfaceDensity { get; set; } = "comfortable";
    public string HostLabel { get; set; } = "This device";
    /// <summary>Optional explicit value used only when a reviewed prompt contains {{USER_NAME}}.</summary>
    public string PromptUserName { get; set; } = "";
    public bool RequireLocalModelEndpoints { get; set; }
    public string ComfyEndpoint { get; set; } = "http://127.0.0.1:8188";
    public string ImageWorkflow { get; set; } = "";
    public string GitHubAppClientId { get; set; } = "";
    public List<string> ChatFolders { get; set; } = [];
    public List<string> InstalledPlugins { get; set; } = [];
    public string? ProjectPath { get; set; }
    public string? ProjectInstructions { get; set; }
}

/// <summary>Running average of measured local response speed for one endpoint + model.</summary>
public sealed record LocalModelPerformance(int Samples, double EstimatedTokensPerSecond, long TimeToFirstTokenMilliseconds, DateTimeOffset LastMeasuredAt)
{
    /// <summary>Dictionary key in <see cref="AppSettings.LocalModelPerformance"/>: normalized endpoint + newline + model ID.</summary>
    public static string Key(string endpoint, string model) => endpoint.Trim().TrimEnd('/').ToLowerInvariant() + "\n" + model;

    /// <summary>
    /// Folds one measured response into the running average. Speed is estimated as
    /// characters / 4 per second after the first token (a rough token count).
    /// </summary>
    public static LocalModelPerformance AddSample(LocalModelPerformance? previous, int responseCharacters,
        long elapsedMilliseconds, long firstTokenMilliseconds, DateTimeOffset measuredAt)
    {
        var durationSeconds = Math.Max(0.001, (elapsedMilliseconds - firstTokenMilliseconds) / 1000d);
        var estimatedTokensPerSecond = Math.Max(0.1, responseCharacters / 4d / durationSeconds);
        var samples = (previous?.Samples ?? 0) + 1;
        var averageRate = previous is null
            ? estimatedTokensPerSecond
            : previous.EstimatedTokensPerSecond + (estimatedTokensPerSecond - previous.EstimatedTokensPerSecond) / samples;
        var averageFirstToken = previous is null
            ? firstTokenMilliseconds
            : (long)Math.Round(previous.TimeToFirstTokenMilliseconds + (firstTokenMilliseconds - previous.TimeToFirstTokenMilliseconds) / (double)samples);
        return new LocalModelPerformance(samples, averageRate, averageFirstToken, measuredAt);
    }
}
public sealed record PinnedModelConnection(string Provider, string Model, string Endpoint);
