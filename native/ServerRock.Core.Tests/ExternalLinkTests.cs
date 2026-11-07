// Which links in model replies may open, and that a link cannot hide its real host
// (ExternalLinkPolicy).
// Uses TestSupport (Check, StubHandler, …). Run by Program.cs.

using ServerRock.Core.Infrastructure;
using ServerRock.Core.Models;
using ServerRock.Core.Services;
using static TestSupport;

internal static class ExternalLinkTests
{
    public static void Run()
    {
        // Links in model replies open only in the system browser; these checks pin
        // which URLs are allowed and that a link cannot hide its real destination.
        Check(ExternalLinkPolicy.TryGetTarget("https://example.com/docs")?.Host == "example.com" && ExternalLinkPolicy.TryGetTarget("http://localhost:8080") is not null,
            "model reply links allow absolute http and https URLs");
        Check(new[] { "javascript:alert(1)", "file:///etc/passwd", "ms-settings:privacy", "data:text/html,hi", "relative/path", "", null, "https://github.com@evil.example/login" }
                .All(href => ExternalLinkPolicy.TryGetTarget(href) is null),
            "model reply links refuse script, file, app-protocol, relative, empty, and credential-disguised URLs");
        var disguisedTarget = new Uri("https://evil.example/login");
        Check(ExternalLinkPolicy.DisplayLabel("github.com", disguisedTarget) == "github.com (evil.example)" &&
              ExternalLinkPolicy.DisplayLabel("https://github.com/login", disguisedTarget) == "https://github.com/login (evil.example)" &&
              ExternalLinkPolicy.DisplayLabel("github.com", new Uri("https://github.com.evil.example/")) == "github.com (github.com.evil.example)",
            "model reply links show the real host when the visible text names a different site");
        Check(ExternalLinkPolicy.DisplayLabel("https://www.example.com/docs/", new Uri("https://example.com/docs")) == "https://www.example.com/docs/" &&
              ExternalLinkPolicy.DisplayLabel("example.com", new Uri("https://www.example.com")) == "example.com" &&
              ExternalLinkPolicy.DisplayLabel("the docs", new Uri("https://docs.example.com/a")) == "the docs (docs.example.com)" &&
              ExternalLinkPolicy.DisplayLabel("  ", new Uri("https://example.com")) == "example.com",
            "model reply link labels stay unchanged when they already name the destination");
    }
}
