// Rules for chat attachments: allowed file types, secret-looking names to block,
// size/character limits, and image signature checks.
// Depends on: ChatAttachment. Used by: MainPage attachments, OpenAiCompatibleProvider, ConversationArchive.

using ServerRock.Core.Models;

namespace ServerRock.Core.Infrastructure;

/// <summary>Limits and screens files explicitly attached to a chat message.</summary>
public static class ChatAttachmentPolicy
{
    public const int MaxFiles = 5;
    public const int MaxCharactersPerFile = 16_000;
    public const int MaxTotalCharacters = 48_000;
    public const long MaxFileBytes = 2 * 1024 * 1024;
    public const long MaxPdfBytes = 20 * 1024 * 1024;
    public const long MaxTotalImageBytes = 6 * 1024 * 1024;

    public static async Task<byte[]> ReadBytesAsync(string path, long maxBytes, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path) || maxBytes < 1 || maxBytes > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(maxBytes));
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (stream.Length > maxBytes) throw new InvalidDataException("The attachment exceeds its allowed size.");
        await using var output = new MemoryStream((int)Math.Min(stream.Length, maxBytes));
        var buffer = new byte[64 * 1024];
        while (true)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
            if (read == 0) break;
            if (output.Length + read > maxBytes) throw new InvalidDataException("The attachment grew beyond its allowed size.");
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }
        return output.ToArray();
    }

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".markdown", ".csv", ".json", ".xml", ".yaml", ".yml", ".toml",
        ".html", ".htm", ".css", ".js", ".ts", ".jsx", ".tsx", ".cs", ".py", ".java",
        ".go", ".rs", ".c", ".h", ".cpp", ".hpp", ".sh", ".sql", ".log"
    };

    public static bool IsAllowedPdfName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        return !IsBlockedName(name) && Path.GetExtension(name).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
    }

    private static readonly HashSet<string> BlockedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".env", ".env.local", ".env.production", "id_rsa", "id_ed25519", "credentials", "secrets"
    };

    public static bool IsAllowedName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        return !IsBlockedName(name) && (AllowedExtensions.Contains(Path.GetExtension(name)) || GetImageMediaType(name) is not null || IsAllowedPdfName(name));
    }

    public static bool IsAllowedTextName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        return !IsBlockedName(name) && AllowedExtensions.Contains(Path.GetExtension(name));
    }

    private static bool IsBlockedName(string name) => string.IsNullOrWhiteSpace(name) ||
        name.StartsWith(".", StringComparison.Ordinal) && BlockedNames.Contains(name) ||
        BlockedNames.Contains(name) || name.Contains(".env.", StringComparison.OrdinalIgnoreCase);

    public static string? GetImageMediaType(string fileName) => Path.GetExtension(Path.GetFileName(fileName)).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        _ => null
    };

    public static bool IsAllowedImageMediaType(string? mediaType) => mediaType is "image/png" or "image/jpeg";

    public static bool HasValidImageSignature(string? mediaType, ReadOnlySpan<byte> bytes) => mediaType switch
    {
        "image/png" => bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
        "image/jpeg" => bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
        _ => false
    };

    public static string BoundContent(string content) => content.Length <= MaxCharactersPerFile
        ? content
        : content[..MaxCharactersPerFile];

    public static int RemainingCharacters(IEnumerable<ChatAttachment> attachments) =>
        Math.Max(0, MaxTotalCharacters - attachments.Where(item => !item.IsImage).Sum(item => item.Content.Length));

    public static long RemainingImageBytes(IEnumerable<ChatAttachment> attachments) =>
        Math.Max(0, MaxTotalImageBytes - attachments.Where(item => item.IsImage)
            .Sum(item => Math.Max(0, item.Content.Length * 3L / 4 - (item.Content.EndsWith("==", StringComparison.Ordinal) ? 2 : item.Content.EndsWith("=", StringComparison.Ordinal) ? 1 : 0))));

    /// <summary>
    /// Identifies one endpoint + exact set of attachments (names, types, contents), so the
    /// "send these files to a remote server?" question is asked once per set.
    /// </summary>
    public static string ConsentKey(string endpoint, IEnumerable<ChatAttachment> attachments)
    {
        using var hash = System.Security.Cryptography.IncrementalHash.CreateHash(System.Security.Cryptography.HashAlgorithmName.SHA256);
        var separator = new byte[] { 0 };
        foreach (var attachment in attachments.OrderBy(item => item.FileName, StringComparer.OrdinalIgnoreCase))
        {
            hash.AppendData(System.Text.Encoding.UTF8.GetBytes(attachment.FileName));
            hash.AppendData(separator);
            hash.AppendData(System.Text.Encoding.UTF8.GetBytes(attachment.MediaType ?? "text"));
            hash.AppendData(separator);
            hash.AppendData(System.Text.Encoding.UTF8.GetBytes(attachment.Content));
            hash.AppendData(separator);
        }
        return endpoint + "\n" + Convert.ToHexString(hash.GetHashAndReset());
    }
}
