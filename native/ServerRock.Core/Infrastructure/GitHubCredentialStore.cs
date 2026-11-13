// Stores the GitHub OAuth token: Windows DPAPI-encrypted file, or the Linux
// desktop keyring through the `secret-tool` CLI. Other platforms report "unavailable".
// Depends on: nothing in Core. Used by: MainPage GitHub workspace UI (MainPage.GitHub.cs).

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using ServerRock.Core.Models;

namespace ServerRock.Core.Infrastructure;

public sealed record GitHubCredentialStorageStatus(bool Available, string Method, string Message);

/// <summary>Stores GitHub OAuth material with Windows DPAPI or the Linux Secret Service.</summary>
public sealed class GitHubCredentialStore(string? windowsCredentialPath = null, string? secretToolPath = null)
{
    private const int MaxStoredCredentialBytes = 64 * 1024;
    private const string ServiceAttribute = "serverrock-ai";
    private const string AccountAttribute = "github-user-token";
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes(ProductIdentity.GitHubCredentialEntropy);
    private readonly string _windowsCredentialPath = windowsCredentialPath ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ServerRock", "AI", "github-user-token.bin");
    private readonly string? _secretToolPath = secretToolPath ?? FindExecutable("secret-tool");

    public GitHubCredentialStorageStatus GetStatus()
    {
        if (OperatingSystem.IsWindows())
            return new(true, "Windows Data Protection API", "GitHub credentials are encrypted for this Windows user.");
        if (OperatingSystem.IsLinux() && _secretToolPath is not null && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DBUS_SESSION_BUS_ADDRESS")))
            return new(true, "Linux Secret Service", "GitHub credentials will be stored in the unlocked desktop keyring.");
        if (OperatingSystem.IsLinux())
            return new(false, "", "A running Secret Service and the secret-tool utility are required to store GitHub credentials securely.");
        return new(false, "", "Secure GitHub credential storage is not available on this operating system yet.");
    }

    public async Task<string?> ReadAsync(CancellationToken cancellationToken = default)
    {
        var status = GetStatus();
        if (!status.Available) throw new PlatformNotSupportedException(status.Message);
        if (OperatingSystem.IsWindows())
        {
            if (!File.Exists(_windowsCredentialPath)) return null;
            try
            {
                if (new FileInfo(_windowsCredentialPath).Length > MaxStoredCredentialBytes)
                    throw new InvalidDataException("The saved GitHub credential is unexpectedly large. Reconnect GitHub to replace it.");
                var ciphertext = await ReadBytesBoundedAsync(_windowsCredentialPath, MaxStoredCredentialBytes, cancellationToken).ConfigureAwait(false);
                var plaintext = ProtectedData.Unprotect(ciphertext, Entropy, DataProtectionScope.CurrentUser);
                try
                {
                    var value = Encoding.UTF8.GetString(plaintext);
                    if (value.Length is < 1 or > 32_768 || value.Any(char.IsControl))
                        throw new InvalidDataException("The saved GitHub credential has an invalid format. Reconnect GitHub to replace it.");
                    return value;
                }
                finally { CryptographicOperations.ZeroMemory(plaintext); }
            }
            catch (CryptographicException error)
            {
                throw new InvalidDataException("The saved GitHub credential could not be decrypted for this Windows user. Reconnect GitHub to replace it.", error);
            }
        }

        var result = await RunSecretToolAsync(["lookup", "application", ServiceAttribute, "account", AccountAttribute], null, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode == 0) return result.StandardOutput.TrimEnd('\r', '\n');
        if (result.ExitCode == 1 && result.StandardError.Length == 0) return null;
        throw new InvalidOperationException(DescribeSecretToolFailure(result, "GitHub credential lookup failed."));
    }

    public async Task WriteAsync(string? value, CancellationToken cancellationToken = default)
    {
        var status = GetStatus();
        if (!status.Available) throw new PlatformNotSupportedException(status.Message);
        if (string.IsNullOrEmpty(value))
        {
            await DeleteAsync(cancellationToken).ConfigureAwait(false);
            return;
        }
        if (value.Length > 32_768 || value.Any(char.IsControl)) throw new ArgumentException("The GitHub credential has an invalid format.", nameof(value));

        if (OperatingSystem.IsWindows())
        {
            var plaintext = Encoding.UTF8.GetBytes(value);
            byte[] ciphertext;
            try { ciphertext = ProtectedData.Protect(plaintext, Entropy, DataProtectionScope.CurrentUser); }
            finally { CryptographicOperations.ZeroMemory(plaintext); }
            var fullPath = Path.GetFullPath(_windowsCredentialPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            var temporaryPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await File.WriteAllBytesAsync(temporaryPath, ciphertext, cancellationToken).ConfigureAwait(false);
                File.Move(temporaryPath, fullPath, overwrite: true);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(ciphertext);
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
            return;
        }

        var stored = await RunSecretToolAsync(["store", $"--label={ProductIdentity.GitHubCredentialLabel}", "application", ServiceAttribute, "account", AccountAttribute], value, cancellationToken).ConfigureAwait(false);
        if (stored.ExitCode != 0) throw new InvalidOperationException(DescribeSecretToolFailure(stored, "GitHub credential could not be saved to the desktop keyring."));
    }

    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        var status = GetStatus();
        if (!status.Available) throw new PlatformNotSupportedException(status.Message);
        if (OperatingSystem.IsWindows())
        {
            if (File.Exists(_windowsCredentialPath)) File.Delete(_windowsCredentialPath);
            return;
        }
        var result = await RunSecretToolAsync(["clear", "application", ServiceAttribute, "account", AccountAttribute], null, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode is not (0 or 1)) throw new InvalidOperationException(DescribeSecretToolFailure(result, "GitHub credential could not be removed from the desktop keyring."));
    }

    private async Task<(int ExitCode, string StandardOutput, string StandardError)> RunSecretToolAsync(
        IReadOnlyList<string> arguments, string? input, CancellationToken cancellationToken)
    {
        var executable = _secretToolPath ?? throw new PlatformNotSupportedException(GetStatus().Message);
        var startInfo = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            RedirectStandardInput = input is not null,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);
        using var process = new Process { StartInfo = startInfo };
        if (!process.Start()) throw new InvalidOperationException("The Linux desktop keyring utility could not be started.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        var standardOutput = ReadLimitedAsync(process.StandardOutput, timeout.Token);
        var standardError = ReadLimitedAsync(process.StandardError, timeout.Token);
        try
        {
            if (input is not null)
            {
                await process.StandardInput.WriteLineAsync(input.AsMemory(), timeout.Token).ConfigureAwait(false);
                await process.StandardInput.FlushAsync(timeout.Token).ConfigureAwait(false);
                process.StandardInput.Close();
            }
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            return (process.ExitCode, await standardOutput.ConfigureAwait(false), await standardError.ConfigureAwait(false));
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
            if (!cancellationToken.IsCancellationRequested)
                throw new TimeoutException("The desktop keyring did not respond. Unlock the keyring and try again.");
            throw;
        }
    }

    private static async Task<string> ReadLimitedAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        const int maxCharacters = 64 * 1024;
        var buffer = new char[4096];
        var output = new StringBuilder(Math.Min(maxCharacters, 4096));
        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
            if (read == 0) break;
            if (output.Length < maxCharacters)
                output.Append(buffer, 0, Math.Min(read, maxCharacters - output.Length));
        }
        return output.Length == maxCharacters ? output.ToString() + "…[output truncated]" : output.ToString();
    }

    private static string DescribeSecretToolFailure((int ExitCode, string StandardOutput, string StandardError) result, string fallback)
    {
        var message = result.StandardError.Trim();
        if (message.Length > 240) message = message[..240] + "…";
        return message.Length == 0 ? fallback : $"{fallback} {message}";
    }

    private static string? FindExecutable(string name)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path)) return null;
        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(directory, name);
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }

    private static async Task<byte[]> ReadBytesBoundedAsync(string path, int maxBytes, CancellationToken cancellationToken)
    {
        await using var source = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 16 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (source.Length > maxBytes) throw new InvalidDataException("The saved GitHub credential is unexpectedly large. Reconnect GitHub to replace it.");
        await using var output = new MemoryStream((int)Math.Min(source.Length, maxBytes));
        var buffer = new byte[16 * 1024];
        while (true)
        {
            var read = await source.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
            if (read == 0) break;
            if (output.Length + read > maxBytes) throw new InvalidDataException("The saved GitHub credential is unexpectedly large. Reconnect GitHub to replace it.");
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }
        if (source.Length > maxBytes) throw new InvalidDataException("The saved GitHub credential is unexpectedly large. Reconnect GitHub to replace it.");
        return output.ToArray();
    }
}
