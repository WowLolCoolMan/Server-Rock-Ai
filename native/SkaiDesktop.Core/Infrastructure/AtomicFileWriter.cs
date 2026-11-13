// Writes a file via temp-file-then-rename so a crash never leaves a half-written store.
// Used by: JsonSettingsStore, ConversationStore, ProjectStore, WorkspaceUndoStore.

using System.Text;

namespace SkaiDesktop.Core.Infrastructure;

internal static class AtomicFileWriter
{
    public static void WriteText(string destination, string content)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(content);
        var fullPath = Path.GetFullPath(destination);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var temporaryPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
                writer.Write(content);
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
