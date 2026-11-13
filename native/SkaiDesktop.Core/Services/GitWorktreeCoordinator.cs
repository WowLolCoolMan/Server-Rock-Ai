// Bounded Git worktree lifecycle for isolated agent/project execution.
// This service owns only the Git worktree boundary; model/tool dispatch and
// process execution remain explicit caller responsibilities.

using System.Diagnostics;
using System.Text;
using System.ComponentModel;
using SkaiDesktop.Core.Infrastructure;

namespace SkaiDesktop.Core.Services;

public sealed record GitWorktreeLease(
    string RepositoryRoot,
    string WorktreePath,
    string Branch,
    string BaseRevision);

/// <summary>
/// Creates and releases clean, named Git worktrees without allowing a caller
/// to point an isolated target at the repository itself or a nested path.
/// Release is intentionally non-forced: dirty or unregistered targets remain
/// for explicit human recovery instead of being deleted by an agent.
/// </summary>
public sealed class GitWorktreeCoordinator
{
    private const int MaxIdentifierCharacters = 256;
    private const int MaxOutputCharacters = 1_048_576;

    public async Task<GitWorktreeLease> CreateAsync(
        string repositoryPath,
        string worktreePath,
        string branch,
        string baseRevision = "HEAD",
        CancellationToken cancellationToken = default)
    {
        var root = await ResolveRepositoryRootAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
        var target = ValidateTargetPath(root, worktreePath);
        ValidateIdentifier(branch, nameof(branch));
        ValidateIdentifier(baseRevision, nameof(baseRevision));
        if (File.Exists(target) || Directory.Exists(target))
            throw new IOException("The isolated worktree path already exists. Choose another target.");

        var parent = Path.GetDirectoryName(target) ?? throw new ArgumentException("Choose a worktree parent folder.", nameof(worktreePath));
        Directory.CreateDirectory(parent);
        var branchCheck = await RunGitAsync(root, ["check-ref-format", "--branch", branch], TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
        EnsureSuccess(branchCheck, "Git rejected the isolated worktree branch name.");

        try
        {
            var result = await RunGitAsync(root, ["worktree", "add", "-b", branch, "--", target, baseRevision], TimeSpan.FromMinutes(2), cancellationToken).ConfigureAwait(false);
            EnsureSuccess(result, "Git could not create the isolated worktree.");
            return new(root, target, branch, baseRevision);
        }
        catch
        {
            // Only remove a directory that this call created and only when Git
            // left an empty partial target. Never force-remove user content.
            if (Directory.Exists(target) && !Directory.EnumerateFileSystemEntries(target).Any())
                Directory.Delete(target);
            throw;
        }
    }

    public async Task ReleaseCleanAsync(
        string repositoryPath,
        string worktreePath,
        CancellationToken cancellationToken = default)
    {
        var root = await ResolveRepositoryRootAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
        var target = ValidateTargetPath(root, worktreePath);
        var registered = await RunGitAsync(root, ["worktree", "list", "--porcelain"], TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
        EnsureSuccess(registered, "Git could not inspect registered worktrees.");
        if (!IsRegisteredPath(registered.StandardOutput, target))
            throw new InvalidOperationException("The requested path is not a registered worktree owned by this repository.");

        var status = await RunGitAsync(target, ["status", "--porcelain"], TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
        EnsureSuccess(status, "Git could not inspect the isolated worktree.");
        if (!string.IsNullOrWhiteSpace(status.StandardOutput))
            throw new InvalidOperationException("The isolated worktree has uncommitted changes. Commit, export, or review them before release.");

        var result = await RunGitAsync(root, ["worktree", "remove", "--", target], TimeSpan.FromMinutes(2), cancellationToken).ConfigureAwait(false);
        EnsureSuccess(result, "Git could not release the clean isolated worktree.");
    }

    private static async Task<string> ResolveRepositoryRootAsync(string repositoryPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath)) throw new ArgumentException("Choose a repository folder.", nameof(repositoryPath));
        var path = Path.GetFullPath(repositoryPath);
        if (!Directory.Exists(path)) throw new DirectoryNotFoundException("The repository folder no longer exists.");
        var result = await RunGitAsync(path, ["rev-parse", "--show-toplevel"], TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
        EnsureSuccess(result, "The selected folder is not a Git repository.");
        var root = Path.GetFullPath(result.StandardOutput.Trim());
        if (!root.Equals(path, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new InvalidOperationException("Select the repository root folder, not a subfolder.");
        return root;
    }

    private static string ValidateTargetPath(string root, string worktreePath)
    {
        if (string.IsNullOrWhiteSpace(worktreePath)) throw new ArgumentException("Choose an isolated worktree path.", nameof(worktreePath));
        var target = Path.GetFullPath(worktreePath);
        if (target.Equals(root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) ||
            IsDescendant(root, target) || IsDescendant(target, root))
            throw new ArgumentException("An isolated worktree must be a sibling or separate external path, not the repository or a nested folder.", nameof(worktreePath));
        return target;
    }

    private static bool IsDescendant(string parent, string candidate)
    {
        var relative = Path.GetRelativePath(parent, candidate);
        return relative != "." && relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }

    private static void ValidateIdentifier(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxIdentifierCharacters || value.Any(char.IsControl))
            throw new ArgumentException("The Git identifier is empty, too long, or contains a control character.", parameterName);
    }

    private static bool IsRegisteredPath(string porcelain, string target)
    {
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        foreach (var line in porcelain.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!line.StartsWith("worktree ", StringComparison.Ordinal)) continue;
            var registered = Path.GetFullPath(line[9..].TrimEnd('\r'));
            if (registered.Equals(target, comparison)) return true;
        }
        return false;
    }

    private static async Task<GitProcessResult> RunGitAsync(string workingDirectory, IReadOnlyList<string> arguments, TimeSpan timeoutDuration, CancellationToken cancellationToken)
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
        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start()) throw new InvalidOperationException("Git could not be started.");
        }
        catch (Win32Exception exception)
        {
            throw new InvalidOperationException("Git is not installed or is not available on PATH.", exception);
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
        var buffer = new char[8192];
        var output = new StringBuilder(Math.Min(MaxOutputCharacters, 8192));
        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
            if (read == 0) break;
            if (output.Length < MaxOutputCharacters) output.Append(buffer, 0, Math.Min(read, MaxOutputCharacters - output.Length));
        }
        return output.Length == MaxOutputCharacters ? output + "…[output truncated]" : output.ToString();
    }

    private static void EnsureSuccess(GitProcessResult result, string fallback)
    {
        if (result.ExitCode == 0) return;
        var detail = result.StandardError.Trim();
        if (detail.Length > 400) detail = detail[..400] + "…";
        throw new InvalidOperationException(detail.Length == 0 ? fallback : $"{fallback} {detail}");
    }

    private sealed record GitProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
