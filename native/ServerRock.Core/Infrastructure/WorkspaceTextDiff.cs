// Bounded line diff used to preview a proposed file replacement.
// Used by: MainPage Coding Agent change review.

namespace ServerRock.Core.Infrastructure;

public enum WorkspaceDiffLineKind
{
    Context,
    Removed,
    Added,
    Omitted,
    Metadata
}

public sealed record WorkspaceDiffLine(WorkspaceDiffLineKind Kind, int? OldLine, int? NewLine, string Text);

public sealed record WorkspaceTextDiffResult(
    IReadOnlyList<WorkspaceDiffLine> Lines,
    int BeforeLineCount,
    int AfterLineCount,
    int AddedLineCount,
    int RemovedLineCount,
    bool IsTooLarge,
    bool HasTextChanges);

/// <summary>Creates a bounded, line-oriented preview for a proposed workspace-file replacement.</summary>
public static class WorkspaceTextDiff
{
    public const int MaxComparableLines = 1_200;
    public const int DefaultContextLines = 3;

    public static WorkspaceTextDiffResult Create(string before, string after, int contextLines = DefaultContextLines)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        if (contextLines < 0) throw new ArgumentOutOfRangeException(nameof(contextLines));

        var oldLines = SplitLines(before);
        var newLines = SplitLines(after);
        if (oldLines.Length > MaxComparableLines || newLines.Length > MaxComparableLines)
            return new([], oldLines.Length, newLines.Length, 0, 0, true, !string.Equals(before, after, StringComparison.Ordinal));

        var oldHasFinalNewline = EndsWithNewline(before);
        var newHasFinalNewline = EndsWithNewline(after);
        var lcs = new int[oldLines.Length + 1, newLines.Length + 1];
        for (var oldIndex = oldLines.Length - 1; oldIndex >= 0; oldIndex--)
        for (var newIndex = newLines.Length - 1; newIndex >= 0; newIndex--)
            lcs[oldIndex, newIndex] = string.Equals(oldLines[oldIndex], newLines[newIndex], StringComparison.Ordinal)
                ? lcs[oldIndex + 1, newIndex + 1] + 1
                : Math.Max(lcs[oldIndex + 1, newIndex], lcs[oldIndex, newIndex + 1]);

        var full = new List<WorkspaceDiffLine>(oldLines.Length + newLines.Length + 1);
        var i = 0;
        var j = 0;
        var added = 0;
        var removed = 0;
        while (i < oldLines.Length || j < newLines.Length)
        {
            if (i < oldLines.Length && j < newLines.Length && string.Equals(oldLines[i], newLines[j], StringComparison.Ordinal))
            {
                full.Add(new(WorkspaceDiffLineKind.Context, i + 1, j + 1, oldLines[i]));
                i++;
                j++;
            }
            else if (i < oldLines.Length && (j == newLines.Length || lcs[i + 1, j] >= lcs[i, j + 1]))
            {
                full.Add(new(WorkspaceDiffLineKind.Removed, i + 1, null, oldLines[i]));
                removed++;
                i++;
            }
            else
            {
                full.Add(new(WorkspaceDiffLineKind.Added, null, j + 1, newLines[j]));
                added++;
                j++;
            }
        }

        if (oldHasFinalNewline != newHasFinalNewline)
            full.Add(new(WorkspaceDiffLineKind.Metadata, null, null, oldHasFinalNewline
                ? "No newline at end of proposed file"
                : "No newline at end of current file"));

        var changeIndices = full.Select((line, index) => (line, index))
            .Where(item => item.line.Kind is WorkspaceDiffLineKind.Added or WorkspaceDiffLineKind.Removed or WorkspaceDiffLineKind.Metadata)
            .Select(item => item.index)
            .ToArray();
        if (changeIndices.Length == 0)
            return new([], oldLines.Length, newLines.Length, added, removed, false, false);
        var visible = new List<WorkspaceDiffLine>();
        var visibleThrough = -1;
        foreach (var changeIndex in changeIndices)
        {
            var start = Math.Max(0, changeIndex - contextLines);
            var end = Math.Min(full.Count - 1, changeIndex + contextLines);
            if (start > visibleThrough + 1)
            {
                var omitted = full.Skip(visibleThrough + 1).Take(start - visibleThrough - 1).Count(line => line.Kind == WorkspaceDiffLineKind.Context);
                if (omitted > 0) visible.Add(new(WorkspaceDiffLineKind.Omitted, null, null, $"… {omitted} unchanged line(s) omitted …"));
            }
            for (var index = Math.Max(start, visibleThrough + 1); index <= end; index++) visible.Add(full[index]);
            visibleThrough = Math.Max(visibleThrough, end);
        }
        if (visibleThrough < full.Count - 1)
        {
            var omitted = full.Skip(visibleThrough + 1).Count(line => line.Kind == WorkspaceDiffLineKind.Context);
            if (omitted > 0) visible.Add(new(WorkspaceDiffLineKind.Omitted, null, null, $"… {omitted} unchanged line(s) omitted …"));
        }

        return new(visible, oldLines.Length, newLines.Length, added, removed, false,
            added > 0 || removed > 0 || oldHasFinalNewline != newHasFinalNewline);
    }

    private static string[] SplitLines(string text)
    {
        if (text.Length == 0) return [];
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
        return EndsWithNewline(text) ? lines[..^1] : lines;
    }

    private static bool EndsWithNewline(string text) => text.EndsWith('\n') || text.EndsWith('\r');
}
