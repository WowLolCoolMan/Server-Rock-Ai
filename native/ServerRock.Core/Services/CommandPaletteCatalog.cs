// UI-independent command-palette metadata and deterministic search.
// Execution stays in the native shell because actions may open dialogs or
// mutate page state; the catalog itself belongs in Core for testability.

namespace ServerRock.Core.Services;

public sealed record CommandPaletteAction(string Id, string Title, string Keywords);

public static class CommandPaletteCatalog
{
    public static readonly CommandPaletteAction[] Actions =
    [
        new("new-chat", "New chat", "new conversation"),
        new("search-conversations", "Search conversations", "find chats coding sessions"),
        new("search-current-chat", "Find in current chat", "search this conversation message"),
        new("review-memory", "Review memory context", "approved local notes injection"),
        new("activity", "Activity & tasks", "operations approvals history"),
        new("models", "Model store", "models downloads benchmark"),
        new("projects", "Projects & folders", "workspace project files"),
        new("chat-workspace", "Chat workspace", "switch mode conversation"),
        new("coding-workspace", "Coding Agent workspace", "switch mode code project"),
        new("quick-open", "Quick open project file", "search files coding agent ctrl p"),
        new("documents-only", "Documents-only mode", "NotebookLM grounded uploaded files attachments"),
        new("github", "GitHub & source control", "repository git push pull"),
        new("images", "Images", "image studio generation"),
        new("skills", "Extensions & skills", "plugins skills marketplace"),
        new("settings", "Connection & settings", "preferences endpoint voice"),
        new("code-review", "Code Review Agent", "review zip repository"),
        new("health", "Health scan", "architecture map diagnostics"),
        new("documentation", "In-app documentation", "help guide privacy safety local workflow"),
        new("snippets", "Prompt snippets", "saved prompts templates expand composer"),
        new("personas", "Personas", "gems reusable roles instructions"),
        new("translate", "Translate text", "language translation review composer"),
        new("quicklinks", "Quicklinks", "bookmarks frequent web links"),
        new("developer-diagnostics", "Developer diagnostics", "developer mode inspect runtime local diagnostics")
    ];

    public static IReadOnlyList<CommandPaletteAction> Search(string? query)
    {
        var text = query?.Trim() ?? string.Empty;
        return Actions
            .Where(action => text.Length == 0
                || action.Title.Contains(text, StringComparison.OrdinalIgnoreCase)
                || action.Keywords.Contains(text, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}
