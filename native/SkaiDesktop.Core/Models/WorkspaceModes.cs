// The two workspace IDs. They are saved in ChatSession.Mode (conversations.json and chat
// archives), so the string values must never change. Display text ("Chat", "Coding Agent")
// is separate and lives in the UI.
// Used by: ChatSession, ConversationStore, ConversationArchive, and MainPage (_mode).

namespace SkaiDesktop.Core.Models;

public static class WorkspaceModes
{
    public const string Chat = "ChatBot";
    public const string CodingAgent = "Coding Agent";
}
