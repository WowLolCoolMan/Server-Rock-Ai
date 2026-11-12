namespace ServerRock.Core.Models;

public sealed class SkillPackageManifest
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string SourceRef { get; set; } = string.Empty;
    public string PackageSha256 { get; set; } = string.Empty;
    public List<string> RequiredPermissions { get; set; } = [];
}

public sealed record InstalledSkill(
    string Id,
    string Name,
    string Version,
    string Publisher,
    string SourceRef,
    string PackageSha256,
    string ActivePath,
    IReadOnlyList<string> RequiredPermissions);
