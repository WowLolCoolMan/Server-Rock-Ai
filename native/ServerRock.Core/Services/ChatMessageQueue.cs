using ServerRock.Core.Models;

namespace ServerRock.Core.Services;

public sealed record QueuedChatRequest(string Text, IReadOnlyList<ChatAttachment>? Attachments);

/// <summary>Bounded FIFO for messages submitted while a response is active.</summary>
public sealed class ChatMessageQueue(int capacity = 8)
{
    private readonly Queue<QueuedChatRequest> _items = new();

    public int Count => _items.Count;

    public void Enqueue(string text, IReadOnlyList<ChatAttachment>? attachments = null)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 50_000 || text.Any(char.IsControl)) throw new ArgumentException("Queued message text is invalid.", nameof(text));
        if (attachments is { Count: > 16 }) throw new ArgumentException("A queued message has too many attachments.", nameof(attachments));
        if (_items.Count >= capacity) throw new InvalidOperationException("The message queue is full. Wait for a response to finish before adding another message.");
        _items.Enqueue(new QueuedChatRequest(text, attachments is null ? null : [.. attachments]));
    }

    public bool TryDequeue(out QueuedChatRequest? request)
    {
        if (_items.Count == 0) { request = null; return false; }
        request = _items.Dequeue();
        return true;
    }
}
