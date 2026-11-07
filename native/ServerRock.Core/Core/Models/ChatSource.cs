// One web search result shown under the assistant message that used it.
// Used by: ChatMessage and MainPage (source cards). Never sent back to the model.

namespace ServerRock.Core.Models;

/// <summary>A safe web result shown with the assistant message that used it.</summary>
public sealed record ChatSource(string Title, string Url, string Snippet);
