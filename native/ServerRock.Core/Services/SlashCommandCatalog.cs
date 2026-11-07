// Slash commands offered in the composer (/help, /model, /mode, …) and the suggestion filter
// behind the popup: typing "/mo" lists matching commands; typing "/mode " lists that command's
// arguments. Running a command is UI work and stays in MainPage.SlashCommands.cs.
// Depends on: nothing. Used by: MainPage.SlashCommands.cs.

namespace ServerRock.Core.Services;

public static class SlashCommandCatalog
{
    public static readonly (string Name, string Description)[] Commands =
    [
        ("/help", "Show keyboard and workspace help"),
        ("/model", "Open the Model Library to choose, download, or pin models"),
        ("/new", "Start a fresh conversation in this workspace"),
        ("/clear", "Clear this conversation and start again"),
        ("/search", "Search saved chats across both workspaces"),
        ("/folder", "Organize this conversation in a folder"),
        ("/rename", "Rename the current saved chat"),
        ("/delete", "Delete the current saved chat"),
        ("/project", "Open a local project context"),
        ("/reverse-engineer", "Analyze the active project and produce a structured architecture report"),
        ("/health", "Run a free, offline project health scan with bounded evidence"),
        ("/map", "Show a compact local architecture and dependency map"),
        ("/skills", "Browse built-in read-only workflow skills"),
        ("/plugins", "Browse supported plugins, extensions, and skills"),
        ("/research", "Turn on web research for the next Chat prompt"),
        ("/tools", "Open the available local tools menu"),
        ("/status", "Show the current model, workspace, and privacy status"),
        ("/pet", "Open the smart desktop companion"),
        ("/newproject", "Create a named project and choose its folders"),
        ("/folders", "Manage folders for the active project"),
        ("/settings", "Configure the model connection"),
        ("/web", "Turn Chat web search on or off: /web on | /web off"),
        ("/temporary", "Use an unsaved chat: /temporary on | /temporary off"),
        ("/mode", "Switch screens: /mode chat | /mode code")
    ];

    /// <summary>
    /// Suggestions for the popup. With no argument yet, up to 16 commands whose name starts with
    /// <paramref name="commandName"/>; otherwise that command's arguments starting with
    /// <paramref name="argumentPrefix"/>, returned as full "/command value" text.
    /// </summary>
    public static (string Name, string Description)[] Suggest(string commandName, string? argumentPrefix)
    {
        if (argumentPrefix is null)
            return Commands.Where(item => item.Name.StartsWith(commandName, StringComparison.OrdinalIgnoreCase)).Take(16).ToArray();

        (string Value, string Description)[] options = commandName.ToLowerInvariant() switch
        {
            "/mode" => new[] { ("chat", "Switch to the Chat workspace"), ("code", "Switch to Coding Agent") },
            "/web" => new[] { ("on", "Send a search query to DuckDuckGo for Chat answers"), ("off", "Keep Chat prompts off the web search service") },
            "/temporary" => new[] { ("on", "Start a chat excluded from Recents and local history"), ("off", "Clear and end the active temporary chat") },
            _ => []
        };
        return options
            .Where(option => option.Value.StartsWith(argumentPrefix, StringComparison.OrdinalIgnoreCase))
            .Select(option => ($"{commandName} {option.Value}", option.Description))
            .ToArray();
    }
}
