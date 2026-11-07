// Applies one reviewed text-file change inside a selected folder, with path containment,
// a concurrency hash, and an undo receipt.
// Used by: MainPage Coding Agent (proposals, file editor), WorkspaceUndoStore, WorkspaceFileContext, GitHubWorkspaceService.

using System.Security.Cryptography;
using System.Text;

namespace ServerRock.Core.Infrastructure;

/// <summary>Applies a single reviewed text-file change inside a user-selected workspace folder.</summary>
public sealed class WorkspaceFileEditor
{
    private static readonly HashSet<string> BlockedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".env", ".env.local", ".env.production", "secrets.json", "credentials.json", "id_rsa", "id_ed25519"
    };
    private static readonly HashSet<string> BlockedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".svn", ".hg", "node_modules", "vendor", "bin", "obj", "dist", "build", ".venv", "venv", "target",
        ".ssh", ".aws", ".azure", ".gnupg"
    };
    private static readonly HashSet<string> WritableExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".csproj", ".sln", ".xaml", ".xml", ".json", ".md", ".txt", ".yml", ".yaml", ".toml", ".ini",
        ".props", ".targets", ".razor", ".html", ".css", ".js", ".jsx", ".ts", ".tsx", ".py", ".sh", ".ps1",
        ".sql", ".go", ".rs", ".java", ".kt", ".swift", ".c", ".h", ".cpp", ".hpp", ".vue", ".svelte"
    };

    public const int MaxCharacters = 100_000;
    public const int MaxFileBytes = MaxCharacters * 4;

    public string ReadText(string selectedRoot, string relativePath)
    {
        var target = Resolve(selectedRoot, relativePath);
        EnsureNoLinksInPath(selectedRoot, target);
        if (!File.Exists(target)) throw new FileNotFoundException("The selected project file no longer exists.");
        try { return ReadTextBounded(target, MaxFileBytes); }
        catch (InvalidDataException error) { throw new InvalidOperationException("The existing file is too large or changed while being read.", error); }
    }

    public string? GetCurrentHash(string selectedRoot, string relativePath)
    {
        var target = Resolve(selectedRoot, relativePath);
        EnsureNoLinksInPath(selectedRoot, target);
        if (!File.Exists(target)) return null;
        if (IsLink(target)) throw new InvalidOperationException("The target is a symbolic link; it cannot be edited safely.");
        try { return Hash(ReadBytesBounded(target, MaxFileBytes)); }
        catch (InvalidDataException error) { throw new InvalidOperationException("The existing file is too large or changed while being read.", error); }
    }

    public WorkspaceEditReceipt Apply(string selectedRoot, string relativePath, string content, string? expectedHash)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (content.Length > MaxCharacters) throw new InvalidOperationException($"The proposed file exceeds the {MaxCharacters:N0} character limit.");
        if (content.Contains('\0')) throw new InvalidOperationException("Binary content cannot be written by the Coding Agent.");
        var target = Resolve(selectedRoot, relativePath);
        var currentHash = GetCurrentHash(selectedRoot, relativePath);
        if (!string.Equals(currentHash, expectedHash, StringComparison.Ordinal))
            throw new IOException("The file changed after review. Reopen the proposal and review the current version before applying it.");

        var previous = currentHash is null ? null : ReadBytesBounded(target, MaxFileBytes);
        EnsureNoLinksInPath(selectedRoot, target);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        var temp = target + ".serverrock-" + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temp, content, new UTF8Encoding(false));
            File.Move(temp, target, overwrite: true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
        return new WorkspaceEditReceipt(Path.GetFullPath(target), previous, Hash(ReadBytesBounded(target, MaxFileBytes)));
    }

    public void Undo(WorkspaceEditReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        if (!File.Exists(receipt.TargetPath) || IsLink(receipt.TargetPath))
            throw new IOException("The file is no longer in the state created by the approved change.");
        if (!string.Equals(Hash(ReadBytesBounded(receipt.TargetPath, MaxFileBytes)), receipt.AppliedHash, StringComparison.Ordinal))
            throw new IOException("The file has changed since the approved edit. Undo was stopped to preserve newer work.");
        if (receipt.PreviousContent is null)
        {
            File.Delete(receipt.TargetPath);
            return;
        }
        if (receipt.PreviousContent.Length > MaxFileBytes)
            throw new InvalidDataException("The undo record exceeds the workspace file size limit.");
        var temp = receipt.TargetPath + ".serverrock-" + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllBytes(temp, receipt.PreviousContent);
            File.Move(temp, receipt.TargetPath, overwrite: true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    public void Undo(string selectedRoot, string relativePath, WorkspaceEditReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        var target = Resolve(selectedRoot, relativePath);
        if (!string.Equals(target, Path.GetFullPath(receipt.TargetPath), OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new InvalidOperationException("The undo record does not match this project file.");
        if (GetCurrentHash(selectedRoot, relativePath) is null)
            throw new IOException("The file no longer exists in the selected project.");
        Undo(receipt);
    }

    private static string Resolve(string selectedRoot, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(selectedRoot) || string.IsNullOrWhiteSpace(relativePath))
            throw new InvalidOperationException("A selected project folder and relative file path are required.");
        var root = Path.GetFullPath(selectedRoot);
        if (!Directory.Exists(root) || IsLink(root)) throw new DirectoryNotFoundException("The selected project folder is unavailable or linked.");
        if (Path.IsPathRooted(relativePath) || relativePath.Contains(':'))
            throw new InvalidOperationException("Only relative workspace paths are allowed.");
        var parts = relativePath.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || parts.Any(part => part is "." or ".." || part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
            throw new InvalidOperationException("The proposed path is not a valid workspace-relative path.");
        if (IsBlockedName(parts[^1]) || parts[..^1].Any(BlockedDirectories.Contains))
            throw new InvalidOperationException("That path is protected from agent edits.");
        if (!WritableExtensions.Contains(Path.GetExtension(parts[^1])))
            throw new InvalidOperationException("The Coding Agent can only create or replace supported text and source files.");
        var target = Path.GetFullPath(Path.Combine(root, Path.Combine(parts)));
        var relative = Path.GetRelativePath(root, target);
        if (Path.IsPathRooted(relative) || relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("The proposed path leaves the selected workspace folder.");
        return target;
    }

    private static void EnsureNoLinksInPath(string root, string target)
    {
        var current = Path.GetFullPath(root);
        var relative = Path.GetRelativePath(current, target);
        var parts = relative.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            current = Path.Combine(current, part);
            if ((Directory.Exists(current) || File.Exists(current)) && IsLink(current))
                throw new InvalidOperationException("The target path contains a symbolic link; it cannot be edited safely.");
        }
    }

    private static bool IsBlockedName(string name) =>
        BlockedNames.Contains(name) ||
        (name.StartsWith(".env", StringComparison.OrdinalIgnoreCase) &&
         !string.Equals(name, ".env.example", StringComparison.OrdinalIgnoreCase));

    private static bool IsLink(string path)
    {
        try { return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0; }
        catch (IOException) { return true; }
        catch (UnauthorizedAccessException) { return true; }
    }

    private static string Hash(byte[] content) => Convert.ToHexString(SHA256.HashData(content));

    public static string ReadTextBounded(string path, int maxBytes)
    {
        var bytes = ReadBytesBounded(path, maxBytes);
        try
        {
            using var stream = new MemoryStream(bytes, writable: false);
            using var reader = new StreamReader(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true), detectEncodingFromByteOrderMarks: true);
            return reader.ReadToEnd();
        }
        catch (DecoderFallbackException error)
        {
            throw new InvalidDataException("The workspace file is not valid UTF-8 text.", error);
        }
    }

    public static byte[] ReadBytesBounded(string path, int maxBytes)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (maxBytes < 1) throw new ArgumentOutOfRangeException(nameof(maxBytes));
        using var source = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.SequentialScan);
        if (source.Length > maxBytes) throw new InvalidDataException($"The workspace file exceeds the {maxBytes:N0}-byte limit.");
        using var buffer = new MemoryStream((int)Math.Min(source.Length, maxBytes));
        var chunk = new byte[64 * 1024];
        while (true)
        {
            var read = source.Read(chunk, 0, chunk.Length);
            if (read == 0) break;
            if (buffer.Length + read > maxBytes) throw new InvalidDataException($"The workspace file exceeds the {maxBytes:N0}-byte limit.");
            buffer.Write(chunk, 0, read);
        }
        if (source.Length > maxBytes) throw new InvalidDataException($"The workspace file exceeds the {maxBytes:N0}-byte limit.");
        return buffer.ToArray();
    }
}

public sealed record WorkspaceEditReceipt(string TargetPath, byte[]? PreviousContent, string AppliedHash);
