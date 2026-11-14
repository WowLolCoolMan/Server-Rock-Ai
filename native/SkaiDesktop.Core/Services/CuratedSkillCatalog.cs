namespace SkaiDesktop.Core.Services;

/// <summary>
/// Authored discovery lanes for the public GitHub skill marketplace. These are
/// intentionally broad, reusable workflows rather than an unrestricted search
/// box. Results are still inspected as metadata before Skai offers them.
/// </summary>
public sealed record CuratedSkillLane(string Id, string Label, string Query, string Description);

public static class CuratedSkillCatalog
{
    public const int MaxResults = 250;

    public static IReadOnlyList<CuratedSkillLane> Lanes { get; } =
    [
        new("research", "Research", "(agent OR ai) (research OR literature OR summarize) skill", "Research, synthesis, and source-grounded summaries."),
        new("coding", "Coding", "(agent OR ai) (coding OR developer OR refactor OR debugging) skill", "Code review, debugging, refactoring, and implementation workflows."),
        new("writing", "Writing", "(agent OR ai) (writing OR editing OR copywriting) skill", "Writing, editing, outlining, and communication workflows."),
        new("data", "Data", "(agent OR ai) (data OR sql OR analytics OR spreadsheet) skill", "Data cleanup, SQL, analysis, and spreadsheet workflows."),
        new("documents", "Documents", "(agent OR ai) (document OR pdf OR report) skill", "Document extraction, reporting, and review workflows."),
        new("design", "Design", "(agent OR ai) (design OR ui OR ux OR accessibility) skill", "Product design, UI review, and accessibility workflows."),
        new("testing", "Testing", "(agent OR ai) (testing OR qa OR playwright OR evaluation) skill", "Testing, evaluation, browser, and release-quality workflows."),
        new("devops", "DevOps", "(agent OR ai) (devops OR deploy OR ci OR infrastructure) skill", "Build, CI, deployment, and operational workflows."),
        new("security", "Security", "(agent OR ai) (security OR threat-model OR audit) skill", "Security review, threat modeling, and hardening workflows."),
        new("productivity", "Productivity", "(agent OR ai) (productivity OR planning OR meeting) skill", "Planning, prioritization, and personal productivity workflows."),
        new("creative", "Creative", "(agent OR ai) (creative OR image OR video OR audio) skill", "Creative ideation and media-production workflows."),
        new("automation", "Automation", "(agent OR ai) (automation OR workflow OR integration) skill", "Repeatable automations and service-integration workflows.")
    ];

    public static IReadOnlyList<CuratedSkillLane> Select(string? query)
    {
        var value = query?.Trim() ?? string.Empty;
        if (value.Length == 0) return Lanes;
        return Lanes.Where(lane => lane.Label.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                                  lane.Description.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                                  lane.Query.Contains(value, StringComparison.OrdinalIgnoreCase)).ToArray();
    }
}
