namespace SkaiDesktop.Core.Services;

/// <summary>Maps the user-facing effort choice to a bounded provider output budget.</summary>
public static class EffortModePolicy
{
    public const string Light = "light";
    public const string Standard = "standard";
    public const string High = "high";
    public const string Ultra = "ultra";
    public const string Adaptive = "adaptive";

    public static string Normalize(string? mode) => mode?.Trim().ToLowerInvariant() switch
    {
        Light or High or Ultra or Adaptive => mode!.Trim().ToLowerInvariant(),
        _ => Standard
    };

    public static int MaxTokens(string? mode) => Normalize(mode) switch
    {
        Light => 512,
        High => 2048,
        Ultra => 4096,
        _ => 1024
    };

    /// <summary>Chooses a bounded output budget from the request context size.</summary>
    public static int MaxTokens(string? mode, int promptCharacters) => Normalize(mode) == Adaptive
        ? promptCharacters switch
        {
            <= 1200 => 512,
            <= 6000 => 1024,
            <= 16000 => 2048,
            _ => 4096
        }
        : MaxTokens(mode);
}
