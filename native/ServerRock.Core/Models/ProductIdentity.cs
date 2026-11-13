namespace ServerRock.Core.Models;

/// <summary>Centralized product identity so a future rebrand changes one runtime source.</summary>
public static class ProductIdentity
{
    public const string DisplayName = "Server Rock: AI";
    public const string ShortName = "Server Rock";
    public const string ClientName = "Server Rock AI";

    // Stable storage identifiers are intentionally independent from the display name so a
    // future rebrand does not strand existing protected GitHub credentials or keyring entries.
    public const string GitHubCredentialEntropy = "Server Rock AI GitHub credential v1";
    public const string GitHubCredentialLabel = "Server Rock AI GitHub";

    /// <summary>Safe lower-case filename stem derived from the current short product name.</summary>
    public static string FileNameStem => new string(ShortName
        .Where(char.IsLetterOrDigit)
        .Select(char.ToLowerInvariant)
        .ToArray());
}
