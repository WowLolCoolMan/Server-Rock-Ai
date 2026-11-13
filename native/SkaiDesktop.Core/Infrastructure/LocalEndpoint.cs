// Decides whether a model endpoint runs on this device (loopback/localhost only). The UI uses
// this to label data as "on device" vs "remote" and to decide when to ask before sending
// project files or attachments.
// Depends on: nothing. Used by: MainPage chat, connection status, Model Library, composer labels.

using System.Net;

namespace SkaiDesktop.Core.Infrastructure;

public static class LocalEndpoint
{
    /// <summary>True when the URL's host is loopback or "localhost". A LAN address is not local.</summary>
    public static bool IsLocal(string endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)) return false;
        return uri.IsLoopback || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            (IPAddress.TryParse(uri.Host, out var address) && IPAddress.IsLoopback(address));
    }
}
