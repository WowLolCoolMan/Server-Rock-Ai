using SkaiDesktop.Core.Models;
using SkaiDesktop.Core.Services;
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
        Check(queue.Move(1, 0) && queue.Snapshot()[0].Text == "second" && queue.Snapshot()[1].Text == "first", "queue reorders bounded messages");
        Check(!queue.Move(0, 0) && !queue.Move(-1, 0) && !queue.Move(0, 2), "queue rejects invalid or no-op moves");
        Check(queue.TryDequeue(out var first) && first!.Text == "second" && first.Attachments is null, "queue preserves reordered FIFO order");
        Check(queue.TryDequeue(out var second) && second!.Text == "first" && second.Attachments!.Count == 1 && !queue.TryDequeue(out _), "queue drains and preserves attachments");
    }
}
