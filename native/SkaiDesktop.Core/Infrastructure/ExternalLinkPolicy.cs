namespace SkaiDesktop.Core.Infrastructure;

/// <summary>
/// Decides which links from model output may be opened, and how they are labelled.
/// Links always open in the user's system browser, never inside the app.
/// </summary>
public static class ExternalLinkPolicy
{
    /// <summary>
    /// Returns the URL to open, or null when the link must stay plain text.
    /// Only absolute http/https URLs with a host are allowed. URLs that carry
    /// credentials (<c>https://github.com@example.net</c>) are refused because
    /// the part before <c>@</c> is a common way to disguise the real site.
    /// </summary>
    public static Uri? TryGetTarget(string? href)
    {
        if (string.IsNullOrWhiteSpace(href)) return null;
        if (!Uri.TryCreate(href.Trim(), UriKind.Absolute, out var target)) return null;
        if (target.Scheme is not ("http" or "https")) return null;
        if (string.IsNullOrEmpty(target.Host) || !string.IsNullOrEmpty(target.UserInfo)) return null;
        return target;
    }

    /// <summary>
    /// Returns the text to show for a link. When the visible text does not
    /// start with the real destination, the destination host is appended,
    /// so a reply cannot show "github.com" while linking somewhere else.
    /// </summary>
    public static string DisplayLabel(string? label, Uri target)
    {
        var host = target.IdnHost;
        var text = (label ?? "").Trim();
        if (text.Length == 0) return host;
        var normalizedLabel = StripPrefixes(text.ToLowerInvariant()).TrimEnd('/');
        var normalizedHost = StripPrefixes(host.ToLowerInvariant());
        var namesHost = normalizedLabel == normalizedHost
            || normalizedLabel.StartsWith(normalizedHost + "/", StringComparison.Ordinal)
            || normalizedLabel.StartsWith(normalizedHost + ":", StringComparison.Ordinal);
        return namesHost ? text : $"{text} ({host})";
    }

    private static string StripPrefixes(string value)
    {
        foreach (var prefix in new[] { "https://", "http://" })
            if (value.StartsWith(prefix, StringComparison.Ordinal)) value = value[prefix.Length..];
        return value.StartsWith("www.", StringComparison.Ordinal) ? value[4..] : value;
    }
}
