namespace SkaiDesktop.Core.Models;

/// <summary>Centralized product identity so a future rebrand changes one runtime source.</summary>
public static class ProductIdentity
{
    public const string DisplayName = "Skai Desktop";
    public const string ShortName = "Skai";
    public const string ClientName = "Skai Desktop";

    // Stable protocol identifier for outbound User-Agent headers. This is intentionally
    // independent from the display name and must not change during a visual rebrand.
    public const string UserAgentProduct = "ServerRockAI";
    public const string UserAgentVersion = "1.0";

    public static string UserAgent(string surface) =>
        $"{UserAgentProduct}/{UserAgentVersion} ({surface})";

    // Stable storage identifiers are intentionally independent from the display name so a
    // future rebrand does not strand existing protected GitHub credentials or keyring entries.
    public const string GitHubCredentialEntropy = "Server Rock AI GitHub credential v1";
    public const string GitHubCredentialLabel = "Server Rock AI GitHub";
    public const string ProviderCredentialEntropy = "Server Rock AI provider credential v1";
    public const string ProviderCredentialLabel = "Server Rock AI provider";

    /// <summary>Safe lower-case filename stem derived from the current short product name.</summary>
    public static string FileNameStem => new string(ShortName
        .Where(char.IsLetterOrDigit)
        .Select(char.ToLowerInvariant)
        .ToArray());
}
