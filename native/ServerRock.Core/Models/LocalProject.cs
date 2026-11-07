// A named project: instructions plus the folders the user explicitly selected.
// Used by: ProjectStore (projects.json) and MainPage projects / Coding Agent.

namespace ServerRock.Core.Models;

/// <summary>A named local workspace context with explicitly selected folders.</summary>
public sealed class LocalProject
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "New project";
    public string Instructions { get; set; } = "";
    public List<string> FolderPaths { get; set; } = [];
    public string? GitHubRepository { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
