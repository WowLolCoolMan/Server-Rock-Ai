using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ServerRock.Core.Infrastructure;

namespace ServerRock.Native;

internal sealed record CompanionState(
    string PetId = "rocky",
    double Left = -1,
    double Top = 88,
    bool IsVisible = false,
    string CurrentState = "idle",
    Dictionary<string, string>? StateAssets = null,
    bool NotifyOnCompletion = true,
    bool NotifyOnErrors = true,
    bool AskBeforeActions = true);

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, WriteIndented = true)]
[JsonSerializable(typeof(CompanionState))]
internal partial class CompanionStateJsonContext : JsonSerializerContext { }

/// <summary>Stores the selected local companion and its window-relative position.</summary>
internal sealed class CompanionStateStore
{
    private const int MaxBytes = 16 * 1024;
    private readonly string _path;

    public CompanionStateStore(string? path = null)
    {
        _path = path ?? Environment.GetEnvironmentVariable("SRAI_COMPANION_STATE_PATH") ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ServerRock", "AI", "companion.json");
    }

    public CompanionState Load()
    {
        if (!File.Exists(_path)) return new CompanionState();

        var json = BoundedFileReader.ReadText(_path, MaxBytes);
        var state = JsonSerializer.Deserialize(json, CompanionStateJsonContext.Default.CompanionState)
            ?? throw new JsonException("The companion preferences file is empty.");
        // A reaction (error, success, …) belongs to the session that caused it: always start idle.
        return Normalize(state) with { CurrentState = "idle" };
    }

    public void Save(CompanionState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var json = BoundedFileReader.EnsureTextSize(
            JsonSerializer.Serialize(Normalize(state), CompanionStateJsonContext.Default.CompanionState), "companion preferences", MaxBytes);
        var fullPath = Path.GetFullPath(_path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var temporaryPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
            {
                writer.Write(json);
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static CompanionState Normalize(CompanionState state)
    {
        var petId = AllowedPetIds.Contains(state.PetId, StringComparer.Ordinal) ? state.PetId : "rocky";
        var left = double.IsFinite(state.Left) && state.Left is >= -1 and <= 100_000 ? state.Left : -1;
        var top = double.IsFinite(state.Top) && state.Top is >= 0 and <= 100_000 ? state.Top : 88;
        var assets = (state.StateAssets ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase))
            .Where(item => AllowedStates.Contains(item.Key, StringComparer.OrdinalIgnoreCase)
                && item.Value.Length <= 1024
                && !item.Value.Any(char.IsControl)
                && Path.IsPathFullyQualified(item.Value)
                && new[] { ".svg", ".png", ".jpg", ".jpeg", ".webp" }.Contains(Path.GetExtension(item.Value), StringComparer.OrdinalIgnoreCase))
            .ToDictionary(item => item.Key.ToLowerInvariant(), item => item.Value, StringComparer.OrdinalIgnoreCase);
        var currentState = AllowedStates.Contains(state.CurrentState, StringComparer.OrdinalIgnoreCase)
            ? state.CurrentState.ToLowerInvariant()
            : "idle";
        return state with { PetId = petId, Left = left, Top = top, CurrentState = currentState, StateAssets = assets };
    }

    // Must list every pet in CompanionChoices (MainPage.Companion.cs); an unknown ID is
    // saved back as "rocky".
    internal static readonly string[] AllowedPetIds = ["rocky", "byte", "sprout", "nova", "ember"];

    internal static readonly string[] AllowedStates = ["idle", "thinking", "coding", "coding_malicious", "stuck", "success", "error"];
}
