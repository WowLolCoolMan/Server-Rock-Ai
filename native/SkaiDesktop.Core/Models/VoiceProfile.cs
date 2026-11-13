namespace SkaiDesktop.Core.Models;

/// <summary>A bounded, user-visible voice presentation preset.</summary>
public sealed class VoiceProfile
{
    public string Id { get; set; } = "skai-neutral";
    public string Name { get; set; } = "Skai Neutral";
    public string Description { get; set; } = "Balanced speed and clarity.";
    /// <summary>Optional OS voice identifier. Empty means the platform default voice.</summary>
    public string VoiceName { get; set; } = "";
    public int Rate { get; set; } = 0;
    public int Volume { get; set; } = 100;
}

public static class VoiceProfileCatalog
{
    public static IReadOnlyList<VoiceProfile> Defaults { get; } =
    [
        new() { Id = "skai-neutral", Name = "Skai Neutral", Description = "Balanced speed and clarity." },
        new() { Id = "skai-warm", Name = "Skai Warm", Description = "Softer pacing for longer conversations.", Rate = -5 },
        new() { Id = "skai-direct", Name = "Skai Direct", Description = "Crisp, quicker responses.", Rate = 8 },
        new() { Id = "skai-calm", Name = "Skai Calm", Description = "Slower pacing for focused listening.", Rate = -12 }
    ];

    public static VoiceProfile Resolve(IEnumerable<VoiceProfile>? profiles, string? id)
    {
        var source = profiles?.Where(item => item is not null).ToArray();
        var candidates = source is { Length: > 0 } ? source : Defaults;
        var profile = candidates.FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        return profile is null ? Defaults[0] : new VoiceProfile
        {
            Id = profile.Id,
            Name = profile.Name,
            Description = profile.Description,
            VoiceName = profile.VoiceName,
            Rate = Math.Clamp(profile.Rate, -50, 50),
            Volume = Math.Clamp(profile.Volume, 0, 100)
        };
    }
}
