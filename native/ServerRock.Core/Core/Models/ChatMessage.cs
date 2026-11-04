using System.Text.Json.Serialization;

namespace ServerRock.Core.Models;

public sealed record ChatMessage(
    string Role,
    string Content,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] List<ChatAttachment>? Attachments = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] List<ChatSource>? Sources = null)
{
    /// <summary>Creates the provider payload version without local-only UI metadata.</summary>
    public ChatMessage ForModel(string content) => new(Role, content, Attachments?.Where(attachment => attachment.IsImage).ToList());
}
