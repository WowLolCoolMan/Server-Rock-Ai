namespace SkaiDesktop.Core.Models;

/// <summary>Describes a voice runtime that can be selected in the local marketplace.</summary>
public sealed record VoiceModelDefinition(
    string Id,
    string Name,
    string Description,
    string Runtime,
    string License,
    string Size,
    bool IsInstalled,
    bool IsLowResource,
    string SourceUrl);

public static class VoiceModelCatalog
{
    public static IReadOnlyList<VoiceModelDefinition> All { get; } =
    [
        new("os-native", "OS-native speech", "Uses the platform speech service; no model download required.", "Windows Speech / Linux espeak-ng", "OS-provided", "No model", true, true, ""),
        new("kokoro-82m-onnx", "Kokoro 82M ONNX", "Natural multi-voice speech through a local ONNX runtime.", "ONNX Runtime", "Apache-2.0", "~90 MB quantized", false, false, "https://huggingface.co/onnx-community/Kokoro-82M-ONNX"),
        new("pocket-tts", "Pocket TTS", "CPU-first streaming speech with voice cloning support.", "Pocket TTS", "MIT", "Model-dependent", false, false, "https://github.com/kyutai-labs/pocket-tts"),
        new("pocket-tts-low-resource", "Pocket TTS Low Resource", "Small CPU-first voice for constrained laptops.", "Pocket TTS", "MIT", "Small English model", false, true, "https://github.com/kyutai-labs/pocket-tts")
    ];

    public static VoiceModelDefinition Resolve(string? id) =>
        All.FirstOrDefault(model => model.Id.Equals(id, StringComparison.OrdinalIgnoreCase)) ?? All[0];
}
