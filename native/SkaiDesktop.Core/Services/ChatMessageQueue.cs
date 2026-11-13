using SkaiDesktop.Core.Models;

namespace SkaiDesktop.Core.Services;

public sealed record QueuedChatRequest(string Text, IReadOnlyList<ChatAttachment>? Attachments);

/// <summary>Bounded FIFO for messages submitted while a response is active.</summary>
public sealed class ChatMessageQueue(int capacity = 8)
{
    public const int DefaultCapacity = 8;
    private readonly Queue<QueuedChatRequest> _items = new();

    public int Count => _items.Count;

    public IReadOnlyList<QueuedChatRequest> Snapshot() => _items.ToArray();

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

    public bool RemoveAt(int index)
    {
        if (index < 0 || index >= _items.Count) return false;
        var remaining = _items.ToArray();
        _items.Clear();
        for (var i = 0; i < remaining.Length; i++) if (i != index) _items.Enqueue(remaining[i]);
        return true;
    }

    public bool Move(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= _items.Count || toIndex < 0 || toIndex >= _items.Count || fromIndex == toIndex) return false;
        var remaining = _items.ToList();
        var item = remaining[fromIndex];
        remaining.RemoveAt(fromIndex);
        remaining.Insert(toIndex, item);
        _items.Clear();
        foreach (var queued in remaining) _items.Enqueue(queued);
        return true;
    }

    public void Clear() => _items.Clear();
}
