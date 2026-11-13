namespace SkaiDesktop.Core.Models;

/// <summary>Local reusable prompt, knowledge, and tool-guidance bundle.</summary>
public sealed class SavedPreset
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public string Knowledge { get; set; } = string.Empty;
    public string ToolGuidance { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
