using System.Text.Json;
using ServerRock.Core.Models;

namespace ServerRock.Core.Infrastructure;

/// <summary>Atomically persists explicitly selected, unsent attachments by workspace.</summary>
public sealed class WorkspaceAttachmentDraftStore
{
    public const int MaxFilesPerWorkspace = 4;
    public const int MaxTotalCharactersPerWorkspace = 12_000_000;
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _path;

    public WorkspaceAttachmentDraftStore(string? path = null)
    {
        _path = path ?? Environment.GetEnvironmentVariable("SRAI_ATTACHMENT_DRAFTS_PATH") ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ServerRock", "AI", "attachment-drafts.json");
    }

    public IReadOnlyList<ChatAttachment> Load(string workspace)
    {
        if (string.IsNullOrWhiteSpace(workspace) || !File.Exists(_path)) return [];
        var all = ReadAll();
        return all.TryGetValue(workspace, out var attachments) ? Validate(workspace, attachments) : [];
    }

    public void Save(string workspace, IEnumerable<ChatAttachment> attachments)
    {
        if (string.IsNullOrWhiteSpace(workspace)) return;
        ArgumentNullException.ThrowIfNull(attachments);
        var validated = Validate(workspace, attachments.ToList());
        var all = File.Exists(_path) ? ReadAll() : new Dictionary<string, List<ChatAttachment>>(StringComparer.Ordinal);
        if (validated.Count == 0) all.Remove(workspace);
        else all[workspace] = validated.ToList();
        var json = BoundedFileReader.EnsureTextSize(JsonSerializer.Serialize(all, Options), "saved attachment drafts");
        AtomicFileWriter.WriteText(_path, json);
    }

    private Dictionary<string, List<ChatAttachment>> ReadAll()
    {
        try
        {
            var all = JsonSerializer.Deserialize<Dictionary<string, List<ChatAttachment>>>(BoundedFileReader.ReadText(_path), Options)
                ?? new Dictionary<string, List<ChatAttachment>>(StringComparer.Ordinal);
            foreach (var (workspace, attachments) in all.ToArray()) all[workspace] = Validate(workspace, attachments ?? []).ToList();
            return new Dictionary<string, List<ChatAttachment>>(all, StringComparer.Ordinal);
        }
        catch (JsonException error)
        {
            throw new InvalidDataException("The saved attachment drafts could not be read. The original file was left untouched.", error);
        }
    }

    private static IReadOnlyList<ChatAttachment> Validate(string workspace, IEnumerable<ChatAttachment> attachments)
    {
        var items = attachments.ToList();
        if (items.Count > MaxFilesPerWorkspace) throw new InvalidDataException($"Attachment drafts for {workspace} exceed the {MaxFilesPerWorkspace}-file limit.");
        if (items.Any(item => item is null || string.IsNullOrWhiteSpace(item.FileName) || item.Content is null))
            throw new InvalidDataException($"Attachment drafts for {workspace} contain an invalid attachment.");
        if (items.Sum(item => (long)item.Content.Length) > MaxTotalCharactersPerWorkspace)
            throw new InvalidDataException($"Attachment drafts for {workspace} exceed the local size limit.");
        return items;
    }
}
