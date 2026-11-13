namespace ServerRock.Core.Models;

/// <summary>Local reusable prompt text. Expansion is always previewed before use.</summary>
public sealed class PromptSnippet
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
