using SkaiDesktop.Core.Infrastructure;
using SkaiDesktop.Core.Models;
using SkaiDesktop.Core.Services;
using static TestSupport;

internal static class ChatMessageQueueStoreTests
{
    public static void Run()
    {
        var path = Path.Combine(Path.GetTempPath(), $"server-rock-queue-{Guid.NewGuid():N}.json");
        var environmentPath = Path.Combine(Path.GetTempPath(), $"server-rock-queue-env-{Guid.NewGuid():N}.json");
        var previousEnvironmentPath = Environment.GetEnvironmentVariable("SRAI_QUEUE_PATH");
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

            Environment.SetEnvironmentVariable("SRAI_QUEUE_PATH", environmentPath);
            var environmentStore = new ChatMessageQueueStore();
            environmentStore.Save([new QueuedChatRequest("environment override", null)]);
            Check(environmentStore.Load().Single().Text == "environment override", "queue store honors the explicit disposable environment path");
        }
        finally
        {
            Environment.SetEnvironmentVariable("SRAI_QUEUE_PATH", previousEnvironmentPath);
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(environmentPath)) File.Delete(environmentPath);
        }
    }
}
