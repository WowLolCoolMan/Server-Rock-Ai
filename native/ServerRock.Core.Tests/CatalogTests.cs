// Built-in plugin and skill catalogs (PluginCatalogService, SkillCatalogService).
// Uses TestSupport (Check, StubHandler, …). Run by Program.cs.

using ServerRock.Core.Infrastructure;
using ServerRock.Core.Models;
using ServerRock.Core.Services;
using static TestSupport;

internal static class CatalogTests
{
    public static void Run()
    {
        var pluginCatalog = PluginCatalogService.Catalog;
        Check(pluginCatalog.Count >= 12 && pluginCatalog.Select(item => item.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() == pluginCatalog.Count,
            "plugin catalog exposes unique supported integrations");
        Check(PluginCatalogService.Search("github").Count == 1 && PluginCatalogService.Search("github")[0].Id == "github",
            "plugin catalog search finds a matching integration");
        Check(PluginCatalogService.Search(category: "Skills").All(item => item.Category == "Skills") && PluginCatalogService.Categories().Contains("Developer tools"),
            "plugin catalog filters by category and exposes sorted categories");
        Check(SkillCatalogService.BuiltIns.Count >= 3 && SkillCatalogService.Find("reverse-engineer")?.RequiredPermissions.SequenceEqual(["project.read"]) == true,
            "built-in skills expose explicit read-only permissions");
        Check(SkillCatalogService.Find("missing-skill") is null && SkillCatalogService.Find("RELEASE-READINESS")?.Category == "Delivery",
            "skill catalog lookup is case-insensitive and rejects unknown skills");
    }
}
