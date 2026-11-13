namespace SkaiDesktop.Core.Models;

public sealed class MemoryEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Scope { get; set; } = "global";
    public string Content { get; set; } = "";
    public string Source { get; set; } = "user";
    public string ApprovalId { get; set; } = "";
    public List<string> Tags { get; set; } = [];
    public bool Enabled { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
