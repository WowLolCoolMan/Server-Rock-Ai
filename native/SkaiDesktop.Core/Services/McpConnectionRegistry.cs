using SkaiDesktop.Core.Models;

namespace SkaiDesktop.Core.Services;

/// <summary>Process-local MCP connection lifecycle and revocation boundary.</summary>
public sealed class McpConnectionRegistry
{
    private readonly Dictionary<string, McpConnection> _connections = new(StringComparer.Ordinal);
    private readonly object _sync = new();
    private readonly McpConnectionStore? _store;

    public McpConnectionRegistry(McpConnectionStore? store = null)
    {
        _store = store;
        foreach (var connection in store?.Load() ?? []) _connections[connection.ConnectionId] = Copy(connection);
    }

    public McpConnection Configure(string connectionId, string serverId, string transport, string manifestDigest = "")
    {
        ValidateIdentity(connectionId, "connection identity");
        ValidateIdentity(serverId, "server identity");
        if (transport is not ("stdio" or "streamable-http")) throw new ArgumentException("The MCP transport is unsupported.", nameof(transport));
        if (manifestDigest.Length > 128 || manifestDigest.Any(char.IsControl)) throw new ArgumentException("The MCP manifest digest is invalid.", nameof(manifestDigest));
        lock (_sync)
        {
            var connection = new McpConnection { ConnectionId = connectionId, ServerId = serverId, Transport = transport, ManifestDigest = manifestDigest, UpdatedAtUtc = DateTimeOffset.UtcNow };
            _connections[connectionId] = connection;
            _store?.Save(_connections.Values);
            return Copy(connection);
        }
    }

    public McpConnection MarkHealthy(string connectionId)
    {
        return Update(connectionId, McpConnectionStates.Healthy, "");
    }

    public McpConnection MarkDegraded(string connectionId, string error)
    {
        if (string.IsNullOrWhiteSpace(error) || error.Length > 2_000 || error.Any(char.IsControl)) throw new ArgumentException("The MCP health error is invalid.", nameof(error));
        return Update(connectionId, McpConnectionStates.Degraded, error.Trim());
    }

    public McpConnection Disconnect(string connectionId) => Update(connectionId, McpConnectionStates.Disconnected, "");

    public McpConnection Revoke(string connectionId) => Update(connectionId, McpConnectionStates.Revoked, "Connection revoked by the user.");

    public McpConnection Get(string connectionId)
    {
        lock (_sync) return Copy(GetInternal(connectionId));
    }

    public void Authorize(McpToolDescriptor descriptor, McpToolGrant grant)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(grant);
        lock (_sync)
        {
            var connection = GetInternal(grant.ConnectionId);
            if (!string.Equals(connection.ServerId, descriptor.ServerId, StringComparison.Ordinal) || !string.Equals(connection.ConnectionId, grant.ConnectionId, StringComparison.Ordinal)) throw new UnauthorizedAccessException("The MCP connection does not match the requested server.");
            if (connection.State != McpConnectionStates.Healthy) throw new UnauthorizedAccessException($"The MCP connection is {connection.State} and cannot execute tools.");
        }
        McpToolPermissionGate.Authorize(descriptor, grant);
    }

    private McpConnection Update(string connectionId, string state, string error)
    {
        lock (_sync)
        {
            var connection = GetInternal(connectionId);
            if (connection.State == McpConnectionStates.Revoked && state != McpConnectionStates.Revoked) throw new InvalidOperationException("A revoked MCP connection cannot be reactivated.");
            connection.State = state;
            connection.LastError = error;
            connection.UpdatedAtUtc = DateTimeOffset.UtcNow;
            _store?.Save(_connections.Values);
            return Copy(connection);
        }
    }

    private McpConnection GetInternal(string connectionId)
    {
        ValidateIdentity(connectionId, "connection identity");
        return _connections.TryGetValue(connectionId, out var connection) ? connection : throw new KeyNotFoundException("The MCP connection does not exist.");
    }

    private static McpConnection Copy(McpConnection source) => new() { ConnectionId = source.ConnectionId, ServerId = source.ServerId, Transport = source.Transport, State = source.State, ManifestDigest = source.ManifestDigest, LastError = source.LastError, UpdatedAtUtc = source.UpdatedAtUtc };

    private static void ValidateIdentity(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 128 || value.Any(char.IsControl)) throw new ArgumentException($"The {label} is invalid.");
    }
}
