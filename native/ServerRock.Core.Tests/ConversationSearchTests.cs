using ServerRock.Core.Models;
using ServerRock.Core.Services;
using static TestSupport;

internal static class ConversationSearchTests
{
    public static void Run()
    {
        var sessions = new[]
        {
            new ChatSession { Title = "Planning", FolderName = "Work", Tags = ["priority"], UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-1), Messages = [new ChatMessage("user", "Discuss launch", [new ChatAttachment("roadmap.pdf", "text")])] },
            new ChatSession { Title = "Recipes", UpdatedAt = DateTimeOffset.UtcNow, Messages = [new ChatMessage("assistant", "Use basil", Sources: [new ChatSource("Cooking notes", "https://example.org/cooking", "notes")])] },
            new ChatSession { Title = "Archived project", Archived = true, UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-2) }
        };
        Check(ConversationSearchService.Search(sessions, "priority").Single().Session.Title == "Planning", "conversation search finds tags");
        Check(ConversationSearchService.Search(sessions, "roadmap.pdf").Single().Session.Title == "Planning", "conversation search finds attachment names");
        Check(ConversationSearchService.Search(sessions, "ROADMAP.PDF").Single().Session.Title == "Planning", "conversation search is culture independent");
        var sourceHit = ConversationSearchService.Search(sessions, "cooking").Single();
        Check(sourceHit.Session.Title == "Recipes" && sourceHit.Display.Contains("Recipes", StringComparison.Ordinal), "conversation search finds source metadata and builds a bounded display");
        Check(ConversationSearchService.Search(sessions, "").Count == 3 && ConversationSearchService.Search(sessions, "")[0].Session.Title == "Recipes", "empty conversation search returns recent chats first");
        var rejected = false;
        try { ConversationSearchService.Search(sessions, new string('x', ConversationSearchService.MaxQueryCharacters + 1)); }
        catch (ArgumentException) { rejected = true; }
        Check(rejected, "conversation search bounds query text");
    }
}
