namespace SkaiDesktop.Core.Models;

/// <summary>Local reusable role instructions. Activation is explicit and never changes provider settings.</summary>
public sealed class SavedPersona
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
