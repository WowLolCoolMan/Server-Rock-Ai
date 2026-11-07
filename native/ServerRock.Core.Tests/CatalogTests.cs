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
        // Only integrations that really work are marked built in, and each says where to find it;
        // the rest must read "Not available yet" so the store never fakes an install.
        Check(pluginCatalog.Where(item => item.IsBuiltIn).Select(item => item.Id).Order().SequenceEqual(["github", "image-tools", "terminal-tools", "web-search"]) &&
              pluginCatalog.All(item => item.IsBuiltIn == !string.IsNullOrWhiteSpace(item.WhereToFind)),
            "plugin catalog marks only connected integrations as built in, with a location");
        Check(pluginCatalog.Single(item => item.Id == "gmail").ListLabel.EndsWith("· Not available yet", StringComparison.Ordinal) &&
              pluginCatalog.Single(item => item.Id == "github").ListLabel.EndsWith("· Built in", StringComparison.Ordinal),
            "plugin catalog list labels show built-in or not-available status");
        Check(SkillCatalogService.BuiltIns.Count >= 3 && SkillCatalogService.Find("reverse-engineer")?.RequiredPermissions.SequenceEqual(["project.read"]) == true,
            "built-in skills expose explicit read-only permissions");
        Check(SkillCatalogService.Find("missing-skill") is null && SkillCatalogService.Find("RELEASE-READINESS")?.Category == "Delivery",
            "skill catalog lookup is case-insensitive and rejects unknown skills");
    }
}
