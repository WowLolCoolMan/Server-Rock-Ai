using System.Security.Cryptography;
using System.Text;
using SkaiDesktop.Core.Models;

namespace SkaiDesktop.Core.Services;

/// <summary>Fail-closed verification for a release manifest and its staged artifact.</summary>
public static class ReleaseManifestVerifier
{
    private const int MaxKeyCharacters = 16_000;
    private const int MaxSignatureCharacters = 4_000;

    public static VerifiedReleaseManifest Verify(SignedReleaseManifest manifest, string publicKeyPem, string expectedChannel, string? currentVersion = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ValidateText(manifest.Version, 64, "release version");
        ValidateText(manifest.Channel, 32, "release channel");
        ValidateText(manifest.Artifact, 256, "release artifact");
        ValidateText(manifest.Sha256, 64, "artifact digest");
        ValidateText(manifest.SignatureBase64, MaxSignatureCharacters, "manifest signature");
        ValidateText(manifest.KeyId, 128, "signing key identity");
        if (string.IsNullOrWhiteSpace(publicKeyPem) || publicKeyPem.Length > MaxKeyCharacters || publicKeyPem.Any(character => char.IsControl(character) && character is not ('\r' or '\n' or '\t'))) throw new InvalidDataException("The signing public key is invalid.");
        ValidateText(expectedChannel, 32, "expected channel");
        if (!string.Equals(manifest.Channel, expectedChannel, StringComparison.Ordinal)) throw new InvalidDataException("The release channel does not match the requested channel.");
        var version = ParseVersion(manifest.Version);
        if (currentVersion is not null && version <= ParseVersion(currentVersion)) throw new InvalidDataException("The release is not newer than the current version.");
        if (!IsSha256(manifest.Sha256)) throw new InvalidDataException("The release artifact digest is not a SHA-256 hex digest.");
        byte[] signature;
        try { signature = Convert.FromBase64String(manifest.SignatureBase64); }
        catch (FormatException exception) { throw new InvalidDataException("The release manifest signature is not valid base64.", exception); }
        if (signature.Length is < 32 or > 4_096) throw new InvalidDataException("The release manifest signature length is invalid.");
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);
            if (!rsa.VerifyData(GetSigningPayload(manifest), signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)) throw new CryptographicException("The release manifest signature is invalid.");
        }
        catch (Exception exception) when (exception is CryptographicException or ArgumentException)
        {
            throw new InvalidDataException("The release manifest could not be authenticated.", exception);
        }
        return new VerifiedReleaseManifest(manifest.Version, manifest.Channel, manifest.Artifact, manifest.Sha256.ToLowerInvariant(), manifest.KeyId);
    }

    public static async Task VerifyArtifactAsync(VerifiedReleaseManifest manifest, Stream artifact, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(artifact);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[64 * 1024];
        while (true)
        {
            var read = await artifact.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
            if (read == 0) break;
            hash.AppendData(buffer, 0, read);
        }
        var actual = Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
        if (!string.Equals(actual, manifest.Sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("The release artifact digest does not match the authenticated manifest.");
    }

    public static byte[] GetSigningPayload(SignedReleaseManifest manifest) => Encoding.UTF8.GetBytes(string.Join("\n", manifest.Version, manifest.Channel, manifest.Artifact, manifest.Sha256.ToLowerInvariant(), manifest.KeyId));

    private static Version ParseVersion(string value)
    {
        var core = value.Split(['-', '+'], 2)[0];
        if (!Version.TryParse(core, out var version) || version.Major < 0 || version.Minor < 0 || version.Build < 0 || value.Length > 64) throw new InvalidDataException("The release version is invalid.");
        return version;
    }

    private static bool IsSha256(string value) => value.Length == 64 && value.All(Uri.IsHexDigit);

    private static void ValidateText(string value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength || value.Any(char.IsControl)) throw new InvalidDataException($"The {label} is invalid.");
    }
}
