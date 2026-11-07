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

        // Slash command catalog and suggestion filter (moved from MainPage into Core).
        Check(SlashCommandCatalog.Commands.Select(item => item.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == SlashCommandCatalog.Commands.Length
            && SlashCommandCatalog.Commands.All(item => item.Name.StartsWith('/') && item.Description.Length > 0),
            "slash command catalog has unique, described commands");
        Check(SlashCommandCatalog.Suggest("/mo", null).Any(item => item.Name == "/mode") && SlashCommandCatalog.Suggest("/", null).Length <= 16,
            "slash suggestions match by prefix and show at most 16 commands");
        Check(SlashCommandCatalog.Suggest("/mode", "c").Select(item => item.Name).SequenceEqual(["/mode chat", "/mode code"])
            && SlashCommandCatalog.Suggest("/web", "of").Single().Name == "/web off" && SlashCommandCatalog.Suggest("/help", "").Length == 0,
            "slash argument suggestions complete the full command text");
    }
}
