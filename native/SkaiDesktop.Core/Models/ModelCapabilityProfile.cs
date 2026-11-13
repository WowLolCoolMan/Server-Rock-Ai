namespace SkaiDesktop.Core.Models;

/// <summary>How confidently the native client knows a model capability.</summary>
public enum CapabilitySupport
{
    Unknown,
    Supported,
    Unsupported
}

/// <summary>Bounded, non-secret routing facts for the currently selected model.</summary>
public sealed record ModelCapabilityProfile(
    string Provider,
    string Model,
    string EndpointPrivacy,
    CapabilitySupport Streaming,
    CapabilitySupport Vision,
    string Evidence);
