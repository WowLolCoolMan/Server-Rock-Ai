using System.Text.Json;
using SkaiDesktop.Core.Infrastructure;

namespace SkaiDesktop.Core.Services;

/// <summary>Bounded, explicit local document corpus for retrieval. It never crawls files implicitly.</summary>
public sealed class DocumentCorpusStore
{
    public const int MaxDocuments = 512;
    public const int MaxContentCharacters = 200_000;
    public const int MaxTitleCharacters = 256;
    public const int MaxSourceCharacters = 1024;
    public const int MaxQueryCharacters = 512;
    private const int MaxSerializedCharacters = 64 * 1024 * 1024;
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true, MaxDepth = 8 };
    private readonly string _path;
    private readonly ICorpusVectorIndex _vectorIndex;

    public DocumentCorpusStore(string? path = null, ICorpusVectorIndex? vectorIndex = null)
    {
        _path = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ServerRock", "AI", "document-corpus.json");
        _vectorIndex = vectorIndex ?? new TokenVectorIndex();
    }

    public IReadOnlyList<CorpusDocument> Load() => Read().OrderByDescending(document => document.UpdatedAtUtc).ToArray();

    public CorpusDocument Upsert(string? id, string title, string source, string content, IEnumerable<string>? tags = null)
    {
        var documents = Read().ToList();
        var normalizedId = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("n") : Normalize(id, 128, "document identity");
        var existing = documents.SingleOrDefault(document => document.Id == normalizedId);
        var now = DateTimeOffset.UtcNow;
        var document = new CorpusDocument
        {
            Id = normalizedId,
            Title = Normalize(title, MaxTitleCharacters, "document title"),
            Source = Normalize(source, MaxSourceCharacters, "document source"),
            Content = NormalizeContent(content),
            Tags = NormalizeTags(tags),
            CreatedAtUtc = existing?.CreatedAtUtc ?? now,
            UpdatedAtUtc = now
        };
        if (existing is not null) documents[documents.IndexOf(existing)] = document;
        else documents.Add(document);
        Save(documents);
        return document;
    }

    public IReadOnlyList<CorpusSearchResult> Search(string? query, int limit = 12)
    {
        var normalized = query?.Trim() ?? "";
        if (normalized.Length > MaxQueryCharacters || normalized.Any(char.IsControl)) throw new ArgumentException("The corpus query is invalid.", nameof(query));
        if (limit is < 1 or > 50) throw new ArgumentOutOfRangeException(nameof(limit));
        var terms = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return Load().Select(document =>
        {
            var haystack = $"{document.Title}\n{document.Source}\n{document.Content}\n{string.Join(' ', document.Tags)}";
            var score = terms.Length == 0 ? 1 : terms.Sum(term => haystack.Contains(term, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
            var vectorScore = terms.Length == 0 ? 0 : _vectorIndex.Similarity(normalized, haystack);
            return new CorpusSearchResult(document, score, CreateSnippet(document.Content, terms), score + vectorScore * 2, _vectorIndex.Name);
        }).Where(result => terms.Length == 0 || result.Score > 0 || result.HybridScore > 0).OrderByDescending(result => result.HybridScore).ThenByDescending(result => result.Document.UpdatedAtUtc).Take(limit).ToArray();
    }

    public void Remove(string id)
    {
        var documents = Read().ToList();
        if (documents.RemoveAll(document => document.Id == Normalize(id, 128, "document identity")) == 0) throw new KeyNotFoundException("The corpus document does not exist.");
        Save(documents);
    }

    private List<CorpusDocument> Read()
    {
        if (!File.Exists(_path)) return [];
        try
        {
            var documents = JsonSerializer.Deserialize<List<CorpusDocument>>(BoundedFileReader.ReadText(_path), Options) ?? [];
            Validate(documents);
            return documents;
        }
        catch (JsonException exception) { throw new InvalidDataException("The local document corpus could not be read. The original file was left untouched.", exception); }
    }

    private void Save(IEnumerable<CorpusDocument> documents)
    {
        var list = documents.ToList();
        Validate(list);
        var json = JsonSerializer.Serialize(list, Options);
        if (json.Length > MaxSerializedCharacters) throw new InvalidDataException("The local document corpus exceeds its bounded size limit.");
        AtomicFileWriter.WriteText(_path, json);
    }

    private static void Validate(IReadOnlyList<CorpusDocument> documents)
    {
        if (documents.Count > MaxDocuments || documents.Select(document => document.Id).Distinct(StringComparer.Ordinal).Count() != documents.Count) throw new InvalidDataException("The local document corpus contains too many or duplicate documents.");
        foreach (var document in documents)
        {
            Normalize(document.Id, 128, "document identity"); Normalize(document.Title, MaxTitleCharacters, "document title"); Normalize(document.Source, MaxSourceCharacters, "document source"); NormalizeContent(document.Content); NormalizeTags(document.Tags);
        }
    }

    private static string NormalizeContent(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxContentCharacters || value.Any(character => char.IsControl(character) && character is not ('\r' or '\n' or '\t'))) throw new ArgumentException("The document content is invalid.");
        return value.Trim();
    }
    private static string Normalize(string value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength || value.Any(char.IsControl)) throw new ArgumentException($"The {label} is invalid.");
        return value.Trim();
    }
    private static List<string> NormalizeTags(IEnumerable<string>? tags)
    {
        var result = (tags ?? []).Select(tag => Normalize(tag, 64, "document tag")).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (result.Count > 16) throw new ArgumentException("A document can have at most 16 tags.");
        return result;
    }
    private static string CreateSnippet(string content, IReadOnlyList<string> terms)
    {
        if (content.Length <= 240 || terms.Count == 0) return content[..Math.Min(240, content.Length)];
        var index = terms.Select(term => content.IndexOf(term, StringComparison.OrdinalIgnoreCase)).Where(index => index >= 0).DefaultIfEmpty(0).Min();
        var start = Math.Max(0, index - 80); var length = Math.Min(240, content.Length - start);
        return (start > 0 ? "…" : "") + content.Substring(start, length) + (start + length < content.Length ? "…" : "");
    }
}

public sealed class CorpusDocument
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Source { get; set; } = "";
    public string Content { get; set; } = "";
    public List<string> Tags { get; set; } = [];
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public interface ICorpusVectorIndex
{
    string Name { get; }
    double Similarity(string query, string document);
}

/// <summary>Deterministic local sparse-vector index. It is vector-ready without requiring a cloud embedding service.</summary>
public sealed class TokenVectorIndex : ICorpusVectorIndex
{
    public string Name => "hybrid-token-vector";

    public double Similarity(string query, string document)
    {
        var left = Build(query); var right = Build(document);
        if (left.Count == 0 || right.Count == 0) return 0;
        var dot = left.Sum(pair => right.TryGetValue(pair.Key, out var value) ? pair.Value * value : 0);
        var leftNorm = Math.Sqrt(left.Sum(pair => pair.Value * pair.Value));
        var rightNorm = Math.Sqrt(right.Sum(pair => pair.Value * pair.Value));
        return leftNorm == 0 || rightNorm == 0 ? 0 : dot / (leftNorm * rightNorm);
    }

    private static Dictionary<string, double> Build(string value) => value.Split([' ', '\r', '\n', '\t', '.', ',', ':', ';', '/', '(', ')', '[', ']', '{', '}', '-', '_'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(token => token.Length > 1)
        .GroupBy(token => token.ToLowerInvariant())
        .ToDictionary(group => group.Key, group => (double)group.Count(), StringComparer.Ordinal);
}

public sealed record CorpusSearchResult(CorpusDocument Document, int Score, string Snippet, double HybridScore = 0, string RetrievalMode = "lexical");
