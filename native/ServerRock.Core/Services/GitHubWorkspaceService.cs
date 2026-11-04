using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace ServerRock.Core.Services;

public sealed record GitWorkspaceFileChange(string Status, string Path, string? OriginalPath = null);
public sealed record GitWorkspaceStatus(string Branch, IReadOnlyList<GitWorkspaceFileChange> Changes, int CommitsAhead, int CommitsBehind, string Repository);

/// <summary>Runs local Git operations for GitHub repositories without putting OAuth tokens in command arguments or remotes.</summary>
public sealed class GitHubWorkspaceService
{
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

    public async Task<string> CommitAllAsync(string repositoryPath, string message, string accessToken, CancellationToken cancellationToken = default)
    {
        ValidateAccessToken(accessToken);
        message = (message ?? "").Trim();
        if (message.Length is < 1 or > 256 || message.Any(character => char.IsControl(character) && character is not '\n' and not '\r' and not '\t'))
            throw new ArgumentException("Enter a commit message between 1 and 256 characters.", nameof(message));

        var status = await GetStatusAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
        if (status.Changes.Count == 0) throw new InvalidOperationException("There are no file changes to commit.");
        var unsafePaths = FindUnsafeChangePaths(status.Changes);
        if (unsafePaths.Count > 0)
            throw new InvalidOperationException($"Refusing to stage secret-like files: {string.Join(", ", unsafePaths.Take(8))}{(unsafePaths.Count > 8 ? "…" : "")}");
        var root = await GetRepositoryRootAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
        var stage = await RunGitAsync(root, ["add", "--all"], null, TimeSpan.FromMinutes(2), cancellationToken).ConfigureAwait(false);
        EnsureSuccess(stage, "Git could not stage the workspace changes.");
        var commit = await RunGitAsync(root, ["commit", "-m", message], null, TimeSpan.FromMinutes(2), cancellationToken).ConfigureAwait(false);
        EnsureSuccess(commit, "Git could not create the commit. Check your Git author name and email in Git settings.");
        return commit.StandardOutput.Trim();
    }

    public async Task PushAsync(string repositoryPath, string accessToken, CancellationToken cancellationToken = default)
    {
        ValidateAccessToken(accessToken);
        var status = await GetStatusAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
        if (status.Changes.Count > 0) throw new InvalidOperationException("Commit or discard the listed changes before pushing.");
        if (status.CommitsAhead == 0) throw new InvalidOperationException("There are no local commits waiting to push.");
        var root = await GetRepositoryRootAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
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
        TimeSpan timeoutDuration, CancellationToken cancellationToken)
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
