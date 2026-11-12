namespace ServerRock.Core.Services;

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

        if (localOnly && !uri.IsLoopback)
            return new(false, "Local-only mode requires a localhost or loopback endpoint.", false);

        return new(true, string.Empty, uri.IsLoopback);
    }
}
