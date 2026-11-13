namespace SkaiDesktop.Core.Services;

public sealed class WorkspaceDraftStore
{
    public const int MaxDraftCharacters = 100_000;
    private readonly Dictionary<string, string> _drafts = new(StringComparer.Ordinal);

    public WorkspaceDraftStore(IReadOnlyDictionary<string, string>? initial = null)
    {
        if (initial is null) return;
        foreach (var (workspace, draft) in initial)
            Set(workspace, draft);
    }

    public string Get(string workspace)
    {
        return string.IsNullOrWhiteSpace(workspace) ? string.Empty : _drafts.GetValueOrDefault(workspace, string.Empty);
    }

    public void Set(string workspace, string? draft)
    {
        if (string.IsNullOrWhiteSpace(workspace)) return;
        var value = draft ?? string.Empty;
        _drafts[workspace] = value.Length <= MaxDraftCharacters ? value : value[..MaxDraftCharacters];
    }

    public void Clear(string workspace) => Set(workspace, string.Empty);

    public Dictionary<string, string> Snapshot() => new(_drafts, StringComparer.Ordinal);
}
