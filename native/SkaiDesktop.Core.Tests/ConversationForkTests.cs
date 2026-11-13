using SkaiDesktop.Core.Models;
using SkaiDesktop.Core.Services;
using static TestSupport;

internal static class ConversationForkTests
{
    public static void Run()
    {
        var messages = new List<ChatMessage>
        {
            new("user", "first", [new ChatAttachment("notes.txt", "local")]),
            new("assistant", "answer", Sources: [new ChatSource("Docs", "https://example.test/docs", "snippet")]),
            new("user", "later")
        };
        var prefix = ConversationForkService.CopyMessages(messages, throughIndex: 1);
        Check(prefix.Count == 2 && prefix[1].Content == "answer" && prefix[0].Attachments is not null && prefix[1].Sources is not null,
            "message fork copies the selected conversation prefix with local metadata");
        Check(!ReferenceEquals(prefix[0].Attachments, messages[0].Attachments) && !ReferenceEquals(prefix[1].Sources, messages[1].Sources),
            "message fork does not share mutable attachment or source lists with the original");
        var full = ConversationForkService.CopyMessages(messages);
        Check(full.Count == messages.Count, "full conversation fork preserves all messages");
        try
        {
            _ = ConversationForkService.CopyMessages(messages, throughIndex: messages.Count);
            throw new Exception("Expected an out-of-range message fork to be rejected.");
        }
        catch (ArgumentOutOfRangeException) { Check(true, "message fork rejects an out-of-range message index"); }
    }
}
