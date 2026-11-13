// Diagnostic log redaction and export (AppLogService).
// Uses TestSupport (Check, StubHandler, …). Run by Program.cs.

using SkaiDesktop.Core.Infrastructure;
using SkaiDesktop.Core.Models;
using SkaiDesktop.Core.Services;
using static TestSupport;

internal static class DiagnosticsTests
{
    public static async Task RunAsync()
    {
        var logDirectory = Path.Combine(Path.GetTempPath(), "server-rock-log-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var logs = new AppLogService(logDirectory);
            logs.Info("Diagnostics", "request bearer abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");
            logs.Error("Crash", new InvalidOperationException("api_key=super-secret-value path=C:\\Users\\basic\\private.txt"));
            var text = await File.ReadAllTextAsync(Path.Combine(logDirectory, "server-rock.log"));
            Check(!text.Contains("super-secret-value", StringComparison.Ordinal) && !text.Contains("C:\\Users\\basic", StringComparison.Ordinal) && text.Contains("[REDACTED]", StringComparison.Ordinal), "diagnostic logs redact secrets and machine paths");
            var archive = logs.Export(Path.Combine(logDirectory, "exports"));
            Check(File.Exists(archive), "diagnostic logs export to a shareable archive");
        }
        finally { if (System.IO.Directory.Exists(logDirectory)) System.IO.Directory.Delete(logDirectory, recursive: true); }
    }
}
