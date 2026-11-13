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
    /// <summary>Bounded response-budget preference: adaptive, light, standard, high, or ultra.</summary>
    public string EffortMode { get; set; } = "standard";
    /// <summary>When enabled, sends bounded full text for text/PDF attachments instead of ranked chunks.</summary>
    public bool FullTextContextInjectionEnabled { get; set; }
    public bool WebSearchEnabled { get; set; }
    public bool SmartPromptRewritingEnabled { get; set; }
    public bool MemoryInjectionEnabled { get; set; }
    public List<string> WorkspaceRouteHistory { get; set; } = [];
    /// <summary>Bounded unsent composer drafts keyed by workspace.</summary>
    public Dictionary<string, string> WorkspaceDrafts { get; set; } = new(StringComparer.Ordinal);
    /// <summary>Bounded split-chat ratios keyed by workspace and project context.</summary>
    public Dictionary<string, double> SplitChatRatios { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public bool VoiceOutputEnabled { get; set; }
    public string AccentColor { get; set; } = "cyan";
    public string ThemeMode { get; set; } = "light";
    /// <summary>Ambient workspace backdrop preset. Kept separate from the light/dark palette.</summary>
    public string BackdropPreset { get; set; } = "none";
    /// <summary>Disables decorative backdrop motion while retaining the selected visual preset.</summary>
    public bool ReduceMotion { get; set; }
    /// <summary>Enables local-only developer diagnostics and verbose inspection surfaces.</summary>
    public bool DeveloperMode { get; set; }
    public string InterfaceDensity { get; set; } = "comfortable";
    public string HostLabel { get; set; } = "This device";
    /// <summary>Optional explicit value used only when a reviewed prompt contains {{USER_NAME}}.</summary>
    public string PromptUserName { get; set; } = "";
    public bool RequireLocalModelEndpoints { get; set; }
    public string ComfyEndpoint { get; set; } = "http://127.0.0.1:8188";
    public string ImageWorkflow { get; set; } = "";
    public string GitHubAppClientId { get; set; } = "";
    public List<string> ChatFolders { get; set; } = [];
    /// <summary>Bounded local prompt snippets; never sent unless inserted or explicitly expanded.</summary>
    public List<PromptSnippet> PromptSnippets { get; set; } = [];
    /// <summary>Bounded local reusable AI roles/personas.</summary>
    public List<SavedPersona> SavedPersonas { get; set; } = [];
    public string? ActivePersonaId { get; set; }
    /// <summary>Bounded local web bookmarks; links open only after explicit user action.</summary>
    public List<Quicklink> Quicklinks { get; set; } = [];
    public List<string> InstalledPlugins { get; set; } = [];
    public string? ProjectPath { get; set; }
    public string? ProjectInstructions { get; set; }
    /// <summary>Last reviewed project file that can be resumed without reopening the file browser.</summary>
    public string? LastProjectFileProjectId { get; set; }
    public string? LastProjectFilePath { get; set; }
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
