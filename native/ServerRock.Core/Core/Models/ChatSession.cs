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
    public string Mode { get; set; } = "ChatBot";
    public bool IncludeProjectFilesInChat { get; set; }
    public string? FolderName { get; set; }
    /// <summary>Import origin used to make explicit archive migrations idempotent.</summary>
    public string? ImportSource { get; set; }
    /// <summary>The original conversation identifier in the imported source.</summary>
    public string? ImportSourceId { get; set; }
}
