using SkaiDesktop.Core.Models;

namespace SkaiDesktop.Core.Infrastructure;

/// <summary>
/// Resolves only conservative, local capability facts. Unknown models remain
/// usable; definitive negative matches prevent obviously incompatible payloads.
/// </summary>
public static class ModelCapabilityResolver
{
    private static readonly string[] NonVisionMarkers = [
        "embed", "embedding", "rerank", "re-rank", "whisper", "asr", "speech-to-text", "tts"];

    public static ModelCapabilityProfile Resolve(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var provider = string.IsNullOrWhiteSpace(settings.ModelProvider) ? "unknown" : settings.ModelProvider.Trim();
        var model = string.IsNullOrWhiteSpace(settings.Model) ? "unknown" : settings.Model.Trim();
        var privacy = LocalEndpoint.IsLocal(settings.Endpoint) ? "local" : "remote";
        var vision = settings.VisionCapabilityOverride.Trim().ToLowerInvariant() switch
        {
            "supported" => CapabilitySupport.Supported,
            "unsupported" => CapabilitySupport.Unsupported,
            _ => IsDefinitelyNonVision(model) ? CapabilitySupport.Unsupported : CapabilitySupport.Unknown
        };
        var evidence = settings.VisionCapabilityOverride.Trim().ToLowerInvariant() switch
        {
            "supported" or "unsupported" => "user-selected vision capability override",
            _ when vision == CapabilitySupport.Unsupported => "model identifier indicates a non-vision runtime",
            _ => "provider/model metadata does not declare vision support"
        };
        return new(provider, model, privacy, CapabilitySupport.Supported, vision, evidence);
    }

    public static void ValidateMessageCapabilities(AppSettings settings, IReadOnlyList<ChatMessage> messages)
    {
        var profile = Resolve(settings);
        if (profile.Vision != CapabilitySupport.Unsupported || !messages.Any(message => message.Attachments?.Any(attachment => attachment.IsImage) == true)) return;
        throw new InvalidOperationException($"The selected model '{profile.Model}' is identified as a non-vision model and cannot receive image attachments.");
    }

    private static bool IsDefinitelyNonVision(string model) =>
        NonVisionMarkers.Any(marker => model.Contains(marker, StringComparison.OrdinalIgnoreCase));
}
