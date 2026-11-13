using ServerRock.Core.Models;
using ServerRock.Core.Services;
using static TestSupport;

internal static class ConversationMessageSearchTests
{
    public static void Run()
    {
        var messages = new List<ChatMessage>
        {
            new("user", "Review the release checklist"),
            new("assistant", "The release checklist has three items."),
            new("user", "Unrelated follow-up")
        };
        var hits = ConversationMessageSearchService.Search(messages, "CHECKLIST");
        Check(hits.Count == 2 && hits[0].MessageIndex == 0 && hits[1].Role == "assistant", "conversation message search finds matching user and assistant turns");
        Check(ConversationMessageSearchService.Search(messages, "").Count == 0, "conversation message search treats blank queries as empty");
        try
        {
            _ = ConversationMessageSearchService.Search(messages, new string('x', ConversationMessageSearchService.MaxQueryCharacters + 1));
            throw new Exception("Expected an oversized message search query to be rejected.");
        }
        catch (ArgumentException) { Check(true, "conversation message search bounds query length"); }
        var longMessage = new ChatMessage("assistant", new string('x', ConversationMessageSearchService.MaxPreviewCharacters + 20));
        Check(ConversationMessageSearchService.Search([longMessage], "xxx")[0].Preview.Length == ConversationMessageSearchService.MaxPreviewCharacters + 1,
            "conversation message search bounds result previews");
    }
}
