// Opt-in web search: fetches DuckDuckGo's HTML results page and extracts a few
// title/URL/snippet results. Does not open the result pages.
// Depends on: HttpClient, BoundedHttpContent. Used by: MainPage chat sending when web search is on.

using System.Net;
using System.Text.RegularExpressions;
using SkaiDesktop.Core.Infrastructure;
using SkaiDesktop.Core.Models;

namespace SkaiDesktop.Core.Services;

public sealed record WebSearchResult(string Title, string Url, string Snippet);

/// <summary>Small opt-in web search adapter using DuckDuckGo's public HTML search results.</summary>
public sealed class WebSearchProvider(HttpClient? httpClient = null)
{
    private static readonly HttpClient Shared = new() { Timeout = Timeout.InfiniteTimeSpan };
    private readonly HttpClient _http = httpClient ?? Shared;
    private static readonly Regex ResultRegex = new(
        "<a[^>]*class=\"result__a\"[^>]*href=\"(?<url>[^\"]+)\"[^>]*>(?<title>.*?)</a>.*?<a[^>]*class=\"result__snippet\"[^>]*>(?<snippet>.*?)</a>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex Tags = new("<[^>]+>", RegexOptions.Compiled);

    public async Task<IReadOnlyList<WebSearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query)) throw new ArgumentException("Enter a search query.", nameof(query));
        if (query.Length > 500) throw new ArgumentException("Search queries are limited to 500 characters.", nameof(query));
        var uri = new Uri("https://html.duckduckgo.com/html/?q=" + Uri.EscapeDataString(query));
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.UserAgent.ParseAdd(ProductIdentity.UserAgent("desktop web search"));
        using var response = await _http.SendAsync(request, timeout.Token);
        response.EnsureSuccessStatusCode();
        var html = await BoundedHttpContent.ReadTextAsync(response.Content, timeout.Token, "Web search response");
        var results = new List<WebSearchResult>();
        foreach (Match match in ResultRegex.Matches(html))
        {
            var href = WebUtility.HtmlDecode(match.Groups["url"].Value);
            if (Uri.TryCreate(href, UriKind.RelativeOrAbsolute, out var resultUri) && !resultUri.IsAbsoluteUri && href.StartsWith("//duckduckgo.com/l/?", StringComparison.Ordinal))
            {
                var queryPart = new Uri("https:" + href).Query.TrimStart('?').Split('&')
                    .FirstOrDefault(part => part.StartsWith("uddg=", StringComparison.OrdinalIgnoreCase));
                if (queryPart is not null) href = Uri.UnescapeDataString(queryPart[5..]);
            }
            if (!Uri.TryCreate(href, UriKind.Absolute, out var absolute) || absolute.Scheme is not ("http" or "https")) continue;
            var title = Clean(match.Groups["title"].Value, 200);
            var snippet = Clean(match.Groups["snippet"].Value, 700);
            if (title.Length == 0 || snippet.Length == 0) continue;
            results.Add(new WebSearchResult(title, absolute.AbsoluteUri, snippet));
            if (results.Count == 6) break;
        }
        return results;
    }

    private static string Clean(string html, int maxLength)
    {
        var text = WebUtility.HtmlDecode(Tags.Replace(html, ""));
        text = Regex.Replace(text, "\\s+", " ").Trim();
        return text.Length <= maxLength ? text : text[..maxLength] + "…";
    }
}
