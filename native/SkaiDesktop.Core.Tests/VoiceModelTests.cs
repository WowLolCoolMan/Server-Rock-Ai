using SkaiDesktop.Core.Models;
using static TestSupport;

internal static class VoiceModelTests
{
    public static void Run()
    {
        Check(VoiceModelCatalog.Resolve("unknown").Id == "os-native", "voice catalog falls back to the installed OS-native runtime");
        Check(VoiceModelCatalog.All.Any(model => model.Id == "kokoro-82m-onnx" && model.License == "Apache-2.0"), "voice catalog exposes the Kokoro ONNX option");
        Check(VoiceModelCatalog.All.Any(model => model.Id == "pocket-tts-low-resource" && model.IsLowResource), "voice catalog exposes a low-resource Pocket TTS option");
        var profile = VoiceProfileCatalog.Resolve(null, "missing");
        Check(profile.Id == "skai-neutral", "voice profiles fall back to the Skai Neutral preset");
        var bounded = VoiceProfileCatalog.Resolve([new VoiceProfile { Id = "custom", Name = "Custom", Rate = 999, Volume = -2 }], "custom");
        Check(bounded.Rate == 50 && bounded.Volume == 0, "voice profile settings are bounded before use");
    }
}
