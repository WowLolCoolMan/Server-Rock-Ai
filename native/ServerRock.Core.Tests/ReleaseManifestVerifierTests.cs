using System.Security.Cryptography;
using System.Text;
using ServerRock.Core.Models;
using ServerRock.Core.Services;
using static TestSupport;

internal static class ReleaseManifestVerifierTests
{
    public static async Task RunAsync()
    {
        using var rsa = RSA.Create(2048);
        var artifact = Encoding.UTF8.GetBytes("signed release artifact");
        var manifest = new SignedReleaseManifest
        {
            Version = "1.2.3",
            Channel = "stable",
            Artifact = "Server-Rock-AI-native-win-x64.zip",
            Sha256 = Convert.ToHexString(SHA256.HashData(artifact)).ToLowerInvariant(),
            KeyId = "release-key-1"
        };
        manifest.SignatureBase64 = Convert.ToBase64String(rsa.SignData(ReleaseManifestVerifier.GetSigningPayload(manifest), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        var verified = ReleaseManifestVerifier.Verify(manifest, rsa.ExportSubjectPublicKeyInfoPem(), "stable", "1.2.2");
        await ReleaseManifestVerifier.VerifyArtifactAsync(verified, new MemoryStream(artifact));
        Check(verified.Version == "1.2.3" && verified.Sha256.Length == 64, "signed release manifest and artifact digest are accepted");

        var refused = false;
        try { ReleaseManifestVerifier.Verify(new SignedReleaseManifest { Version = manifest.Version, Channel = manifest.Channel, Artifact = manifest.Artifact, Sha256 = new string('0', 64), KeyId = manifest.KeyId, SignatureBase64 = manifest.SignatureBase64 }, rsa.ExportSubjectPublicKeyInfoPem(), "stable", "1.2.2"); }
        catch (InvalidDataException) { refused = true; }
        Check(refused, "tampered release manifest is rejected");

        refused = false;
        try { await ReleaseManifestVerifier.VerifyArtifactAsync(verified, new MemoryStream(Encoding.UTF8.GetBytes("tampered artifact"))); }
        catch (InvalidDataException) { refused = true; }
        Check(refused, "tampered release artifact is rejected by authenticated digest");

        refused = false;
        try { ReleaseManifestVerifier.Verify(manifest, rsa.ExportSubjectPublicKeyInfoPem(), "beta", "1.2.2"); }
        catch (InvalidDataException) { refused = true; }
        Check(refused, "release channel mismatch is rejected");
    }
}
