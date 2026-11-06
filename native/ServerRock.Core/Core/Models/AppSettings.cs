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
    public double Temperature { get; set; } = 0.7;
    public bool WebSearchEnabled { get; set; }
    public bool SmartPromptRewritingEnabled { get; set; }
    public bool VoiceOutputEnabled { get; set; }
    public string AccentColor { get; set; } = "cyan";
    public string InterfaceDensity { get; set; } = "comfortable";
    public string HostLabel { get; set; } = "This device";
    public bool RequireLocalModelEndpoints { get; set; }
    public string ComfyEndpoint { get; set; } = "http://127.0.0.1:8188";
    public string ImageWorkflow { get; set; } = "";
    public string GitHubAppClientId { get; set; } = "";
    public List<string> ChatFolders { get; set; } = [];
    public List<string> InstalledPlugins { get; set; } = [];
    public string? ProjectPath { get; set; }
    public string? ProjectInstructions { get; set; }
}

public sealed record LocalModelPerformance(int Samples, double EstimatedTokensPerSecond, long TimeToFirstTokenMilliseconds, DateTimeOffset LastMeasuredAt);
public sealed record PinnedModelConnection(string Provider, string Model, string Endpoint);
