namespace ServerRock.Core.Models;

/// <summary>A safe web result shown with the assistant message that used it.</summary>
public sealed record ChatSource(string Title, string Url, string Snippet);
