using System.Text;

namespace ServerRock.Core.Infrastructure;

/// <summary>Reads a small, safe text-only sample from folders the user explicitly selected.</summary>
public static class WorkspaceFileContext
{
    public const int MaxFiles = 16;
    public const int MaxFileCharacters = 8_000;
    public const int MaxTotalCharacters = 48_000;

    private static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".svn", ".hg", "node_modules", "vendor", "bin", "obj", "dist", "build",
        ".venv", "venv", "__pycache__", ".idea", ".vscode", "target"
    };

    private static readonly HashSet<string> ExcludedFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ".env", ".env.local", ".env.production", "secrets.json", "credentials.json", "id_rsa", "id_ed25519"
    };

    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".csproj", ".sln", ".xaml", ".xml", ".json", ".md", ".txt", ".yml", ".yaml",
        ".toml", ".ini", ".props", ".targets", ".razor", ".html", ".css", ".js", ".jsx", ".ts",
        ".tsx", ".py", ".sh", ".ps1", ".sql", ".go", ".rs", ".java", ".kt", ".swift", ".c",
        ".h", ".cpp", ".hpp", ".vue", ".svelte", ".gitignore", ".editorconfig"
    };

    public static IReadOnlyList<(string Path, string Content)> ReadSelectedFiles(IEnumerable<string> selectedFolders)
    {
        ArgumentNullException.ThrowIfNull(selectedFolders);
        var result = new List<(string Path, string Content)>();
        var totalCharacters = 0;
        foreach (var selected in selectedFolders)
        {
            if (result.Count >= MaxFiles || totalCharacters >= MaxTotalCharacters) break;
            if (string.IsNullOrWhiteSpace(selected)) continue;
            var root = Path.GetFullPath(selected);
            if (!Directory.Exists(root) || IsLink(root)) continue;

            var pending = new Stack<string>();
            pending.Push(root);
            while (pending.Count > 0 && result.Count < MaxFiles && totalCharacters < MaxTotalCharacters)
            {
                var directory = pending.Pop();
                IEnumerable<string> files;
                try { files = Directory.EnumerateFiles(directory).OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase).ToArray(); }
                catch (IOException) { continue; }
                catch (UnauthorizedAccessException) { continue; }

                foreach (var file in files)
                {
                    if (result.Count >= MaxFiles || totalCharacters >= MaxTotalCharacters) break;
                    var name = Path.GetFileName(file);
                    if (IsExcludedFile(name) || !TextExtensions.Contains(Path.GetExtension(file)) || IsLink(file)) continue;
                    try
                    {
                        var info = new FileInfo(file);
                        if (info.Length > MaxFileCharacters * 4L) continue;
                        var content = WorkspaceFileEditor.ReadTextBounded(file, MaxFileCharacters * 4);
                        if (content.Contains('\0')) continue;
                        if (content.Length > MaxFileCharacters) content = content[..MaxFileCharacters] + "\n[truncated]";
                        var relative = Path.GetRelativePath(root, file);
                        var remaining = MaxTotalCharacters - totalCharacters;
                        if (content.Length > remaining) content = content[..remaining] + "\n[truncated]";
                        result.Add((relative, content));
                        totalCharacters += content.Length;
                    }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                    catch (DecoderFallbackException) { }
                }

                try
                {
                    foreach (var child in Directory.EnumerateDirectories(directory).OrderByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
                    {
                        if (!ExcludedDirectories.Contains(Path.GetFileName(child)) && !IsLink(child)) pending.Push(child);
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
        return result;
    }

    private static bool IsLink(string path)
    {
        try { return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0; }
        catch (IOException) { return true; }
        catch (UnauthorizedAccessException) { return true; }
    }

    private static bool IsExcludedFile(string name) =>
        ExcludedFiles.Contains(name) ||
        (name.StartsWith(".env", StringComparison.OrdinalIgnoreCase) &&
         !string.Equals(name, ".env.example", StringComparison.OrdinalIgnoreCase));
}
