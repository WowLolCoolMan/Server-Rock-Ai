// Loads/saves named projects (projects.json). A corrupt file is reported and left untouched.
// Depends on: LocalProject, BoundedFileReader, AtomicFileWriter. Used by: MainPage projects.

using System.Text.Json;
using ServerRock.Core.Models;

namespace ServerRock.Core.Infrastructure;

/// <summary>Stores named project contexts in the user's local app data directory.</summary>
public sealed class ProjectStore
{
    public const int MaxProjects = 2_000;
    private const int MaxIdLength = 128;
    private const int MaxNameLength = 200;
    private const int MaxInstructionsLength = 500_000;
    private const int MaxFolderPaths = 128;
    private const int MaxFolderPathLength = 32_768;
    private const int MaxGitHubRepositoryLength = 256;
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _path;

    public ProjectStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ServerRock", "AI", "projects.json");
    }

    public List<LocalProject> Load()
    {
        if (!File.Exists(_path)) return [];
        try
        {
            var projects = JsonSerializer.Deserialize<List<LocalProject>>(BoundedFileReader.ReadText(_path), Options) ?? [];
            foreach (var project in projects)
            {
                if (project is null) throw new InvalidDataException("Saved projects contain an empty entry. The original file was left untouched.");
                project.Id ??= Guid.NewGuid().ToString("N");
                project.Name ??= "New project";
                project.Instructions ??= "";
                project.FolderPaths ??= [];
            }
            // A valid JSON document can still contain duplicate identities or
            // unsafe values. Reject it before the UI maps a project action to
            // the wrong saved project, and leave the source file available for
            // recovery just as we do for malformed JSON.
            ValidateForSave(projects);
            return projects;
        }
        catch (JsonException error)
        {
            throw new InvalidDataException("Saved projects could not be read. The original file was left untouched.", error);
        }
        catch (InvalidDataException error)
        {
            throw new InvalidDataException("Saved projects could not be read. The original file was left untouched.", error);
        }
    }

    public void Save(IEnumerable<LocalProject> projects)
    {
        ArgumentNullException.ThrowIfNull(projects);
        var items = projects.Take(MaxProjects + 1).ToArray();
        ValidateForSave(items);
        var json = BoundedFileReader.EnsureTextSize(JsonSerializer.Serialize(items, Options), "saved projects");
        AtomicFileWriter.WriteText(_path, json);
    }

    private static void ValidateForSave(IReadOnlyList<LocalProject> projects)
    {
        if (projects.Count > MaxProjects) throw new InvalidDataException($"Saved projects can contain at most {MaxProjects} entries.");
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var project in projects)
        {
            if (project is null) throw new InvalidDataException("A saved project entry is missing.");
            if (string.IsNullOrWhiteSpace(project.Id) || project.Id.Length > MaxIdLength || project.Id.Any(char.IsControl) || !ids.Add(project.Id))
                throw new InvalidDataException("Every saved project needs a unique valid ID.");
            if (string.IsNullOrWhiteSpace(project.Name) || project.Name.Length > MaxNameLength || project.Name.Any(char.IsControl))
                throw new InvalidDataException("Every saved project needs a valid name.");
            if (project.Instructions is null || project.Instructions.Length > MaxInstructionsLength)
                throw new InvalidDataException("Project instructions exceed the saved-file limit.");
            if (project.FolderPaths is null || project.FolderPaths.Count > MaxFolderPaths || project.FolderPaths.Any(path => string.IsNullOrWhiteSpace(path) || path.Length > MaxFolderPathLength || path.Any(char.IsControl)))
                throw new InvalidDataException("Project folder paths are incomplete or exceed the saved-file limit.");
            if (project.GitHubRepository is { } repository && (repository.Length > MaxGitHubRepositoryLength || repository.Any(char.IsControl)))
                throw new InvalidDataException("The project GitHub repository value is invalid.");
        }
    }
}
