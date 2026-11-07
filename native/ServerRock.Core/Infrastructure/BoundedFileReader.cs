// Reads local JSON/text files with a hard byte limit before parsing.
// Used by: every *Store class, ConversationArchive, and CompanionStateStore (UI project).

using System.Text;

namespace ServerRock.Core.Infrastructure;

/// <summary>Rejects unexpectedly large local persistence files before deserialization.</summary>
public static class BoundedFileReader
{
    public const int MaxJsonBytes = 8 * 1024 * 1024;

    public static string ReadText(string path, int maxBytes = MaxJsonBytes)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (maxBytes < 1) throw new ArgumentOutOfRangeException(nameof(maxBytes));
        using var source = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.SequentialScan);
        if (source.Length > maxBytes) throw new InvalidDataException($"The saved file exceeds the {maxBytes:N0}-byte limit.");
        using var bytes = new MemoryStream((int)Math.Min(source.Length, maxBytes));
        var chunk = new byte[64 * 1024];
        while (true)
        {
            var read = source.Read(chunk, 0, chunk.Length);
            if (read == 0) break;
            if (bytes.Length + read > maxBytes) throw new InvalidDataException($"The saved file exceeds the {maxBytes:N0}-byte limit.");
            bytes.Write(chunk, 0, read);
        }
        if (source.Length > maxBytes) throw new InvalidDataException($"The saved file exceeds the {maxBytes:N0}-byte limit.");
        try
        {
            bytes.Position = 0;
            using var reader = new StreamReader(bytes, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true), detectEncodingFromByteOrderMarks: true);
            return reader.ReadToEnd();
        }
        catch (DecoderFallbackException error)
        {
            throw new InvalidDataException("The saved file is not valid UTF-8 text.", error);
        }
    }

    public static async Task<string> ReadTextAsync(Stream source, int maxBytes, string label, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        if (maxBytes < 1) throw new ArgumentOutOfRangeException(nameof(maxBytes));
        var remainingLength = source.CanSeek ? source.Length - source.Position : 0;
        if (remainingLength < 0) throw new InvalidDataException($"The {label} has an invalid stream position.");
        if (remainingLength > maxBytes) throw new InvalidDataException($"The {label} exceeds the {maxBytes:N0}-byte limit.");

        using var bytes = new MemoryStream((int)remainingLength);
        var chunk = new byte[64 * 1024];
        while (true)
        {
            var remaining = maxBytes - (int)bytes.Length;
            var readSize = remaining < chunk.Length ? remaining + 1 : chunk.Length;
            var read = await source.ReadAsync(chunk.AsMemory(0, readSize), cancellationToken).ConfigureAwait(false);
            if (read == 0) break;
            if (bytes.Length + read > maxBytes)
                throw new InvalidDataException($"The {label} exceeds the {maxBytes:N0}-byte limit.");
            await bytes.WriteAsync(chunk.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }

        try
        {
            bytes.Position = 0;
            using var reader = new StreamReader(bytes, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true), detectEncodingFromByteOrderMarks: true);
            return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DecoderFallbackException error)
        {
            throw new InvalidDataException($"The {label} is not valid UTF-8 text.", error);
        }
    }

    public static string EnsureTextSize(string content, string label, int maxBytes = MaxJsonBytes)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        if (maxBytes < 1) throw new ArgumentOutOfRangeException(nameof(maxBytes));
        if (Encoding.UTF8.GetByteCount(content) > maxBytes)
            throw new InvalidDataException($"The {label} exceeds the {maxBytes:N0}-byte limit.");
        return content;
    }
}
