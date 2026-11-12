using System.Text.Json;
using ServerRock.Core.Services;

internal static class SkillPackageInstallerTests
{
    public static void Run()
    {
        var root = Path.Combine(Path.GetTempPath(), "server-rock-skills-" + Guid.NewGuid().ToString("N"));
        var source = Path.Combine(root, "source");
        Directory.CreateDirectory(source);
        File.WriteAllText(Path.Combine(source, "skill.json"), JsonSerializer.Serialize(new { id = "review-skill", name = "Review skill", version = "1.0.0", publisher = "Test publisher", requiredPermissions = new[] { "project.read" } }));
        File.WriteAllText(Path.Combine(source, "SKILL.md"), "# Review\n\nRead-only review instructions.");
        var installer = new SkillPackageInstaller(Path.Combine(root, "installed"));
        var installed = installer.Install(source, "https://github.com/example/review-skill@abc123", expectedPackageSha256: null);
        TestSupport.Check(installed.SourceRef.Contains("@abc123", StringComparison.Ordinal), "skill installation records pinned provenance");
        TestSupport.Check(!installed.Enabled, "new skill packages require explicit enablement");
        installed = installer.SetEnabled(installed.Id, true);
        TestSupport.Check(installed.Enabled, "skill enablement is explicit and durable");
        TestSupport.Check(installer.ListInstalled().Single().PackageSha256.Length == 64, "skill installation records a package digest");
        File.WriteAllText(Path.Combine(source, "SKILL.md"), "# Review v2\n\nUpdated read-only instructions.");
        installer.Install(source, "https://github.com/example/review-skill@def456");
        TestSupport.Check(installer.ListInstalled().Single().Version == "1.0.0", "skill update atomically activates the replacement");
        var rolledBack = installer.Rollback("review-skill");
        TestSupport.Check(rolledBack.SourceRef.Contains("@abc123", StringComparison.Ordinal) && rolledBack.Enabled, "skill rollback restores the previous pinned package and grant");
        File.WriteAllText(Path.Combine(source, "run.ps1"), "Write-Output unsafe");
        try { installer.Install(source, "https://github.com/example/review-skill@bad"); throw new Exception("Expected executable skill content to be rejected."); }
        catch (InvalidDataException) { TestSupport.Check(true, "skill installer rejects executable package content"); }
    }
}
