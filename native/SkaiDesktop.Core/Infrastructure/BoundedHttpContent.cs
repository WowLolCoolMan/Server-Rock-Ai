// Reads HTTP response bodies (JSON, text, bytes, SSE lines) with hard size limits.
// Used by: every Core class that calls a remote or local server.

using System.Text;
using System.Text.Json;

namespace SkaiDesktop.Core.Infrastructure;

/// <summary>Reads remote HTTP bodies with explicit byte ceilings before decoding or parsing.</summary>
public static class BoundedHttpContent
{
    public const int MaxJsonBytes = 8 * 1024 * 1024;
    public const int MaxTextBytes = 1 * 1024 * 1024;
    public const int MaxStreamLineCharacters = 256 * 1024;

    public static async Task<JsonDocument> ReadJsonAsync(HttpContent content, CancellationToken cancellationToken = default, string label = "HTTP response")
    {
        var bytes = await ReadBytesAsync(content, MaxJsonBytes, cancellationToken, label).ConfigureAwait(false);
        try { return JsonDocument.Parse(bytes); }
        catch (JsonException exception) { throw new InvalidDataException($"{label} returned invalid JSON.", exception); }
    }

    public static async Task<string> ReadTextAsync(HttpContent content, CancellationToken cancellationToken = default, string label = "HTTP response", int maxBytes = MaxTextBytes)
    {
        var bytes = await ReadBytesAsync(content, maxBytes, cancellationToken, label).ConfigureAwait(false);
        return Encoding.UTF8.GetString(bytes);
    }

    public static async Task<byte[]> ReadBytesAsync(HttpContent content, int maxBytes, CancellationToken cancellationToken = default, string label = "HTTP response")
    {
        ArgumentNullException.ThrowIfNull(content);
        if (maxBytes < 1) throw new ArgumentOutOfRangeException(nameof(maxBytes));
        if (content.Headers.ContentLength is > 0 and var declared && declared > maxBytes)
            throw new InvalidDataException($"{label} exceeds the {maxBytes:N0}-byte limit.");
        await using var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var buffer = new MemoryStream(Math.Min(maxBytes, 64 * 1024));
        var chunk = new byte[64 * 1024];
        var total = 0;
        while (true)
        {
            var read = await stream.ReadAsync(chunk.AsMemory(), cancellationToken).ConfigureAwait(false);
            if (read == 0) break;
            total += read;
            if (total > maxBytes) throw new InvalidDataException($"{label} exceeds the {maxBytes:N0}-byte limit.");
            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }
        return buffer.ToArray();
    }

    public static LimitedLineReader CreateLineReader(StreamReader reader, int maxCharacters = MaxStreamLineCharacters)
    {
        if (maxCharacters < 1) throw new ArgumentOutOfRangeException(nameof(maxCharacters));
        return new LimitedLineReader(reader, maxCharacters);
    }

    public sealed class LimitedLineReader
    {
        private readonly StreamReader _reader;
        private readonly int _maxCharacters;
        private readonly char[] _chunk = new char[4096];
        private int _offset;
        private int _count;

        internal LimitedLineReader(StreamReader reader, int maxCharacters)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _maxCharacters = maxCharacters;
        }

        public async Task<string?> ReadLineAsync(CancellationToken cancellationToken = default)
        {
            var line = new StringBuilder(Math.Min(_maxCharacters, 4096));
            while (true)
            {
                if (_offset >= _count)
                {
                    _count = await _reader.ReadAsync(_chunk.AsMemory(), cancellationToken).ConfigureAwait(false);
                    _offset = 0;
                    if (_count == 0) return line.Length == 0 ? null : line.ToString();
                }

                var start = _offset;
                while (_offset < _count && _chunk[_offset] != '\n') _offset++;
                AppendLimited(line, _chunk, start, _offset - start, _maxCharacters);
                if (_offset < _count)
                {
                    _offset++;
                    if (line.Length > 0 && line[^1] == '\r') line.Length--;
                    return line.ToString();
                }
            }
        }

        private static void AppendLimited(StringBuilder destination, char[] source, int start, int count, int maxCharacters)
        {
            if (destination.Length + count > maxCharacters)
                throw new InvalidDataException($"The HTTP stream line exceeds the {maxCharacters:N0}-character limit.");
            destination.Append(source, start, count);
        }
    }
}
