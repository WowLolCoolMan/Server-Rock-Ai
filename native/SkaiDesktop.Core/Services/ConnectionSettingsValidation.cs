using SkaiDesktop.Core.Infrastructure;

namespace SkaiDesktop.Core.Services;

public sealed record ConnectionSettingsValidation(bool IsValid, string Error, bool IsLocalEndpoint);

public static class ConnectionSettingsValidator
{
    public static ConnectionSettingsValidation Validate(string? endpoint, string? model, bool localOnly)
    {
        var normalizedEndpoint = endpoint?.Trim().TrimEnd('/') ?? string.Empty;
        var normalizedModel = model?.Trim() ?? string.Empty;
        if (!Uri.TryCreate(normalizedEndpoint, UriKind.Absolute, out var uri) ||
            uri is null ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(uri.Host))
            return new(false, "Enter a valid http:// or https:// model endpoint.", false);

        if (string.IsNullOrWhiteSpace(normalizedModel))
            return new(false, "Enter a model ID before saving.", uri.IsLoopback);

        if (localOnly && !LocalEndpoint.IsLocal(normalizedEndpoint))
            return new(false, "Choose a local model endpoint for this route.", false);
        if (!ProviderRoutePolicy.IsAllowed(normalizedEndpoint))
            return new(false, "Cloud routes must use HTTPS without embedded credentials, queries, or URL fragments.", false);

        return new(true, string.Empty, uri.IsLoopback);
    }
}
