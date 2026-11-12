using ServerRock.Core.Infrastructure;
using ServerRock.Core.Models;
using ServerRock.Core.Services;
using static TestSupport;

internal static class ChatMessageQueueStoreTests
{
    public static void Run()
    {
        var path = Path.Combine(Path.GetTempPath(), $"server-rock-queue-{Guid.NewGuid():N}.json");
        try
        {
            var store = new ChatMessageQueueStore(path);
            store.Save([
                new QueuedChatRequest("first", [new ChatAttachment("notes.txt", "queued")]),
                new QueuedChatRequest("second", null)
            ]);
            var loaded = store.Load();
            Check(loaded.Count == 2 && loaded[0].Text == "first" && loaded[0].Attachments![0].Content == "queued", "queued messages persist with FIFO text and attachments");
            store.Save([]);
            Check(store.Load().Count == 0, "clearing the queue persists an empty queue");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
