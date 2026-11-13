// Rebrand-safe product identity contracts used by native export workflows.

using ServerRock.Core.Models;
using static TestSupport;

internal static class ProductIdentityTests
{
    public static void Run()
    {
        Check(ProductIdentity.FileNameStem == "serverrock" &&
              ProductIdentity.FileNameStem.All(char.IsLetterOrDigit) &&
              ProductIdentity.FileNameStem == ProductIdentity.FileNameStem.ToLowerInvariant(),
            "export filename stem is derived from centralized product identity");
        Check(ProductIdentity.GitHubCredentialEntropy.Contains("v1", StringComparison.Ordinal) &&
              ProductIdentity.GitHubCredentialLabel.Contains("GitHub", StringComparison.Ordinal),
            "credential storage identifiers are centralized and stable for rebranding");
    }
}
