// Static list of supported plugins/integrations with search and categories.
// Used by: MainPage plugin store dialog.

namespace ServerRock.Core.Services;

public sealed record PluginDefinition(
    string Id,
    string Name,
    string Category,
    string Description,
    string Access,
    string Display,
    bool IsAvailable = true);

public static class PluginCatalogService
{
    public static IReadOnlyList<PluginDefinition> Catalog { get; } =
    [
        new("github", "GitHub", "Developer tools", "Browse repositories, review diffs, and commit or push approved changes.", "User-authorized repositories", "GitHub · Browse repos, diffs, and push approved changes"),
        new("gmail", "Gmail", "Communication", "Find and summarize messages when you explicitly connect an account.", "Email metadata and message content", "Gmail · Read and manage email"),
        new("google-drive", "Google Drive", "Productivity", "Find documents and use selected files as context.", "Files you choose", "Google Drive · Drive, Docs, Sheets, and Slides"),
        new("outlook-email", "Outlook Email", "Communication", "Triage inboxes and draft responses for your review.", "Email metadata and message content", "Outlook Email · Triage inboxes"),
        new("calendar", "Calendar", "Productivity", "Read availability and prepare event drafts.", "Calendar events you authorize", "Calendar · Read and draft events"),
        new("slack", "Slack", "Communication", "Search channels and summarize selected conversations.", "Authorized workspace messages", "Slack · Search and summarize channels"),
        new("notion", "Notion", "Productivity", "Find pages and create drafts in selected workspaces.", "Pages and databases you authorize", "Notion · Search and draft pages"),
        new("linear", "Linear", "Developer tools", "Review issues and prepare project updates.", "Issues and projects you authorize", "Linear · Issues, projects, and updates"),
        new("web-search", "Web Search", "Research", "Search the public web when you turn it on for a chat.", "Your query and returned snippets", "Web Search · Search the public web"),
        new("image-tools", "Image Tools", "Creative", "Connect a local image workflow for generation and editing.", "Local prompts and workflow output", "Image Tools · Local image generation"),
        new("terminal-tools", "Terminal Tools", "Developer tools", "Expose the guarded project terminal to coding workflows.", "Selected project folder only", "Terminal Tools · Run approved project commands"),
        new("skills-pack", "Skills Pack", "Skills", "Install reusable prompt and workflow skills from a trusted local folder.", "Skill files you explicitly import", "Skills Pack · Reusable local workflows")
    ];

    public static IReadOnlyList<PluginDefinition> Search(string? query = null, string? category = null)
    {
        var term = query?.Trim() ?? string.Empty;
        var selectedCategory = category?.Trim() ?? string.Empty;
        return Catalog
            .Where(item => item.IsAvailable &&
                (term.Length == 0 || item.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                 item.Description.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                 item.Category.Contains(term, StringComparison.OrdinalIgnoreCase)) &&
                (selectedCategory.Length == 0 || selectedCategory.Equals("All categories", StringComparison.OrdinalIgnoreCase) ||
                 item.Category.Equals(selectedCategory, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(item => item.Category)
            .ThenBy(item => item.Name)
            .ToArray();
    }

    public static IReadOnlyList<string> Categories() =>
        Catalog.Where(item => item.IsAvailable)
            .Select(item => item.Category)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item)
            .ToArray();
}
