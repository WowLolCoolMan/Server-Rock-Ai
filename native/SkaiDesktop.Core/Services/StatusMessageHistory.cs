namespace SkaiDesktop.Core.Services;

/// <summary>Retains a bounded, in-memory history of actionable UI status messages.</summary>
public sealed class StatusMessageHistory
{
    public const int MaxEntries = 50;
    public const int MaxMessageCharacters = 2_000;
    private readonly int _capacity;
    private readonly List<string> _entries = [];

    public StatusMessageHistory(int capacity = MaxEntries)
    {
        if (capacity is < 1 or > MaxEntries) throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
    }

    public IReadOnlyList<string> Snapshot() => _entries.ToArray();

    public void Record(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        var normalized = message.Trim();
        if (normalized.Length > MaxMessageCharacters)
            normalized = normalized[..MaxMessageCharacters] + "…";
        if (_entries.Count > 0 && string.Equals(_entries[0], normalized, StringComparison.Ordinal)) return;
        _entries.Insert(0, normalized);
        if (_entries.Count > _capacity) _entries.RemoveAt(_entries.Count - 1);
    }
}
