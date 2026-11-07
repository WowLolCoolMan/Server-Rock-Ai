// DuckDuckGo HTML result parsing (WebSearchProvider).
// Uses TestSupport (Check, StubHandler, …). Run by Program.cs.

using System.Net;
using ServerRock.Core.Infrastructure;
using ServerRock.Core.Models;
using ServerRock.Core.Services;
using static TestSupport;

internal static class WebSearchTests
{
    public static async Task RunAsync()
    {
        var searchProvider = new WebSearchProvider(new HttpClient(new StubHandler((request, _) =>
        {
            Check(request.RequestUri?.Host == "html.duckduckgo.com", "opt-in web search uses the configured public search endpoint");
            var html = "<a class=\"result__a\" href=\"//duckduckgo.com/l/?uddg=https%3A%2F%2Fexample.org%2Fdocs&amp;rut=abc\">Local &amp; private</a><a class=\"result__snippet\">Read <b>official</b> docs.</a>";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(html) });
        })));
        var searchResults = await searchProvider.SearchAsync("Uno Linux");
        Check(searchResults.Count == 1 && searchResults[0].Title == "Local & private", "web search decodes and extracts result titles");
        Check(searchResults[0].Url == "https://example.org/docs" && searchResults[0].Snippet == "Read official docs.", "web search returns safe HTTP source URLs and clean snippets");
    }
}
