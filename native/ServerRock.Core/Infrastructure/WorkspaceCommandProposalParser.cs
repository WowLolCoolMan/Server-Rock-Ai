using System.Text.RegularExpressions;

namespace ServerRock.Core.Infrastructure;

public sealed record WorkspaceCommandProposal(string RelativeWorkingDirectory, string Command, int TimeoutSeconds);

public static class WorkspaceCommandProposalParser
{
    private static readonly Regex CommandPattern = new(
        "<workspace-command(?:\\s+cwd=\"([^\"]*)\")?(?:\\s+timeout-seconds=\"(\\d+)\")?\\s*>(.*?)</workspace-command>",
        RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IReadOnlyList<WorkspaceCommandProposal> Parse(string response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return CommandPattern.Matches(response)
            .Cast<Match>()
            .Take(3)
            .Select(match => new WorkspaceCommandProposal(
                string.IsNullOrWhiteSpace(match.Groups[1].Value) ? "." : match.Groups[1].Value.Trim(),
                match.Groups[3].Value.Trim('\r', '\n', ' '),
                int.TryParse(match.Groups[2].Value, out var timeout)
                    ? Math.Clamp(timeout, 1, (int)WorkspaceCommandRunner.MaxTimeout.TotalSeconds)
                    : 30))
            .Where(proposal => proposal.Command.Length is > 0 and <= WorkspaceCommandRunner.MaxCommandCharacters)
            .ToArray();
    }

    public static string RemoveProposals(string response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return CommandPattern.Replace(response, "").Trim();
    }
}
