// One text file, extracted PDF text, or image attached to a chat message.
// Depends on: nothing. Used by: ChatMessage, ChatAttachmentPolicy, the provider, and MainPage attachments.

namespace SkaiDesktop.Core.Models;

using System.Text.Json.Serialization;

/// <summary>A bounded text file or image explicitly attached to one conversation message.</summary>
public sealed record ChatAttachment(string FileName, string Content, string? MediaType = null)
{
    [JsonIgnore]
    public bool IsImage => MediaType is not null;
}
