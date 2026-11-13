// Compares two folder/file paths the way the OS does (case-insensitive on Windows), ignoring a
// trailing separator.
// Depends on: nothing. Used by: MainPage Coding Agent (matching undo entries and file focus to
// project folders).

namespace SkaiDesktop.Core.Infrastructure;

public static class WorkspacePath
{
    public static bool AreSame(string left, string right)
    {
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(left)), Path.TrimEndingDirectorySeparator(Path.GetFullPath(right)), comparison);
    }
}
