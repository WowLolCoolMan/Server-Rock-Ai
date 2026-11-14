using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using SkaiDesktop.Core.Models;

namespace SkaiDesktop.Core.Infrastructure;

public sealed record ProviderCredentialStorageStatus(bool Available, string Method, string Message);

/// <summary>Stores provider keys outside settings.json and browser storage.</summary>
public sealed class ProviderCredentialStore(string? windowsCredentialDirectory = null, string? secretToolPath = null)
{
    private const int MaxStoredCredentialBytes = 64 * 1024;
    private const int MaxCredentialCharacters = 32_768;
    private const string ServiceAttribute = "serverrock-ai-provider";
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes(ProductIdentity.ProviderCredentialEntropy);
    private readonly string _windowsCredentialDirectory = windowsCredentialDirectory ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ServerRock", "AI", "provider-credentials");
    private readonly string? _secretToolPath = secretToolPath ?? FindExecutable("secret-tool");

    public ProviderCredentialStorageStatus GetStatus()
    {
        if (OperatingSystem.IsWindows())
            return new(true, "Windows Data Protection API", "Provider keys are encrypted for this Windows user.");
        if (OperatingSystem.IsLinux() && _secretToolPath is not null && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DBUS_SESSION_BUS_ADDRESS")))
            return new(true, "Linux Secret Service", "Provider keys are stored in the unlocked desktop keyring.");
        if (OperatingSystem.IsLinux())
            return new(false, "", "A running Secret Service and the secret-tool utility are required for secure provider keys.");
        return new(false, "", "Secure provider-key storage is not available on this operating system yet.");
    }

    public async Task<string?> ReadAsync(string providerId, CancellationToken cancellationToken = default)
    {
        var id = NormalizeProviderId(providerId);
        var status = GetStatus();
        if (!status.Available) throw new PlatformNotSupportedException(status.Message);
        if (OperatingSystem.IsWindows())
        {
            var path = CredentialPath(id);
            if (!File.Exists(path)) return null;
            try
            {
                if (new FileInfo(path).Length > MaxStoredCredentialBytes)
                    throw new InvalidDataException("The saved provider key is unexpectedly large. Save it again to replace it.");
                var ciphertext = await ReadBytesBoundedAsync(path, MaxStoredCredentialBytes, cancellationToken).ConfigureAwait(false);
                var plaintext = ProtectedData.Unprotect(ciphertext, Entropy, DataProtectionScope.CurrentUser);
                try { return ValidateValue(Encoding.UTF8.GetString(plaintext)); }
                finally { CryptographicOperations.ZeroMemory(plaintext); }
            }
            catch (CryptographicException error)
            {
                throw new InvalidDataException("The saved provider key could not be decrypted for this Windows user. Save it again to replace it.", error);
            }
        }

        var result = await RunSecretToolAsync(["lookup", "application", ServiceAttribute, "account", id], null, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode == 0) return ValidateValue(result.StandardOutput.TrimEnd('\r', '\n'));
        if (result.ExitCode == 1 && result.StandardError.Length == 0) return null;
        throw new InvalidOperationException(DescribeSecretToolFailure(result, "Provider key lookup failed."));
    }

    public async Task<bool> ExistsAsync(string providerId, CancellationToken cancellationToken = default) =>
        await ReadAsync(providerId, cancellationToken).ConfigureAwait(false) is not null;

    public async Task WriteAsync(string providerId, string value, CancellationToken cancellationToken = default)
    {
        var id = NormalizeProviderId(providerId);
        var validated = ValidateValue(value);
        var status = GetStatus();
        if (!status.Available) throw new PlatformNotSupportedException(status.Message);
        if (OperatingSystem.IsWindows())
        {
            var plaintext = Encoding.UTF8.GetBytes(validated);
            byte[] ciphertext;
            try { ciphertext = ProtectedData.Protect(plaintext, Entropy, DataProtectionScope.CurrentUser); }
            finally { CryptographicOperations.ZeroMemory(plaintext); }
            var fullPath = CredentialPath(id);
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

        var stored = await RunSecretToolAsync(["store", $"--label={ProductIdentity.ProviderCredentialLabel}", "application", ServiceAttribute, "account", id], validated, cancellationToken).ConfigureAwait(false);
        if (stored.ExitCode != 0) throw new InvalidOperationException(DescribeSecretToolFailure(stored, "Provider key could not be saved to the desktop keyring."));
    }

    public async Task DeleteAsync(string providerId, CancellationToken cancellationToken = default)
    {
        var id = NormalizeProviderId(providerId);
        var status = GetStatus();
        if (!status.Available) throw new PlatformNotSupportedException(status.Message);
        if (OperatingSystem.IsWindows())
        {
            var path = CredentialPath(id);
            if (File.Exists(path)) File.Delete(path);
            return;
        }
        var result = await RunSecretToolAsync(["clear", "application", ServiceAttribute, "account", id], null, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode is not (0 or 1)) throw new InvalidOperationException(DescribeSecretToolFailure(result, "Provider key could not be removed from the desktop keyring."));
    }

    public static string NormalizeProviderId(string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId)) throw new ArgumentException("A provider identifier is required.", nameof(providerId));
        var value = providerId.Trim().ToLowerInvariant();
        if (value.Length > 64 || value.Any(character => !(char.IsLetterOrDigit(character) || character is '.' or '_' or '-')))
            throw new ArgumentException("The provider identifier has an invalid format.", nameof(providerId));
        return value;
    }

    public static string ValidateValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxCredentialCharacters || value.Any(char.IsControl) || value.Any(char.IsWhiteSpace))
            throw new ArgumentException("The provider key has an invalid format.", nameof(value));
        return value.Trim();
    }

    private string CredentialPath(string providerId) => Path.Combine(Path.GetFullPath(_windowsCredentialDirectory), providerId + ".bin");

    private async Task<(int ExitCode, string StandardOutput, string StandardError)> RunSecretToolAsync(IReadOnlyList<string> arguments, string? input, CancellationToken cancellationToken)
    {
        var executable = _secretToolPath ?? throw new PlatformNotSupportedException(GetStatus().Message);
        var startInfo = new ProcessStartInfo(executable) { UseShellExecute = false, RedirectStandardInput = input is not null, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
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
            if (!cancellationToken.IsCancellationRequested) throw new TimeoutException("The desktop keyring did not respond. Unlock the keyring and try again.");
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
            if (output.Length < maxCharacters) output.Append(buffer, 0, Math.Min(read, maxCharacters - output.Length));
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
        if (source.Length > maxBytes) throw new InvalidDataException("The saved provider key is unexpectedly large.");
        await using var output = new MemoryStream((int)Math.Min(source.Length, maxBytes));
        var buffer = new byte[16 * 1024];
        while (true)
        {
            var read = await source.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
            if (read == 0) break;
            if (output.Length + read > maxBytes) throw new InvalidDataException("The saved provider key is unexpectedly large.");
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }
        return output.ToArray();
    }
}
