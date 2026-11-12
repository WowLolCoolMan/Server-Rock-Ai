namespace ServerRock.Core.Models;

public sealed record WorkspaceReadEffect(string NodeId, string RelativePath, int CharactersRead, bool Mutated);
