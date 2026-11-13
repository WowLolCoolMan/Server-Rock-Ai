// Diagnostic log file with secret/path redaction and zip export.
// Depends on: nothing in Core. Used by: App.Log (the single app-wide instance) and the Settings diagnostics export.

using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace SkaiDesktop.Core.Infrastructure;

/// <summary>
/// Writes a small, shareable diagnostic trail without persisting prompts, credentials,
/// or machine-specific paths. The log directory is intentionally plain text so users
/// can attach the folder to a support request.
/// </summary>
public sealed class AppLogService
{
    private static readonly Regex SecretPattern = new(
        "(?i)(bearer\\s+|api[_-]?key\\s*[=:]\\s*|access[_-]?token\\s*[=:]\\s*|refresh[_-]?token\\s*[=:]\\s*|password\\s*[=:]\\s*)[^\\s,;]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex PathPattern = new(@"(?<![A-Za-z0-9])[A-Za-z]:\\[^\r\n ]+|/home/[^\r\n ]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex LongValuePattern = new(@"(?<![A-Za-z0-9])[A-Za-z0-9+/=_-]{48,}(?![A-Za-z0-9])", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private readonly object _gate = new();

    public AppLogService(string? directory = null)
    {
        Directory = Path.GetFullPath(directory ?? Environment.GetEnvironmentVariable("SRAI_LOG_DIRECTORY") ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ServerRock", "AI", "logs"));
        System.IO.Directory.CreateDirectory(Directory);
    }

    public string Directory { get; }

    public void Info(string category, string message) => Write("INFO", category, message);
    public void Warning(string category, string message) => Write("WARN", category, message);
    public void Error(string category, Exception exception, string? message = null) =>
        Write("ERROR", category, $"{message ?? exception.Message} | {exception.GetType().Name}: {exception}");

    public string Export(string destinationDirectory)
    {
        var destination = Path.GetFullPath(destinationDirectory);
        DirectoryInfo? parent = System.IO.Directory.Exists(destination) ? new DirectoryInfo(destination) : new DirectoryInfo(Path.GetDirectoryName(destination)!);
        parent.Create();
        var archive = Path.Combine(parent.FullName, "server-rock-logs-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss") + ".zip");
        var stagingArchive = Path.Combine(Path.GetTempPath(), "server-rock-logs-" + Guid.NewGuid().ToString("N") + ".zip");
        lock (_gate)
        {
            if (File.Exists(archive)) File.Delete(archive);
            try
            {
                ZipFile.CreateFromDirectory(Directory, stagingArchive, CompressionLevel.Fastest, includeBaseDirectory: true);
                File.Move(stagingArchive, archive);
            }
            finally
            {
                if (File.Exists(stagingArchive)) File.Delete(stagingArchive);
            }
        }
        return archive;
    }

    internal static string Redact(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var redacted = SecretPattern.Replace(value, m =>
        {
            var separator = m.Value.IndexOfAny(['=', ':']);
            if (separator >= 0) return m.Value[..(separator + 1)] + "[REDACTED]";
            var space = m.Value.LastIndexOf(' ');
            return (space >= 0 ? m.Value[..(space + 1)] : string.Empty) + "[REDACTED]";
        });
        redacted = PathPattern.Replace(redacted, "[PATH]");
        return LongValuePattern.Replace(redacted, "[REDACTED]");
    }

    private void Write(string level, string category, string message)
    {
        var line = $"{DateTimeOffset.UtcNow:O} [{level}] {Redact(category)}: {Redact(message)}{Environment.NewLine}";
        lock (_gate)
        {
            var path = Path.Combine(Directory, "server-rock.log");
            File.AppendAllText(path, line, new UTF8Encoding(false));
            var info = new FileInfo(path);
            if (info.Length > 2_000_000)
            {
                var rotated = Path.Combine(Directory, "server-rock.previous.log");
                File.Copy(path, rotated, overwrite: true);
                File.WriteAllText(path, string.Empty, new UTF8Encoding(false));
            }
        }
    }
}
