// A bounded local bookmark for a user-approved frequent-access web link.

namespace SkaiDesktop.Core.Models;

public sealed class Quicklink
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
}
