using System.Text;

namespace ServerRock.Core.Services;

public sealed record LocalHealthFinding(string Severity, string Title, string Evidence, string? Path = null);

public sealed record LocalProjectHealthReport(
    int Files,
    int Directories,
    int LinesSampled,
    IReadOnlyList<string> Languages,
    IReadOnlyList<string> DependencyManifests,
    IReadOnlyList<string> EntryPoints,
    IReadOnlyList<LocalHealthFinding> Findings)
{
    public string ToCompactMarkdown(string projectLabel)
    {
        var output = new StringBuilder();
        output.AppendLine($"## Local project health · {projectLabel}");
        output.AppendLine($"Scanned {Files:N0} files in {Directories:N0} directories; sampled {LinesSampled:N0} lines. No model or network request was used.");
        output.AppendLine($"Languages: {(Languages.Count == 0 ? "none detected" : string.Join(", ", Languages))}");
        output.AppendLine($"Dependency manifests: {(DependencyManifests.Count == 0 ? "none detected" : string.Join(", ", DependencyManifests))}");
        output.AppendLine($"Likely entry points: {(EntryPoints.Count == 0 ? "none detected" : string.Join(", ", EntryPoints.Take(8)))}");
        output.AppendLine("\n### Findings");
        if (Findings.Count == 0) output.AppendLine("- **Info**: No obvious local heuristics were triggered.");
        foreach (var finding in Findings.Take(30))
            output.AppendLine($"- **{finding.Severity}**: {finding.Title} — {finding.Evidence}{(finding.Path is null ? string.Empty : $" (`{finding.Path}`)")}");
        return output.ToString().TrimEnd();
    }
}

/// <summary>
/// Bounded, offline project inspection. This intentionally produces a compact evidence packet
/// that can be reviewed locally or sent to a model instead of uploading the whole repository.
/// </summary>
public sealed class LocalProjectHealthAnalyzer
{
    private const int MaxFiles = 2_000;
    private const int MaxLinesPerFile = 1_200;
    private const int MaxFindings = 80;
    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
        { ".git", "bin", "obj", "node_modules", "dist", "release-check", "test-results", ".vs" };
    private static readonly Dictionary<string, string> LanguageByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".cs"] = "C#", [".xaml"] = "XAML", [".ts"] = "TypeScript", [".tsx"] = "TypeScript/React",
        [".js"] = "JavaScript", [".jsx"] = "JavaScript/React", [".py"] = "Python", [".rs"] = "Rust",
        [".go"] = "Go", [".java"] = "Java", [".kt"] = "Kotlin", [".swift"] = "Swift",
        [".cpp"] = "C++", [".h"] = "C/C++", [".md"] = "Markdown"
    };

    public LocalProjectHealthReport Analyze(IEnumerable<string> roots)
    {
        var files = new List<string>();
        var directories = 0;
        foreach (var root in roots.Where(Directory.Exists).Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                foreach (var directory in EnumerateDirectories(root))
                {
                    directories++;
                    foreach (var file in Directory.EnumerateFiles(directory))
                    {
                        if (files.Count >= MaxFiles) break;
                        files.Add(file);
                    }
                    if (files.Count >= MaxFiles) break;
                }
            }
            catch { /* a locked or disappearing folder is evidence-free, not fatal */ }
        }

        var languages = files.Select(file => Path.GetExtension(file))
            .Where(LanguageByExtension.ContainsKey).Select(extension => LanguageByExtension[extension])
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value).ToArray();
        var manifests = files.Where(IsDependencyManifest).Select(file => Path.GetFileName(file))
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value).ToArray();
        var entryPoints = files.Where(IsEntryPoint).Select(file => Path.GetFileName(file) ?? string.Empty)
            .Where(name => name.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToArray();
        var findings = new List<LocalHealthFinding>();
        var lines = 0;

        foreach (var file in files)
        {
            if (!IsTextFile(file)) continue;
            try
            {
                var sampled = File.ReadLines(file).Take(MaxLinesPerFile).ToArray();
                lines += sampled.Length;
                var relative = Path.GetFileName(file);
                var text = string.Join('\n', sampled);
                if (text.Contains("TODO", StringComparison.OrdinalIgnoreCase) || text.Contains("FIXME", StringComparison.OrdinalIgnoreCase))
                    Add(findings, new("Info", "Deferred work marker", "TODO/FIXME marker found", relative));
                if (LooksLikeSecret(text))
                    Add(findings, new("High", "Possible credential or secret", "A bounded scan matched a token-like assignment; verify and rotate if real", relative));
                if (Path.GetFileName(file).Equals("MainPage.xaml.cs", StringComparison.OrdinalIgnoreCase) && sampled.Length > 2_500)
                    Add(findings, new("Medium", "Large UI orchestration file", $"{sampled.Length:N0}+ sampled lines in a single page class", relative));
            }
            catch { }
        }

        if (files.Count >= MaxFiles) Add(findings, new("Info", "Scan limit reached", $"Inspection stopped at {MaxFiles:N0} files to stay responsive and token-efficient."));
        if (manifests.Length == 0) Add(findings, new("Medium", "Dependency inventory unavailable", "No recognized package manifest was found in the selected folders."));
        if (entryPoints.Length == 0) Add(findings, new("Info", "Entry point not obvious", "No conventional application entry point was detected by local heuristics."));
        return new(files.Count, directories, lines, languages, manifests, entryPoints, findings);
    }

    private static IEnumerable<string> EnumerateDirectories(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            yield return current;
            IEnumerable<string> children;
            try { children = Directory.EnumerateDirectories(current); }
            catch { continue; }
            foreach (var child in children)
                if (!IgnoredDirectories.Contains(Path.GetFileName(child))) pending.Push(child);
        }
    }

    private static bool IsDependencyManifest(string file)
    {
        var name = Path.GetFileName(file).ToLowerInvariant();
        return name is "package.json" or "package-lock.json" or "pnpm-lock.yaml" or "yarn.lock" or
            "cargo.toml" or "go.mod" or "pyproject.toml" or "requirements.txt" || name.EndsWith(".csproj", StringComparison.Ordinal);
    }

    private static bool IsEntryPoint(string file) => Path.GetFileName(file).ToLowerInvariant() is
        "program.cs" or "app.xaml" or "app.xaml.cs" or "main.py" or "main.go" or "main.rs" or "index.js";

    private static bool IsTextFile(string file) => LanguageByExtension.ContainsKey(Path.GetExtension(file)) ||
        Path.GetFileName(file).Contains("lock", StringComparison.OrdinalIgnoreCase) ||
        Path.GetFileName(file).Equals("package.json", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeSecret(string text) =>
        text.Contains("api_key=", StringComparison.OrdinalIgnoreCase) || text.Contains("apikey:", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("access_token=", StringComparison.OrdinalIgnoreCase) || text.Contains("-----BEGIN PRIVATE KEY-----", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("ghp_", StringComparison.Ordinal) || text.Contains("sk-", StringComparison.Ordinal);

    private static void Add(List<LocalHealthFinding> findings, LocalHealthFinding finding)
    {
        if (findings.Count < MaxFindings) findings.Add(finding);
    }
}
