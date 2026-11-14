using System.Reflection;
using System.Text.Json;

namespace SkaiDesktop.Core.Services;

public sealed record CuratedSkillEntry(string Id, string Name, string Description, string Url, string Repository, string Path);

public static class AuthoredSkillCatalog
{
    private static readonly Lazy<IReadOnlyList<CuratedSkillEntry>> Loaded = new(Load);

    public static IReadOnlyList<CuratedSkillEntry> Entries => Loaded.Value;

    public static string LaneFor(CuratedSkillEntry entry)
    {
        var haystack = $"{entry.Name} {entry.Description} {entry.Path}".ToLowerInvariant();
        if (ContainsAny(haystack, "security", "threat", "audit", "privacy", "compliance")) return "Security";
        if (ContainsAny(haystack, "test", "qa", "playwright", "eval", "coverage", "debug")) return "Testing";
        if (ContainsAny(haystack, "deploy", "devops", "docker", "kubernetes", "ci", "infrastructure", "cloud")) return "DevOps";
        if (ContainsAny(haystack, "data", "sql", "database", "analytics", "spreadsheet", "pandas", "polars")) return "Data";
        if (ContainsAny(haystack, "pdf", "docx", "document", "report", "slide", "xlsx")) return "Documents";
        if (ContainsAny(haystack, "design", "frontend", "ui", "ux", "figma", "accessibility")) return "Design";
        if (ContainsAny(haystack, "image", "video", "audio", "creative", "art")) return "Creative";
        if (ContainsAny(haystack, "research", "literature", "paper", "science", "citation")) return "Research";
        if (ContainsAny(haystack, "write", "copy", "comms", "blog", "content")) return "Writing";
        if (ContainsAny(haystack, "automation", "workflow", "integration", "mcp", "api")) return "Automation";
        if (ContainsAny(haystack, "plan", "meeting", "productivity", "project")) return "Productivity";
        return "Coding";
    }

    private static IReadOnlyList<CuratedSkillEntry> Load()
    {
        var resourceName = typeof(AuthoredSkillCatalog).Assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith("authored-skill-catalog.json", StringComparison.OrdinalIgnoreCase));
        if (resourceName is null) return Array.Empty<CuratedSkillEntry>();
        using var stream = typeof(AuthoredSkillCatalog).Assembly.GetManifestResourceStream(resourceName);
        return stream is null
            ? Array.Empty<CuratedSkillEntry>()
            : JsonSerializer.Deserialize<List<CuratedSkillEntry>>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }

    private static bool ContainsAny(string value, params string[] terms) => terms.Any(value.Contains);
}
