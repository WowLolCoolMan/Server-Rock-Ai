// Keeps the last approved workspace file edits (workspace-undo.json) and undoes them
// only if the file still matches what was applied.
// Depends on: WorkspaceFileEditor, BoundedFileReader, AtomicFileWriter. Used by: MainPage undo history.

using System.Text.Json;

namespace SkaiDesktop.Core.Infrastructure;

/// <summary>Stores bounded, local undo records for explicitly approved workspace file changes.</summary>
public sealed class WorkspaceUndoStore
{
    public const int MaxEntries = 20;
    public const int MaxPreviousContentBytes = WorkspaceFileEditor.MaxCharacters * 4;
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _path;

    public WorkspaceUndoStore(string? path = null)
    {
        _path = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ServerRock", "AI", "workspace-undo.json");
    }

    public List<WorkspaceUndoEntry> Load()
    {
        if (!File.Exists(_path)) return [];
        try
        {
            var entries = JsonSerializer.Deserialize<List<WorkspaceUndoEntry>>(BoundedFileReader.ReadText(_path), Options) ?? [];
            return entries.Where(IsWellFormed)
                .OrderByDescending(entry => entry.CreatedUtc)
                .Take(MaxEntries)
                .ToList();
        }
        catch (JsonException error)
        {
            throw new InvalidDataException("Workspace undo history could not be read. The original file was left untouched.", error);
        }
        catch (InvalidDataException error)
        {
            throw new InvalidDataException("Workspace undo history could not be read. The original file was left untouched.", error);
        }
    }

    public void Add(IEnumerable<WorkspaceUndoEntry> existing, WorkspaceUndoEntry entry)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(entry);
        if (!IsWellFormed(entry)) throw new ArgumentException("The workspace undo entry is incomplete or exceeds the saved-file limit.", nameof(entry));
        var entries = existing.Where(IsWellFormed).Where(item => item.Id != entry.Id).Prepend(entry)
            .OrderByDescending(item => item.CreatedUtc).Take(MaxEntries).ToList();
        Save(entries);
    }

    public void Remove(IEnumerable<WorkspaceUndoEntry> existing, string id)
    {
        ArgumentNullException.ThrowIfNull(existing);
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("An undo entry ID is required.", nameof(id));
        Save(existing.Where(entry => entry.Id != id));
    }

    public void Undo(WorkspaceUndoEntry entry, WorkspaceFileEditor editor)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(editor);
        editor.Undo(entry.RootPath, entry.RelativePath,
            new WorkspaceEditReceipt(Path.GetFullPath(Path.Combine(entry.RootPath, entry.RelativePath)), entry.PreviousContent, entry.AppliedHash));
    }

    private void Save(IEnumerable<WorkspaceUndoEntry> entries)
    {
        var materialized = entries.Where(IsWellFormed).OrderByDescending(entry => entry.CreatedUtc).Take(MaxEntries).ToList();
        var json = BoundedFileReader.EnsureTextSize(JsonSerializer.Serialize(materialized, Options), "workspace undo history");
        AtomicFileWriter.WriteText(_path, json);
    }

    private static bool IsWellFormed(WorkspaceUndoEntry? entry) =>
        entry is not null && !string.IsNullOrWhiteSpace(entry.Id) && !string.IsNullOrWhiteSpace(entry.ProjectName) &&
        !string.IsNullOrWhiteSpace(entry.RootPath) && Path.IsPathRooted(entry.RootPath) &&
        !string.IsNullOrWhiteSpace(entry.RelativePath) && !Path.IsPathRooted(entry.RelativePath) &&
        !string.IsNullOrWhiteSpace(entry.AppliedHash) && entry.AppliedHash.Length == 64 &&
        (entry.PreviousContent?.Length ?? 0) <= MaxPreviousContentBytes;
}

public sealed record WorkspaceUndoEntry(
    string Id,
    DateTimeOffset CreatedUtc,
    string ProjectName,
    string RootPath,
    string RelativePath,
    byte[]? PreviousContent,
    string AppliedHash);
