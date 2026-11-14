// Local `git` operations for a GitHub repository: clone, status, diff preview, commit, push.
// Tokens are passed via a credential helper, never on the command line or in the remote URL.
// Depends on: GitHubApiClient, WorkspaceFileEditor (bounded file read). Used by: MainPage.GitHub.cs.

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using SkaiDesktop.Core.Infrastructure;

namespace SkaiDesktop.Core.Services;

public sealed record GitWorkspaceFileChange(string Status, string Path, string? OriginalPath = null);
public sealed record GitWorkspaceStatus(string Branch, IReadOnlyList<GitWorkspaceFileChange> Changes, int CommitsAhead, int CommitsBehind, string Repository);
public sealed record GitWorkspaceDiffPreview(string Text, int IncludedFiles, int RedactedFiles, bool IsTruncated);

/// <summary>Runs local Git operations for GitHub repositories without putting OAuth tokens in command arguments or remotes.</summary>
public sealed class GitHubWorkspaceService
{
    public const int MaxDiffPreviewCharacters = 256_000;
    private const int MaxDiffFileCharacters = 64_000;
    private const int MaxDiffFiles = 24;
    private static readonly Regex AheadBehindPattern = new("^# branch\\.ab \\+(\\d+) -(\\d+)$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly HashSet<string> ProtectedDirectories = new(StringComparer.OrdinalIgnoreCase) { ".git", ".ssh", ".aws", ".azure", ".gnupg" };
    private static readonly HashSet<string> ProtectedNames = new(StringComparer.OrdinalIgnoreCase) { "credentials.json", "secrets.json", "id_rsa", "id_ed25519" };

    public async Task<string> CloneAsync(GitHubRepository repository, string destination, string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ValidateGitHubCloneUri(repository.CloneUrl);
        ValidateAccessToken(accessToken);
        var fullDestination = Path.GetFullPath(destination);
        var parent = Path.GetDirectoryName(fullDestination) ?? throw new ArgumentException("Choose a destination folder.", nameof(destination));
        Directory.CreateDirectory(parent);
        if (File.Exists(fullDestination) || Directory.Exists(fullDestination) && Directory.EnumerateFileSystemEntries(fullDestination).Any())
            throw new IOException("The repository destination already exists and is not empty. Choose another folder.");

        var result = await RunGitAsync(parent, ["clone", "--quiet", "--", repository.CloneUrl.AbsoluteUri, fullDestination], accessToken,
            TimeSpan.FromMinutes(10), cancellationToken).ConfigureAwait(false);
        EnsureSuccess(result, "GitHub repository could not be cloned.");
        var status = await GetStatusAsync(fullDestination, cancellationToken).ConfigureAwait(false);
        if (!status.Repository.Equals(repository.FullName, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The cloned repository identity did not match the selected GitHub repository.");
        return fullDestination;
    }

    public async Task<GitWorkspaceStatus> GetStatusAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        var root = await GetRepositoryRootAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
        var remoteResult = await RunGitAsync(root, ["remote", "get-url", "origin"], null, TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
        EnsureSuccess(remoteResult, "This project does not have a GitHub origin remote.");
        if (!Uri.TryCreate(remoteResult.StandardOutput.Trim(), UriKind.Absolute, out var remoteUri))
            throw new InvalidOperationException("The origin remote is not a valid GitHub URL.");
        var repository = ParseGitHubRemote(remoteUri);

        var statusResult = await RunGitAsync(root, ["status", "--porcelain=v2", "--branch", "--untracked-files=all"], null,
            TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
        EnsureSuccess(statusResult, "Git could not read the workspace status.");
        return ParseStatus(statusResult.StandardOutput, repository);
    }

    /// <summary>Reads local Git state without requiring a GitHub origin or credentials.</summary>
    public async Task<GitWorkspaceStatus> GetLocalStatusAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        var root = await GetRepositoryRootAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
        var statusResult = await RunGitAsync(root, ["status", "--porcelain=v2", "--branch", "--untracked-files=all"], null,
            TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
        EnsureSuccess(statusResult, "Git could not read the local workspace status.");
        return ParseStatus(statusResult.StandardOutput, "local repository");
    }

    public async Task<string> CommitSelectedAsync(string repositoryPath, IEnumerable<GitWorkspaceFileChange> selectedChanges, string message,
        string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selectedChanges);
        var selected = selectedChanges.Distinct().ToArray();
        if (selected.Length == 0) throw new InvalidOperationException("Select at least one safe file to commit.");
        var status = await GetStatusAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
        var currentByIdentity = status.Changes.ToDictionary(ChangeIdentity, StringComparer.Ordinal);
        var currentSelection = new List<GitWorkspaceFileChange>(selected.Length);
        foreach (var requested in selected)
        {
            var identity = ChangeIdentity(requested);
            if (!currentByIdentity.TryGetValue(identity, out var current))
                throw new InvalidOperationException($"The workspace changed since review. Reopen the GitHub review before committing {DisplayPath(requested)}.");
            currentSelection.Add(current);
        }
        var unsafePaths = FindUnsafeChangePaths(currentSelection);
        if (unsafePaths.Count > 0)
            throw new InvalidOperationException($"Refusing to stage secret-like files: {string.Join(", ", unsafePaths.Take(8))}{(unsafePaths.Count > 8 ? "…" : "")}");
        return await CommitSelectedCoreAsync(repositoryPath, currentSelection, message, accessToken, cancellationToken).ConfigureAwait(false);
    }

    public async Task<GitWorkspaceDiffPreview> GetDiffPreviewAsync(string repositoryPath, IEnumerable<GitWorkspaceFileChange> selectedChanges,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selectedChanges);
        var selected = selectedChanges.Distinct().ToArray();
        if (selected.Length == 0) return new("No files selected for diff preview.", 0, 0, false);
        if (selected.Length > MaxDiffFiles) throw new InvalidOperationException($"Select at most {MaxDiffFiles} files for a bounded diff preview.");
        var status = await GetLocalStatusAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
        var currentByIdentity = status.Changes.ToDictionary(ChangeIdentity, StringComparer.Ordinal);
        var builder = new StringBuilder();
        var included = 0;
        var redacted = 0;
        var truncated = false;
        foreach (var requested in selected)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!currentByIdentity.TryGetValue(ChangeIdentity(requested), out var current))
                throw new InvalidOperationException($"The workspace changed since review. Reopen the GitHub review before previewing {DisplayPath(requested)}.");
            if (FindUnsafeChangePaths([current]).Count > 0)
            {
                redacted++;
                AppendDiff(builder, $"# [protected path redacted; review locally before staging]\n# status {current.Status}\n", ref truncated);
                continue;
            }

            var root = await GetRepositoryRootAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
            var paths = new[] { current.Path, current.OriginalPath }
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.Ordinal)
                .Cast<string>()
                .ToArray();
            var staged = await RunGitAsync(root, ["diff", "--cached", "--no-ext-diff", "--no-color", "--find-renames", "--unified=3", "--", .. paths], null,
                TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
            EnsureSuccess(staged, "Git could not read the staged diff.");
            var unstaged = await RunGitAsync(root, ["diff", "--no-ext-diff", "--no-color", "--find-renames", "--unified=3", "--", .. paths], null,
                TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
            EnsureSuccess(unstaged, "Git could not read the workspace diff.");
            var diff = (staged.StandardOutput + unstaged.StandardOutput).Trim();
            if (diff.Length == 0 && current.Status == "??") diff = BuildUntrackedDiff(root, current.Path);
            if (diff.Length == 0) diff = $"# {current.Status} {DisplayPath(current)}\n# No text diff is available (the file may be binary or unchanged in the selected index).\n";
            included++;
            AppendDiff(builder, diff + "\n", ref truncated);
        }
        if (builder.Length == 0) builder.Append("No diff preview is available for the selected files.\n");
        return new(builder.ToString(), included, redacted, truncated);
    }

    /// <summary>Commits explicitly selected local changes without contacting GitHub.</summary>
    public async Task<string> CommitSelectedLocallyAsync(string repositoryPath, IEnumerable<GitWorkspaceFileChange> selectedChanges, string message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selectedChanges);
        var selected = selectedChanges.Distinct().ToArray();
        if (selected.Length == 0) throw new InvalidOperationException("Select at least one safe file to commit.");
        var status = await GetLocalStatusAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
        var currentByIdentity = status.Changes.ToDictionary(ChangeIdentity, StringComparer.Ordinal);
        var currentSelection = selected.Select(requested => currentByIdentity.TryGetValue(ChangeIdentity(requested), out var current)
            ? current
            : throw new InvalidOperationException($"The workspace changed since review. Reopen the Git review before committing {DisplayPath(requested)}.")).ToArray();
        return await CommitSelectedCoreAsync(repositoryPath, currentSelection, message, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Pushes clean local commits through the user's configured Git credential helper.</summary>
    public async Task<string> PushLocalAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        var status = await GetLocalStatusAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
        if (status.Changes.Count > 0) throw new InvalidOperationException("Commit or discard the listed changes before pushing.");
        if (GitHubWorkspaceService.HasConflicts(status.Changes)) throw new InvalidOperationException("Resolve conflicts before pushing.");
        if (status.CommitsAhead == 0) throw new InvalidOperationException("There are no local commits waiting to push.");
        if (status.CommitsBehind > 0) throw new InvalidOperationException("The remote branch has newer commits. Fetch and review them before pushing.");
        var root = await GetRepositoryRootAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
        var remote = await RunGitAsync(root, ["remote", "get-url", "origin"], null, TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
        EnsureSuccess(remote, "This project has no origin remote configured. Add one before pushing.");
        var push = await RunGitAsync(root, ["push", "--set-upstream", "origin", "HEAD"], null, TimeSpan.FromMinutes(5), cancellationToken).ConfigureAwait(false);
        EnsureSuccess(push, "Git push failed. Your local commit is preserved; review the Git credential helper and remote state.");
        return (push.StandardOutput + push.StandardError).Trim();
    }

    private async Task<string> CommitSelectedCoreAsync(string repositoryPath, IReadOnlyCollection<GitWorkspaceFileChange> selectedChanges, string message,
        string? accessToken, CancellationToken cancellationToken)
    {
        if (accessToken is not null) ValidateAccessToken(accessToken);
        message = (message ?? "").Trim();
        if (message.Length is < 1 or > 256 || message.Any(character => char.IsControl(character) && character is not '\n' and not '\r' and not '\t'))
            throw new ArgumentException("Enter a commit message between 1 and 256 characters.", nameof(message));

        if (selectedChanges.Count == 0) throw new InvalidOperationException("There are no file changes to commit.");
        var root = await GetRepositoryRootAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
        var paths = selectedChanges
            .SelectMany(change => change.OriginalPath is not null && !change.Status.StartsWith("R", StringComparison.Ordinal)
                ? new[] { change.Path, change.OriginalPath }
                : new[] { change.Path })
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var temporaryIndex = Path.Combine(Path.GetTempPath(), $"server-rock-index-{Guid.NewGuid():N}");
        try
        {
            var resetIndex = await RunGitAsync(root, ["read-tree", "HEAD"], null, TimeSpan.FromMinutes(2), cancellationToken, temporaryIndex).ConfigureAwait(false);
            EnsureSuccess(resetIndex, "Git could not prepare an isolated commit index.");
            foreach (var path in paths)
            {
                var target = Path.GetFullPath(Path.Combine(root, path));
                var arguments = File.Exists(target) || Directory.Exists(target)
                    ? new[] { "add", "--", path }
                    : new[] { "add", "-u", "--", path };
                var stage = await RunGitAsync(root, arguments, null, TimeSpan.FromMinutes(2), cancellationToken, temporaryIndex).ConfigureAwait(false);
                EnsureSuccess(stage, "Git could not stage the workspace changes.");
            }
            var commit = await RunGitAsync(root, ["commit", "-m", message], null, TimeSpan.FromMinutes(2), cancellationToken, temporaryIndex).ConfigureAwait(false);
            EnsureSuccess(commit, "Git could not create the commit. Check your Git author name and email in Git settings.");
            return commit.StandardOutput.Trim();
        }
        finally
        {
            try { if (File.Exists(temporaryIndex)) File.Delete(temporaryIndex); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static string ChangeIdentity(GitWorkspaceFileChange change) =>
        $"{change.Status}\u001f{change.Path}\u001f{change.OriginalPath}";

    private static string DisplayPath(GitWorkspaceFileChange change) =>
        change.OriginalPath is null ? change.Path : $"{change.Path} (from {change.OriginalPath})";

    private static void AppendDiff(StringBuilder builder, string text, ref bool truncated)
    {
        if (builder.Length >= MaxDiffPreviewCharacters)
        {
            truncated = true;
            return;
        }
        var remaining = MaxDiffPreviewCharacters - builder.Length;
        if (text.Length <= remaining)
        {
            builder.Append(text);
            return;
        }
        builder.Append(text.AsSpan(0, Math.Max(0, remaining - 32)));
        builder.Append("\n[… diff preview truncated …]\n");
        truncated = true;
    }

    private static string BuildUntrackedDiff(string root, string relativePath)
    {
        var target = Path.GetFullPath(Path.Combine(root, relativePath));
        var relative = Path.GetRelativePath(root, target);
        if (Path.IsPathRooted(relative) || relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            return "# [untracked path redacted; it leaves the repository root]\n";
        if (!File.Exists(target)) return $"# ?? {relativePath}\n# The untracked file disappeared before preview.\n";
        string text;
        try { text = WorkspaceFileEditor.ReadTextBounded(target, MaxDiffFileCharacters * 4); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return $"# ?? {relativePath}\n# Binary or unreadable untracked file; content omitted.\n";
        }
        if (text.Contains('\0')) return $"# ?? {relativePath}\n# Binary untracked file; content omitted.\n";
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
        var builder = new StringBuilder();
        builder.Append("diff --git a/").Append(relativePath).Append(" b/").Append(relativePath)
            .Append("\nnew file mode 100644\n--- /dev/null\n+++ b/").Append(relativePath)
            .Append("\n@@ -0,0 +1,").Append(Math.Max(1, lines.Length)).Append(" @@\n");
        foreach (var line in lines)
        {
            if (builder.Length + line.Length + 2 > MaxDiffFileCharacters) { builder.Append("+[… file diff truncated …]\n"); break; }
            builder.Append('+').Append(line).Append('\n');
        }
        return builder.ToString();
    }

    public async Task PushAsync(string repositoryPath, string accessToken, CancellationToken cancellationToken = default)
    {
        ValidateAccessToken(accessToken);
        var status = await GetStatusAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
        if (status.Changes.Count > 0) throw new InvalidOperationException("Commit or discard the listed changes before pushing.");
        if (status.CommitsAhead == 0) throw new InvalidOperationException("There are no local commits waiting to push.");
        var root = await GetRepositoryRootAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
        var fetch = await RunGitAsync(root, ["fetch", "--prune", "--no-tags", "origin"], accessToken, TimeSpan.FromMinutes(2), cancellationToken).ConfigureAwait(false);
        EnsureSuccess(fetch, "GitHub remote state could not be refreshed. Your local commits were not pushed; check connectivity and GitHub access, then try again.");
        status = await GetStatusAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
        if (status.Changes.Count > 0) throw new InvalidOperationException("The workspace changed while refreshing GitHub state. Review the changes before pushing.");
        if (status.CommitsAhead == 0) throw new InvalidOperationException("There are no local commits waiting to push after refreshing GitHub state.");
        string[] arguments = status.CommitsBehind > 0
            ? throw new InvalidOperationException("The GitHub branch has newer commits. Fetch and review them before pushing to avoid overwriting remote work.")
            : ["push", "--set-upstream", "origin", "HEAD"];
        var push = await RunGitAsync(root, arguments, accessToken, TimeSpan.FromMinutes(5), cancellationToken).ConfigureAwait(false);
        EnsureSuccess(push, "GitHub push failed. Your local commit is preserved and can be pushed again after resolving the GitHub error.");
    }

    public static GitWorkspaceStatus ParseStatus(string porcelainV2, string repository)
    {
        var branch = "(detached HEAD)";
        var ahead = 0;
        var behind = 0;
        var changes = new List<GitWorkspaceFileChange>();
        foreach (var line in porcelainV2.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.TrimEnd('\r');
            if (trimmed.StartsWith("# branch.head ", StringComparison.Ordinal)) branch = trimmed[14..];
            else if (AheadBehindPattern.Match(trimmed) is { Success: true } match)
            {
                _ = int.TryParse(match.Groups[1].Value, out ahead);
                _ = int.TryParse(match.Groups[2].Value, out behind);
            }
            else if (trimmed.StartsWith("? ", StringComparison.Ordinal))
                changes.Add(new("??", trimmed[2..]));
            else if (trimmed.StartsWith("1 ", StringComparison.Ordinal))
            {
                var fields = trimmed.Split(' ', 9, StringSplitOptions.None);
                if (fields.Length == 9) changes.Add(new(fields[1], fields[8]));
            }
            else if (trimmed.StartsWith("2 ", StringComparison.Ordinal))
            {
                var fields = trimmed.Split(' ', 10, StringSplitOptions.None);
                if (fields.Length == 10)
                {
                    var paths = fields[9].Split('\t', 2, StringSplitOptions.None);
                    changes.Add(new(fields[1], paths[0], paths.Length == 2 ? paths[1] : null));
                }
            }
            else if (trimmed.StartsWith("u ", StringComparison.Ordinal))
            {
                var fields = trimmed.Split(' ', 11, StringSplitOptions.None);
                if (fields.Length == 11) changes.Add(new("UU", fields[10]));
            }
        }
        return new(branch, changes, ahead, behind, repository);
    }

    public static IReadOnlyList<string> FindUnsafeChangePaths(IEnumerable<GitWorkspaceFileChange> changes) =>
        changes
            .SelectMany(change => new[] { change.Path, change.OriginalPath })
            .Where(path => path is not null)
            .Select(path => path!.Replace('\\', '/'))
            .Where(IsUnsafeChangePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public static bool IsConflictStatus(string? status) => status is not null &&
        (status.Contains('U', StringComparison.OrdinalIgnoreCase) || status is "AA" or "DD");

    public static bool HasConflicts(IEnumerable<GitWorkspaceFileChange> changes) =>
        changes.Any(change => IsConflictStatus(change.Status));

    private static bool IsUnsafeChangePath(string path)
    {
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;
        if (parts[..^1].Any(ProtectedDirectories.Contains)) return true;
        var name = parts[^1];
        if (ProtectedNames.Contains(name)) return true;
        if (name.StartsWith(".env", StringComparison.OrdinalIgnoreCase) &&
            !name.Equals(".env.example", StringComparison.OrdinalIgnoreCase)) return true;
        return Path.GetExtension(name) is ".pem" or ".key" or ".pfx" or ".p12";
    }

    private static async Task<string> GetRepositoryRootAsync(string repositoryPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath)) throw new ArgumentException("Choose a local repository folder.", nameof(repositoryPath));
        var path = Path.GetFullPath(repositoryPath);
        if (!Directory.Exists(path)) throw new DirectoryNotFoundException("The local repository folder no longer exists.");
        var result = await RunGitAsync(path, ["rev-parse", "--show-toplevel"], null, TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
        EnsureSuccess(result, "This folder is not a Git repository.");
        var root = Path.GetFullPath(result.StandardOutput.Trim());
        if (!root.Equals(path, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new InvalidOperationException("Select the repository root folder, not a subfolder.");
        return root;
    }

    private static string ParseGitHubRemote(Uri uri)
    {
        if (uri.Scheme != Uri.UriSchemeHttps || !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) || uri.Port != 443 ||
            uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0)
            throw new InvalidOperationException("The origin remote must be a plain HTTPS GitHub repository URL.");
        try { return GitHubApiClient.ParseRepositoryReference(uri.AbsolutePath.Trim('/')); }
        catch (ArgumentException exception) { throw new InvalidOperationException("The origin remote must point to a GitHub owner/repository.", exception); }
    }

    private static void ValidateGitHubCloneUri(Uri uri)
    {
        if (uri.Scheme != Uri.UriSchemeHttps || !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) || uri.Port != 443 ||
            uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0)
            throw new ArgumentException("Only clean HTTPS GitHub clone URLs are allowed.", nameof(uri));
        _ = GitHubApiClient.ParseRepositoryReference(uri.AbsolutePath.Trim('/'));
    }

    private static async Task<GitProcessResult> RunGitAsync(string workingDirectory, IReadOnlyList<string> arguments, string? accessToken,
        TimeSpan timeoutDuration, CancellationToken cancellationToken, string? indexFile = null)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);
        startInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";
        startInfo.Environment["GCM_INTERACTIVE"] = "Never";
        if (!string.IsNullOrWhiteSpace(indexFile)) startInfo.Environment["GIT_INDEX_FILE"] = indexFile;
        if (!string.IsNullOrEmpty(accessToken))
        {
            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes("x-access-token:" + accessToken));
            startInfo.Environment["GIT_CONFIG_COUNT"] = "2";
            startInfo.Environment["GIT_CONFIG_KEY_0"] = "credential.helper";
            startInfo.Environment["GIT_CONFIG_VALUE_0"] = "";
            startInfo.Environment["GIT_CONFIG_KEY_1"] = "http.https://github.com/.extraheader";
            startInfo.Environment["GIT_CONFIG_VALUE_1"] = "AUTHORIZATION: basic " + basic;
        }

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start()) throw new InvalidOperationException("Git could not be started. Install Git and make sure it is available on PATH.");
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            throw new InvalidOperationException("Git is not installed or is not available on PATH. Install Git to clone, commit, and push repositories.", exception);
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(timeoutDuration);
        var stdout = ReadLimitedAsync(process.StandardOutput, timeout.Token);
        var stderr = ReadLimitedAsync(process.StandardError, timeout.Token);
        try
        {
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            return new(process.ExitCode, await stdout.ConfigureAwait(false), await stderr.ConfigureAwait(false));
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
            if (!cancellationToken.IsCancellationRequested) throw new TimeoutException("Git did not finish before the operation timed out.");
            throw;
        }
    }

    private static async Task<string> ReadLimitedAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        const int maxCharacters = 1_048_576;
        var buffer = new char[8192];
        var output = new StringBuilder(Math.Min(maxCharacters, 8192));
        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
            if (read == 0) break;
            if (output.Length < maxCharacters)
                output.Append(buffer, 0, Math.Min(read, maxCharacters - output.Length));
        }
        return output.Length == maxCharacters ? output + "…[output truncated]" : output.ToString();
    }

    private static void EnsureSuccess(GitProcessResult result, string fallback)
    {
        if (result.ExitCode == 0) return;
        var detail = result.StandardError.Trim();
        if (detail.Length > 400) detail = detail[..400] + "…";
        if (detail.Contains("AUTHORIZATION: basic", StringComparison.OrdinalIgnoreCase)) detail = "GitHub rejected the saved authorization. Reconnect GitHub and try again.";
        throw new InvalidOperationException(detail.Length == 0 ? fallback : $"{fallback} {detail}");
    }

    private static void ValidateAccessToken(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken) || accessToken.Length > 16_384 || accessToken.Any(char.IsControl))
            throw new ArgumentException("A valid GitHub sign-in is required.", nameof(accessToken));
    }

    private sealed record GitProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
