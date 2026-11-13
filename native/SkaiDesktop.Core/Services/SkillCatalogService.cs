// Static list of built-in read-only workflow skills and their permissions.
// Used by: MainPage /skills and /reverse-engineer.

namespace SkaiDesktop.Core.Services;

public sealed record SkillDefinition(
    string Id,
    string Name,
    string Description,
    string Category,
    string Prompt,
    IReadOnlyList<string> RequiredPermissions);

public static class SkillCatalogService
{
    public static IReadOnlyList<SkillDefinition> BuiltIns { get; } =
    [
        new(
            "reverse-engineer",
            "Reverse engineer a project",
            "Produce a cited architecture, workflow, risk, and improvement report without modifying files.",
            "Analysis",
            "Reverse-engineer this project as a senior software archaeologist. Inspect the selected project files and produce a clear report with system purpose, architecture, runtime/data flow, persistence, integrations, testing posture, risks, and a prioritized improvement plan. Cite relevant file paths and distinguish observed facts from inferences. Do not modify files or run destructive commands.",
            ["project.read"]),
        new(
            "project-audit",
            "Audit project health",
            "Check project structure, tests, dependency boundaries, and obvious maintainability risks.",
            "Analysis",
            "Audit the selected project for maintainability and reliability. Review structure, dependency boundaries, test coverage signals, configuration, error handling, and release risks. Return findings grouped by severity with file-path evidence. Do not modify files or run destructive commands.",
            ["project.read"]),
        new(
            "release-readiness",
            "Review release readiness",
            "Check build, test, packaging, diagnostics, and release evidence before publishing.",
            "Delivery",
            "Prepare this release-readiness review for {{USER_NAME}} on {{DATE}}. Inspect build and test configuration, packaging, versioning, diagnostics, update/rollback behavior, and documented acceptance evidence. Return a fail-closed checklist with concrete gaps and the next safest actions. Do not modify files or publish artifacts.",
            ["project.read"])
    ];

    public static SkillDefinition? Find(string id) =>
        BuiltIns.FirstOrDefault(skill => skill.Id.Equals(id.Trim(), StringComparison.OrdinalIgnoreCase));
}
