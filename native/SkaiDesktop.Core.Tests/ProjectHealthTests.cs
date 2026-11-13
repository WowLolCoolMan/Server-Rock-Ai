// Offline project health scan and architecture map (LocalProjectHealthAnalyzer): finds
// manifests, entry points, and risk markers from bounded samples, and never follows links
// out of the project. Added on main; moved here when main was merged into native/polish.
// Uses TestSupport (Check). Run by Program.cs.

using SkaiDesktop.Core.Services;
using static TestSupport;

internal static class ProjectHealthTests
{
    public static async Task RunAsync()
    {
        var healthRoot = Path.Combine(Path.GetTempPath(), "server-rock-health-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(healthRoot);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(healthRoot, "package.json"), "{\"name\":\"fixture\"}");
            await File.WriteAllTextAsync(Path.Combine(healthRoot, "Program.cs"), "// TODO: cover this path\nvar api_key=\"test\";\n");
            var health = new LocalProjectHealthAnalyzer().Analyze([healthRoot]);
            Check(health.Files == 2 && health.DependencyManifests.Contains("package.json") && health.EntryPoints.Contains("Program.cs") &&
                health.Findings.Any(item => item.Title == "Deferred work marker") && health.Findings.Any(item => item.Title == "Possible credential or secret") &&
                health.ToCompactMarkdown("fixture").Contains("No model or network request was used", StringComparison.Ordinal) &&
                health.ToCompactMarkdown("fixture", includeArchitectureMap: true).Contains("### Architecture map", StringComparison.Ordinal),
                "local project health analyzer produces bounded token-free evidence");
        }
        finally { if (Directory.Exists(healthRoot)) Directory.Delete(healthRoot, recursive: true); }

        var safetyRoot = Path.Combine(Path.GetTempPath(), "server-rock-health-safety-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(safetyRoot);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(safetyRoot, "MainPage.xaml.cs"), new string('x', 400_000));
            var outside = Path.Combine(Path.GetTempPath(), "server-rock-health-outside-" + Guid.NewGuid().ToString("N") + ".cs");
            await File.WriteAllTextAsync(outside, "var access_token=\"must-not-be-read\";");
            try
            {
                var link = Path.Combine(safetyRoot, "linked.cs");
                try { File.CreateSymbolicLink(link, outside); }
                catch (Exception exception) when (exception is UnauthorizedAccessException or IOException or PlatformNotSupportedException)
                {
                    link = string.Empty;
                }
                var safe = new LocalProjectHealthAnalyzer().Analyze([safetyRoot]);
                Check(safe.Files == 1 && safe.Findings.Any(item => item.Title == "Large UI orchestration file") &&
                    (link.Length == 0 || safe.Findings.All(item => item.Path != link)),
                    "local project health scan bounds samples and excludes reparse-point files");
            }
            finally { if (File.Exists(outside)) File.Delete(outside); }
        }
        finally { if (Directory.Exists(safetyRoot)) Directory.Delete(safetyRoot, recursive: true); }
    }
}
