using System.Text.RegularExpressions;

namespace SkaiDesktop.Core.Infrastructure;

public sealed record WorkspaceChangeProposal(string RelativePath, string Content);

/// <summary>Extracts bounded, review-only file replacement proposals from a model response.</summary>
public static class WorkspaceChangeProposalParser
{
    private static readonly Regex ChangePattern = new(
        "<workspace-change\\s+path=\"([^\"]+)\">(.*?)</workspace-change>",
        RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IReadOnlyList<WorkspaceChangeProposal> Parse(string response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return ChangePattern.Matches(response)
            .Cast<Match>()
            .Take(8)
            .Select(match => new WorkspaceChangeProposal(
                match.Groups[1].Value.Trim(),
                match.Groups[2].Value.Trim('\r', '\n')))
            .Where(proposal => proposal.RelativePath.Length is > 0 and <= 512
                && proposal.Content.Length is > 0 and <= WorkspaceFileEditor.MaxCharacters
                && !proposal.RelativePath.Any(char.IsControl)
                && !proposal.Content.Contains('\0'))
            .ToArray();
    }

    public static string RemoveProposals(string response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return ChangePattern.Replace(response, "").Trim();
    }
}
