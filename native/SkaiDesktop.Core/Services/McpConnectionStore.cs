using System.Text.Json;
using SkaiDesktop.Core.Infrastructure;
using SkaiDesktop.Core.Models;

namespace SkaiDesktop.Core.Services;

/// <summary>Atomic, credential-free persistence for reviewed MCP connection metadata.</summary>
public sealed class McpConnectionStore
{
    public const int MaxConnections = 64;
    private const int MaxSerializedCharacters = 512 * 1024;
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true, MaxDepth = 8 };
    private readonly string _path;

    public McpConnectionStore(string? path = null)
    {
        _path = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ServerRock", "AI", "mcp-connections.json");
    }

    public IReadOnlyList<McpConnection> Load()
    {
        if (!File.Exists(_path)) return [];
        try
        {
            var connections = JsonSerializer.Deserialize<List<McpConnection>>(BoundedFileReader.ReadText(_path), Options) ?? [];
            Validate(connections);
            return connections.Select(Copy).ToArray();
        }
        catch (JsonException exception) { throw new InvalidDataException("MCP connection metadata could not be read. The original file was left untouched.", exception); }
    }

    public void Save(IEnumerable<McpConnection> connections)
    {
        var list = connections.Select(Copy).ToList();
        Validate(list);
        var json = JsonSerializer.Serialize(list, Options);
        if (json.Length > MaxSerializedCharacters) throw new InvalidDataException("MCP connection metadata exceeds the bounded size limit.");
        AtomicFileWriter.WriteText(_path, json);
    }

    private static void Validate(IReadOnlyList<McpConnection> connections)
    {
        if (connections.Count > MaxConnections || connections.Select(item => item.ConnectionId).Distinct(StringComparer.Ordinal).Count() != connections.Count) throw new InvalidDataException("MCP connection metadata contains too many or duplicate connections.");
        foreach (var connection in connections)
        {
            ValidateText(connection.ConnectionId, 128, "connection identity");
            ValidateText(connection.ServerId, 128, "server identity");
            if (connection.Transport is not ("stdio" or "streamable-http")) throw new InvalidDataException("MCP connection metadata contains an unsupported transport.");
            if (connection.State is not (McpConnectionStates.Configured or McpConnectionStates.Healthy or McpConnectionStates.Degraded or McpConnectionStates.Disconnected or McpConnectionStates.Revoked)) throw new InvalidDataException("MCP connection metadata contains an unsupported state.");
            ValidateText(connection.ManifestDigest, 128, "manifest digest", allowEmpty: true);
            ValidateText(connection.LastError, 2_000, "last error", allowEmpty: true);
        }
    }

    private static void ValidateText(string value, int maxLength, string label, bool allowEmpty = false)
    {
        if ((!allowEmpty && string.IsNullOrWhiteSpace(value)) || value.Length > maxLength || value.Any(char.IsControl)) throw new InvalidDataException($"The {label} is invalid.");
    }

    private static McpConnection Copy(McpConnection source) => new() { ConnectionId = source.ConnectionId, ServerId = source.ServerId, Transport = source.Transport, State = source.State, ManifestDigest = source.ManifestDigest, LastError = source.LastError, UpdatedAtUtc = source.UpdatedAtUtc };
}
