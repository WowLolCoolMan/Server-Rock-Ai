// One saved conversation plus the project context and mode it was created in.
// Depends on: ChatMessage.
// Used by: ConversationStore / ConversationArchive (persistence) and MainPage recents, folders, and branches.

namespace ServerRock.Core.Models;

/// <summary>A locally stored conversation and the project context active when it was created.</summary>
public sealed class ChatSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "New chat";
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<ChatMessage> Messages { get; set; } = [];
    public string? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string? ProjectInstructions { get; set; }
    public string Mode { get; set; } = WorkspaceModes.Chat;
    public bool IncludeProjectFilesInChat { get; set; }
    public string? FolderName { get; set; }
    /// <summary>Whether this chat is pinned above other chats in its list.</summary>
    public bool Pinned { get; set; }
    /// <summary>Whether this chat was intentionally archived in the source workspace.</summary>
    public bool Archived { get; set; }
    /// <summary>Source chat ID when this conversation was forked.</summary>
    public string? ForkedFrom { get; set; }
    /// <summary>Messages before the currently selected branch.</summary>
    public List<ChatMessage> BaseMessages { get; set; } = [];
    /// <summary>Saved alternate conversation branches.</summary>
    public List<List<ChatMessage>> Branches { get; set; } = [];
    /// <summary>One-based selected branch index; zero means the current main history.</summary>
    public int BranchIndex { get; set; }
    /// <summary>Import origin used to make explicit archive migrations idempotent.</summary>
    public string? ImportSource { get; set; }
    /// <summary>The original conversation identifier in the imported source.</summary>
    public string? ImportSourceId { get; set; }
}
