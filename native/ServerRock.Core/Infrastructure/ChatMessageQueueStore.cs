using System.Text.Json;
using ServerRock.Core.Models;
using ServerRock.Core.Services;

namespace ServerRock.Core.Infrastructure;

/// <summary>Atomically persists only queued, not active, chat requests locally.</summary>
public sealed class ChatMessageQueueStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _path;

    public ChatMessageQueueStore(string? path = null)
    {
        _path = path ?? Environment.GetEnvironmentVariable("SRAI_QUEUE_PATH") ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ServerRock", "AI", "queued-messages.json");
    }

    public IReadOnlyList<QueuedChatRequest> Load()
    {
        if (!File.Exists(_path)) return [];
        try
        {
            var requests = JsonSerializer.Deserialize<List<QueuedChatRequest>>(BoundedFileReader.ReadText(_path), Options) ?? [];
            var validated = new ChatMessageQueue(ChatMessageQueue.DefaultCapacity);
            foreach (var request in requests)
            {
                if (request is null) throw new InvalidDataException("The saved message queue contains an empty request.");
                validated.Enqueue(request.Text, request.Attachments);
            }
            return validated.Snapshot();
        }
        catch (JsonException error)
        {
            throw new InvalidDataException("The saved message queue could not be read. The original file was left untouched.", error);
        }
    }

    public void Save(IEnumerable<QueuedChatRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);
        var validated = new ChatMessageQueue(ChatMessageQueue.DefaultCapacity);
        foreach (var request in requests)
        {
            if (request is null) throw new InvalidDataException("The message queue contains an empty request.");
            validated.Enqueue(request.Text, request.Attachments);
        }
        var json = BoundedFileReader.EnsureTextSize(JsonSerializer.Serialize(validated.Snapshot(), Options), "saved message queue");
        AtomicFileWriter.WriteText(_path, json);
    }
}
