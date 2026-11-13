namespace ServerRock.Core.Services;

/// <summary>Bounded, branch-aware history for user-visible native workspace routes.</summary>
public sealed class NavigationHistory
{
    public const int MaxEntries = 32;
    private readonly List<string> _entries = [];
    private int _index = -1;

    public NavigationHistory(string initialRoute)
    {
        AddRoute(initialRoute);
    }

    public string Current => _entries[_index];
    public bool CanGoBack => _index > 0;
    public bool CanGoForward => _index >= 0 && _index < _entries.Count - 1;

    public void Record(string route)
    {
        ValidateRoute(route);
        if (string.Equals(Current, route, StringComparison.Ordinal)) return;
        if (_index < _entries.Count - 1) _entries.RemoveRange(_index + 1, _entries.Count - _index - 1);
        AddRoute(route);
    }

    public string? PeekBack() => CanGoBack ? _entries[_index - 1] : null;
    public string? PeekForward() => CanGoForward ? _entries[_index + 1] : null;

    public string? MoveBack()
    {
        if (!CanGoBack) return null;
        _index--;
        return Current;
    }

    public string? MoveForward()
    {
        if (!CanGoForward) return null;
        _index++;
        return Current;
    }

    private void AddRoute(string route)
    {
        ValidateRoute(route);
        _entries.Add(route);
        _index = _entries.Count - 1;
        if (_entries.Count > MaxEntries)
        {
            _entries.RemoveAt(0);
            _index--;
        }
    }

    private static void ValidateRoute(string route)
    {
        if (string.IsNullOrWhiteSpace(route) || route.Length > 128 || route.Any(char.IsControl))
            throw new ArgumentException("A navigation route is invalid.", nameof(route));
    }
}
