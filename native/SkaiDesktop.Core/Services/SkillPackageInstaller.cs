using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SkaiDesktop.Core.Infrastructure;
using SkaiDesktop.Core.Models;

namespace SkaiDesktop.Core.Services;

/// <summary>Stages and activates Markdown-only skills without executing package content.</summary>
public sealed class SkillPackageInstaller
{
    public const int MaxFiles = 128;
    public const int MaxFileBytes = 256 * 1024;
    public const long MaxPackageBytes = 5 * 1024 * 1024;
    private const string ManifestFileName = "skill.json";
    private const string SkillFileName = "SKILL.md";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true, MaxDepth = 12 };
    private readonly string _root;

    public SkillPackageInstaller(string? root = null)
    {
        _root = Path.GetFullPath(root ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ServerRock", "AI", "skills"));
    }

    public IReadOnlyList<InstalledSkill> ListInstalled()
    {
        var activeRoot = Path.Combine(_root, "active");
        if (!Directory.Exists(activeRoot)) return [];
        var result = new List<InstalledSkill>();
        foreach (var directory in Directory.EnumerateDirectories(activeRoot))
        {
            var manifestPath = Path.Combine(directory, ManifestFileName);
            if (!File.Exists(manifestPath)) continue;
            var manifest = ReadManifest(manifestPath);
            result.Add(ToInstalled(manifest, directory));
        }
        return result.OrderBy(item => item.Id, StringComparer.Ordinal).ToArray();
    }

    public bool CanRollback(string id)
    {
        ValidateId(id);
        var backups = Path.Combine(_root, "backups", id);
        return Directory.Exists(backups) && Directory.EnumerateDirectories(backups).Any();
    }

    public string ReadInstructions(string id)
    {
        ValidateId(id);
        var skill = ListInstalled().SingleOrDefault(item => item.Id.Equals(id, StringComparison.Ordinal));
        if (skill is null) throw new InvalidOperationException("The installed skill is no longer available.");
        var root = Path.GetFullPath(skill.ActivePath);
        var path = Path.GetFullPath(Path.Combine(root, SkillFileName));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("The installed skill path is invalid.");
        return BoundedFileReader.ReadText(path, MaxFileBytes);
    }

    public SkillPackagePreview Preview(string sourceDirectory, string? expectedPackageSha256 = null)
    {
        var source = Path.GetFullPath(sourceDirectory);
        if (!Directory.Exists(source) || IsLink(source)) throw new DirectoryNotFoundException("The skill source directory is unavailable or linked.");
        var manifest = ReadManifest(Path.Combine(source, ManifestFileName));
        var files = ReadPackageFiles(source);
        var digest = ComputePackageDigest(files);
        if (!string.IsNullOrWhiteSpace(expectedPackageSha256) && !digest.Equals(expectedPackageSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The downloaded skill package checksum does not match its pinned digest.");
        var existing = ListInstalled().SingleOrDefault(item => item.Id.Equals(manifest.Id, StringComparison.Ordinal));
        return new SkillPackagePreview(manifest.Id, manifest.Name, manifest.Version, manifest.Publisher, digest, manifest.RequiredPermissions.ToArray(), existing is not null, existing?.Version, existing?.PackageSha256);
    }

    public InstalledSkill Install(string sourceDirectory, string sourceRef, string? expectedPackageSha256 = null)
    {
        sourceRef = SkillSourceReference.Validate(sourceRef);
        var source = Path.GetFullPath(sourceDirectory);
        if (!Directory.Exists(source) || IsLink(source)) throw new DirectoryNotFoundException("The skill source directory is unavailable or linked.");
        var manifest = ReadManifest(Path.Combine(source, ManifestFileName));
        ValidateManifest(manifest);
        var files = ReadPackageFiles(source);
        var digest = ComputePackageDigest(files);
        if (!string.IsNullOrWhiteSpace(expectedPackageSha256) && !digest.Equals(expectedPackageSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The downloaded skill package checksum does not match its pinned digest.");
        manifest.SourceRef = sourceRef;
        manifest.PackageSha256 = digest;

        var activeRoot = Path.Combine(_root, "active");
        var backupRoot = Path.Combine(_root, "backups", manifest.Id);
        var stage = Path.Combine(_root, "staging", manifest.Id + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stage);
        try
        {
            foreach (var file in files)
            {
                var target = Path.Combine(stage, file.RelativePath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.WriteAllBytes(target, file.Content);
            }
            File.WriteAllText(Path.Combine(stage, ManifestFileName), JsonSerializer.Serialize(manifest, JsonOptions), new UTF8Encoding(false));
            Directory.CreateDirectory(activeRoot);
            Directory.CreateDirectory(backupRoot);
            var active = Path.Combine(activeRoot, manifest.Id);
            if (Directory.Exists(active))
            {
                var backup = Path.Combine(backupRoot, DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff") + "-" + Guid.NewGuid().ToString("N"));
                Directory.Move(active, backup);
            }
            Directory.Move(stage, active);
            return ToInstalled(manifest, active);
        }
        finally
        {
            if (Directory.Exists(stage)) Directory.Delete(stage, recursive: true);
        }
    }

    public InstalledSkill SetEnabled(string id, bool enabled)
    {
        ValidateId(id);
        var active = Path.Combine(_root, "active", id);
        var manifestPath = Path.Combine(active, ManifestFileName);
        if (!File.Exists(manifestPath)) throw new InvalidOperationException("The installed skill is no longer available.");
        var manifest = ReadManifest(manifestPath);
        manifest.Enabled = enabled;
        AtomicFileWriter.WriteText(manifestPath, JsonSerializer.Serialize(manifest, JsonOptions));
        return ToInstalled(manifest, active);
    }

    public InstalledSkill Rollback(string id)
    {
        ValidateId(id);
        var active = Path.Combine(_root, "active", id);
        var backups = Path.Combine(_root, "backups", id);
        if (!Directory.Exists(active) || !Directory.Exists(backups)) throw new InvalidOperationException("No installed skill backup is available for rollback.");
        var previous = Directory.EnumerateDirectories(backups).OrderByDescending(item => item, StringComparer.Ordinal).FirstOrDefault();
        if (previous is null) throw new InvalidOperationException("No installed skill backup is available for rollback.");
        var displaced = Path.Combine(backups, "rollback-" + Guid.NewGuid().ToString("N"));
        Directory.Move(active, displaced);
        try
        {
            Directory.Move(previous, active);
            return ToInstalled(ReadManifest(Path.Combine(active, ManifestFileName)), active);
        }
        catch
        {
            if (Directory.Exists(active)) Directory.Delete(active, recursive: true);
            Directory.Move(displaced, active);
            throw;
        }
    }

    private static List<PackageFile> ReadPackageFiles(string source)
    {
        var files = Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories).ToArray();
        if (files.Length > MaxFiles) throw new InvalidDataException($"The skill package contains more than {MaxFiles} files.");
        var packageBytes = 0L;
        var result = new List<PackageFile>();
        foreach (var file in files)
        {
            if (IsLink(file)) throw new InvalidDataException("Skill packages cannot contain symbolic links or reparse points.");
            var relative = Path.GetRelativePath(source, file).Replace(Path.DirectorySeparatorChar, '/');
            if (relative.Equals(ManifestFileName, StringComparison.OrdinalIgnoreCase)) continue;
            if (!relative.Equals(SkillFileName, StringComparison.Ordinal) && !relative.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Skill packages may contain Markdown instructions only; executable and binary files are rejected.");
            if (relative.Split('/').Any(part => part is "." or ".." || part.Length == 0)) throw new InvalidDataException("The skill package contains an unsafe relative path.");
            var info = new FileInfo(file);
            if (info.Length > MaxFileBytes) throw new InvalidDataException($"The skill file exceeds the {MaxFileBytes:N0}-byte limit.");
            var content = File.ReadAllBytes(file);
            packageBytes += content.Length;
            if (packageBytes > MaxPackageBytes) throw new InvalidDataException($"The skill package exceeds the {MaxPackageBytes:N0}-byte limit.");
            _ = DecodeUtf8(content);
            result.Add(new PackageFile(relative, content));
        }
        if (result.All(file => !file.RelativePath.Equals(SkillFileName, StringComparison.Ordinal))) throw new InvalidDataException("The skill package must contain a root SKILL.md file.");
        return result.OrderBy(file => file.RelativePath, StringComparer.Ordinal).ToList();
    }

    private static SkillPackageManifest ReadManifest(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("The skill package manifest was not found.");
        try
        {
            var json = BoundedFileReader.ReadText(path, MaxFileBytes);
            var manifest = JsonSerializer.Deserialize<SkillPackageManifest>(json, JsonOptions) ?? throw new InvalidDataException("The skill package manifest is empty.");
            ValidateManifest(manifest);
            return manifest;
        }
        catch (JsonException error) { throw new InvalidDataException("The skill package manifest is invalid JSON.", error); }
    }

    private static void ValidateManifest(SkillPackageManifest manifest)
    {
        ValidateId(manifest.Id);
        foreach (var value in new[] { manifest.Name, manifest.Version, manifest.Publisher })
            if (string.IsNullOrWhiteSpace(value) || value.Length > 256 || value.Any(char.IsControl)) throw new InvalidDataException("The skill package manifest contains invalid identity metadata.");
        if (manifest.RequiredPermissions.Count > 32 || manifest.RequiredPermissions.Any(permission => string.IsNullOrWhiteSpace(permission) || permission.Length > 128 || permission.Any(char.IsControl))) throw new InvalidDataException("The skill package manifest contains invalid permissions.");
    }

    private static void ValidateId(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > 80 || id.Any(ch => !(char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.'))) throw new InvalidDataException("The skill package ID is invalid.");
    }

    private static string ComputePackageDigest(IEnumerable<PackageFile> files)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var file in files)
        {
            hash.AppendData(Encoding.UTF8.GetBytes(file.RelativePath));
            hash.AppendData([0]);
            hash.AppendData(file.Content);
            hash.AppendData([0]);
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static string DecodeUtf8(byte[] content) => new UTF8Encoding(false, true).GetString(content);

    private static InstalledSkill ToInstalled(SkillPackageManifest manifest, string path) =>
        new(manifest.Id, manifest.Name, manifest.Version, manifest.Publisher, manifest.SourceRef, manifest.PackageSha256, path, manifest.RequiredPermissions.ToArray(), manifest.Enabled);

    private static bool IsLink(string path)
    {
        try { return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0; }
        catch (IOException) { return true; }
        catch (UnauthorizedAccessException) { return true; }
    }

    private sealed record PackageFile(string RelativePath, byte[] Content);
}
