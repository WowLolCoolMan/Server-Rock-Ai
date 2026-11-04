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
