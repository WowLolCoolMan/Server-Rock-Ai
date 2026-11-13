namespace ServerRock.Core.Models;

/// <summary>Centralized product identity so a future rebrand changes one runtime source.</summary>
public static class ProductIdentity
{
    public const string DisplayName = "Server Rock: AI";
    public const string ShortName = "Server Rock";
    public const string ClientName = "Server Rock AI";

    /// <summary>Safe lower-case filename stem derived from the current short product name.</summary>
    public static string FileNameStem => new string(ShortName
        .Where(char.IsLetterOrDigit)
        .Select(char.ToLowerInvariant)
        .ToArray());
}
