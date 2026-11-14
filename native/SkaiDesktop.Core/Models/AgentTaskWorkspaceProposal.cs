namespace SkaiDesktop.Core.Models;

/// <summary>A bounded file replacement proposed by a local agent task for later review.</summary>
public sealed class AgentTaskWorkspaceChange
{
    public string RelativePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? BaseHash { get; set; }
    public string? AppliedHash { get; set; }
    public string? UndoId { get; set; }
}

/// <summary>A bounded command proposed by a local agent task for later review.</summary>
public sealed class AgentTaskWorkspaceCommand
{
    public string RelativeWorkingDirectory { get; set; } = ".";
    public string Command { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}
