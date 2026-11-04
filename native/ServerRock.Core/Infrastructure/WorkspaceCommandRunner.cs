using System.Diagnostics;
using System.Text;

namespace ServerRock.Core.Infrastructure;

/// <summary>Runs one explicitly approved command with a selected project folder as its working directory.</summary>
public sealed class WorkspaceCommandRunner
{
    public const int MaxCommandCharacters = 4_000;
    public const int MaxOutputCharacters = 30_000;
    public static readonly TimeSpan MaxTimeout = TimeSpan.FromMinutes(2);

    public async Task<WorkspaceCommandResult> RunAsync(
        string selectedRoot,
        string relativeWorkingDirectory,
        string command,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command) || command.Length > MaxCommandCharacters || command.Contains('\0'))
            throw new ArgumentException($"Enter a command of 1 to {MaxCommandCharacters:N0} characters.", nameof(command));
        if (timeout < TimeSpan.FromSeconds(1) || timeout > MaxTimeout)
            throw new ArgumentOutOfRangeException(nameof(timeout), $"Command timeout must be between 1 second and {MaxTimeout.TotalMinutes:N0} minutes.");

        var workingDirectory = ResolveWorkingDirectory(selectedRoot, relativeWorkingDirectory);
        var isWindows = OperatingSystem.IsWindows();
        var startInfo = new ProcessStartInfo
        {
            FileName = isWindows ? "powershell.exe" : "/bin/sh",
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        foreach (var name in startInfo.Environment.Keys.Where(IsSensitiveEnvironmentName).ToArray())
            startInfo.Environment.Remove(name);
        if (isWindows)
        {
            startInfo.ArgumentList.Add("-NoLogo");
            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-NonInteractive");
            startInfo.ArgumentList.Add("-Command");
            startInfo.ArgumentList.Add(command);
        }
        else
        {
            startInfo.ArgumentList.Add("-c");
            startInfo.ArgumentList.Add(command);
        }

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        if (!process.Start()) throw new InvalidOperationException("The command process could not be started.");
        process.StandardInput.Close();
        var output = new StringBuilder();
        var outputGate = new object();
        var truncated = 0;
        var standardOutput = DrainAsync(process.StandardOutput, output, outputGate, () => Interlocked.Exchange(ref truncated, 1));
        var standardError = DrainAsync(process.StandardError, output, outputGate, () => Interlocked.Exchange(ref truncated, 1));
        var timedOut = false;
        using var timeoutSource = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
        try
        {
            await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            timedOut = true;
            TryKill(process);
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        await Task.WhenAll(standardOutput, standardError).ConfigureAwait(false);
        return new WorkspaceCommandResult(process.ExitCode, output.ToString(), timedOut, truncated != 0, workingDirectory);
    }

    public static string ResolveWorkingDirectory(string selectedRoot, string relativeWorkingDirectory)
    {
        if (string.IsNullOrWhiteSpace(selectedRoot)) throw new ArgumentException("Select a project folder first.", nameof(selectedRoot));
        var root = Path.GetFullPath(selectedRoot);
        if (!Directory.Exists(root) || IsLink(root)) throw new DirectoryNotFoundException("The selected project folder is unavailable or linked.");
        if (string.IsNullOrWhiteSpace(relativeWorkingDirectory) || relativeWorkingDirectory == ".") return root;
        if (Path.IsPathRooted(relativeWorkingDirectory) || relativeWorkingDirectory.Contains(':'))
            throw new ArgumentException("Choose a working directory inside the selected project.", nameof(relativeWorkingDirectory));
        var normalized = relativeWorkingDirectory.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        var target = Path.GetFullPath(Path.Combine(root, normalized));
        var relative = Path.GetRelativePath(root, target);
        if (relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) || Path.IsPathRooted(relative))
            throw new ArgumentException("The working directory must stay inside the selected project.", nameof(relativeWorkingDirectory));
        if (!Directory.Exists(target)) throw new DirectoryNotFoundException("The selected project subfolder does not exist.");
        var current = root;
        foreach (var segment in relative.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if (IsLink(current)) throw new ArgumentException("The selected working directory contains a symbolic link.", nameof(relativeWorkingDirectory));
        }
        return target;
    }

    private static async Task DrainAsync(TextReader reader, StringBuilder output, object gate, Action markTruncated)
    {
        var buffer = new char[2048];
        while (true)
        {
            var count = await reader.ReadAsync(buffer).ConfigureAwait(false);
            if (count == 0) return;
            lock (gate)
            {
                var remaining = MaxOutputCharacters - output.Length;
                if (remaining > 0) output.Append(buffer, 0, Math.Min(remaining, count));
                if (count > remaining) markTruncated();
            }
        }
    }

    private static bool IsLink(string path)
    {
        try { return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0; }
        catch (IOException) { return true; }
        catch (UnauthorizedAccessException) { return true; }
    }

    private static void TryKill(Process process)
    {
        try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
        catch (InvalidOperationException) { }
        catch (System.ComponentModel.Win32Exception) { }
    }

    private static bool IsSensitiveEnvironmentName(string name) =>
        name.Contains("API_KEY", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("TOKEN", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("SECRET", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("PASSWORD", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("CREDENTIAL", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("PRIVATE_KEY", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("AUTHORIZATION", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("COOKIE", StringComparison.OrdinalIgnoreCase);
}

public sealed record WorkspaceCommandResult(int ExitCode, string Output, bool TimedOut, bool OutputTruncated, string WorkingDirectory);
