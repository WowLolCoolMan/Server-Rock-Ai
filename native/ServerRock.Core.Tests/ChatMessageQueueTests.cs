using ServerRock.Core.Models;
using ServerRock.Core.Services;
using static TestSupport;

internal static class ChatMessageQueueTests
{
    public static void Run()
    {
        var queue = new ChatMessageQueue(2);
        queue.Enqueue("first", [new ChatAttachment("notes.txt", "queued")]);
        queue.Enqueue("second");
        Check(queue.Count == 2, "queue accepts bounded follow-up messages");
        var refused = false;
        try { queue.Enqueue("third"); }
        catch (InvalidOperationException) { refused = true; }
        Check(refused, "queue refuses messages beyond its capacity");
        Check(queue.TryDequeue(out var first) && first!.Text == "first" && first.Attachments!.Count == 1, "queue preserves FIFO order and attachments");
        Check(queue.TryDequeue(out var second) && second!.Text == "second" && !queue.TryDequeue(out _), "queue drains and reports empty state");
    }
}
