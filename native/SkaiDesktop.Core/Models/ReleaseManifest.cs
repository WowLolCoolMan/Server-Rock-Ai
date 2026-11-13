namespace SkaiDesktop.Core.Models;

public sealed class SignedReleaseManifest
{
    public string Version { get; set; } = "";
    public string Channel { get; set; } = "stable";
    public string Artifact { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public string SignatureBase64 { get; set; } = "";
    public string KeyId { get; set; } = "";
}

public sealed record VerifiedReleaseManifest(string Version, string Channel, string Artifact, string Sha256, string KeyId);
