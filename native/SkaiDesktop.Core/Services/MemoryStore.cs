using System.Text.Json;
using SkaiDesktop.Core.Infrastructure;
using SkaiDesktop.Core.Models;

namespace SkaiDesktop.Core.Services;

/// <summary>Explicit, local-only persistent memory. Nothing is learned or saved implicitly.</summary>
public sealed class MemoryStore
{
    public const int MaxEntries = 512;
    public const int MaxContentCharacters = 20_000;
    public const int MaxTags = 16;
    private const int MaxSerializedCharacters = 12 * 1024 * 1024;
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true, MaxDepth = 8 };
    private readonly string _path;

    public MemoryStore(string? path = null)
    {
        _path = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ServerRock", "AI", "memory.json");
    }

    public IReadOnlyList<MemoryEntry> Load()
    {
        if (!File.Exists(_path)) return [];
        try
        {
            var entries = JsonSerializer.Deserialize<List<MemoryEntry>>(BoundedFileReader.ReadText(_path), Options) ?? [];
            Validate(entries);
            return entries.Select(Copy).OrderByDescending(entry => entry.UpdatedAtUtc).ToArray();
        }
        catch (JsonException exception) { throw new InvalidDataException("Persistent memory could not be read. The original file was left untouched.", exception); }
    }

    public MemoryEntry Add(string content, string scope, string source, string approvalId, IEnumerable<string>? tags = null)
    {
        var now = DateTimeOffset.UtcNow;
        var entry = new MemoryEntry { Scope = NormalizeText(scope, 128, "memory scope"), Content = NormalizeContent(content), Source = NormalizeText(source, 256, "memory source"), ApprovalId = NormalizeText(approvalId, 128, "memory approval"), Tags = NormalizeTags(tags), CreatedAtUtc = now, UpdatedAtUtc = now };
        var entries = Load().ToList();
        entries.Add(entry);
        Save(entries);
        return Copy(entry);
    }

    public MemoryEntry Update(string id, string content, string scope, string source, string approvalId, IEnumerable<string>? tags = null)
    {
        var entries = Load().ToList();
        var entry = entries.SingleOrDefault(item => item.Id == id) ?? throw new KeyNotFoundException("The memory entry does not exist.");
        entry.Content = NormalizeContent(content);
        entry.Scope = NormalizeText(scope, 128, "memory scope");
        entry.Source = NormalizeText(source, 256, "memory source");
        entry.ApprovalId = NormalizeText(approvalId, 128, "memory approval");
        entry.Tags = NormalizeTags(tags);
        entry.UpdatedAtUtc = DateTimeOffset.UtcNow;
        Save(entries);
        return Copy(entry);
    }

    public void SetEnabled(string id, bool enabled)
    {
        var entries = Load().ToList();
        var entry = entries.SingleOrDefault(item => item.Id == id) ?? throw new KeyNotFoundException("The memory entry does not exist.");
        entry.Enabled = enabled;
        entry.UpdatedAtUtc = DateTimeOffset.UtcNow;
        Save(entries);
    }

    public void Remove(string id)
    {
        var entries = Load().ToList();
        if (entries.RemoveAll(item => item.Id == id) == 0) throw new KeyNotFoundException("The memory entry does not exist.");
        Save(entries);
    }

    public IReadOnlyList<MemoryEntry> Search(string? query, string? scope = null, bool enabledOnly = true)
    {
        var normalizedQuery = query?.Trim() ?? "";
        if (normalizedQuery.Length > 512 || normalizedQuery.Any(char.IsControl)) throw new ArgumentException("The memory search query is invalid.", nameof(query));
        if (scope is not null) NormalizeText(scope, 128, "memory scope");
        return Load().Where(entry => (!enabledOnly || entry.Enabled) && (scope is null || entry.Scope.Equals(scope, StringComparison.Ordinal)) && (normalizedQuery.Length == 0 || entry.Content.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) || entry.Tags.Any(tag => tag.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)))).ToArray();
    }

    /// <summary>Returns a validated, local-only JSON backup without changing the store.</summary>
    public string ExportJson()
    {
        var entries = Load().Select(Copy).ToList();
        Validate(entries);
        return JsonSerializer.Serialize(new MemoryBackup
        {
            Format = "server-rock-memory-v1",
            ExportedAtUtc = DateTimeOffset.UtcNow,
            Entries = entries
        }, Options);
    }

    public MemoryImportPlan PlanImport(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Length > MaxSerializedCharacters)
            throw new InvalidDataException("The memory backup is empty or exceeds the bounded size limit.");
        MemoryBackup backup;
        try { backup = JsonSerializer.Deserialize<MemoryBackup>(json, Options) ?? throw new InvalidDataException("The memory backup is empty."); }
        catch (JsonException exception) { throw new InvalidDataException("The memory backup is not valid JSON.", exception); }
        if (!string.Equals(backup.Format, "server-rock-memory-v1", StringComparison.Ordinal) || backup.Entries is null)
            throw new InvalidDataException("The memory backup format is unsupported.");
        Validate(backup.Entries);
        var existingIds = Load().Select(entry => entry.Id).ToHashSet(StringComparer.Ordinal);
        var incoming = backup.Entries.Select(Copy).Where(entry => !existingIds.Contains(entry.Id)).ToList();
        return new MemoryImportPlan(incoming, backup.Entries.Count - incoming.Count);
    }

    public int ApplyImport(MemoryImportPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var existing = Load().ToList();
        var existingIds = existing.Select(entry => entry.Id).ToHashSet(StringComparer.Ordinal);
        var additions = plan.Entries.Select(Copy).Where(entry => existingIds.Add(entry.Id)).ToList();
        if (existing.Count + additions.Count > MaxEntries) throw new InvalidDataException("Importing this backup would exceed the memory entry limit.");
        Save(existing.Concat(additions));
        return additions.Count;
    }

    private void Save(IEnumerable<MemoryEntry> entries)
    {
        var list = entries.Select(Copy).ToList();
        Validate(list);
        var json = JsonSerializer.Serialize(list, Options);
        if (json.Length > MaxSerializedCharacters) throw new InvalidDataException("Persistent memory exceeds the bounded size limit.");
        AtomicFileWriter.WriteText(_path, json);
    }

    private static void Validate(IReadOnlyList<MemoryEntry> entries)
    {
        if (entries.Count > MaxEntries || entries.Select(entry => entry.Id).Distinct(StringComparer.Ordinal).Count() != entries.Count) throw new InvalidDataException("Persistent memory contains too many or duplicate entries.");
        foreach (var entry in entries)
        {
            NormalizeText(entry.Id, 128, "memory identity");
            NormalizeText(entry.Scope, 128, "memory scope");
            NormalizeContent(entry.Content);
            NormalizeText(entry.Source, 256, "memory source");
            NormalizeText(entry.ApprovalId, 128, "memory approval");
            NormalizeTags(entry.Tags);
        }
    }

    private static string NormalizeContent(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxContentCharacters || value.Any(character => char.IsControl(character) && character is not ('\r' or '\n' or '\t'))) throw new ArgumentException("The memory content is invalid.");
        return value.Trim();
    }
    private static string NormalizeText(string value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength || value.Any(char.IsControl)) throw new ArgumentException($"The {label} is invalid.");
        return value.Trim();
    }
    private static List<string> NormalizeTags(IEnumerable<string>? tags)
    {
        var values = (tags ?? []).Select(tag => NormalizeText(tag, 64, "memory tag")).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (values.Count > MaxTags) throw new ArgumentException($"A memory entry can have at most {MaxTags} tags.");
        return values;
    }
    private static MemoryEntry Copy(MemoryEntry source) => new() { Id = source.Id, Scope = source.Scope, Content = source.Content, Source = source.Source, ApprovalId = source.ApprovalId, Tags = source.Tags.ToList(), Enabled = source.Enabled, CreatedAtUtc = source.CreatedAtUtc, UpdatedAtUtc = source.UpdatedAtUtc };

    private sealed class MemoryBackup
    {
        public string Format { get; set; } = "server-rock-memory-v1";
        public DateTimeOffset ExportedAtUtc { get; set; }
        public List<MemoryEntry> Entries { get; set; } = [];
    }
}

public sealed record MemoryImportPlan(IReadOnlyList<MemoryEntry> Entries, int SkippedDuplicates);
