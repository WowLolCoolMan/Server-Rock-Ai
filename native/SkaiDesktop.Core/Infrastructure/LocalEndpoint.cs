// Decides whether a model endpoint runs on this device (loopback/localhost only). The UI uses
// this to label data as "on device" and to decide whether local-only work may proceed.
// Depends on: nothing. Used by: MainPage chat, connection status, Model Library, composer labels.

using System.Net;

namespace SkaiDesktop.Core.Infrastructure;

public static class LocalEndpoint
{
    /// <summary>True only for an HTTP(S) loopback URL without embedded credentials or a fragment.</summary>
    public static bool IsLocal(string endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme is not ("http" or "https")
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Fragment)) return false;
        return uri.IsLoopback || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            (IPAddress.TryParse(uri.Host, out var address) && IPAddress.IsLoopback(address));
    }
}
