using System.Net;

namespace SkaiDesktop.Core.Infrastructure;

/// <summary>Defines the safe transport boundary for model providers.</summary>
public static class ProviderRoutePolicy
{
    public static bool IsRemote(string? endpoint)
    {
        if (!Uri.TryCreate(endpoint?.Trim().TrimEnd('/'), UriKind.Absolute, out var uri) || uri is null)
            return false;
        if (uri.Scheme != Uri.UriSchemeHttps || uri.IsLoopback || string.IsNullOrWhiteSpace(uri.Host))
            return false;
        if (!string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            return false;
        return !IPAddress.TryParse(uri.Host, out var address) || !IPAddress.IsLoopback(address);
    }

    public static bool IsAllowed(string? endpoint) => LocalEndpoint.IsLocal(endpoint ?? string.Empty) || IsRemote(endpoint);

    public static bool IsLocalProvider(string? provider) => provider is not null &&
        (provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase)
         || provider.Equals("LM Studio", StringComparison.OrdinalIgnoreCase)
         || provider.Equals("AirLLM", StringComparison.OrdinalIgnoreCase));
}
