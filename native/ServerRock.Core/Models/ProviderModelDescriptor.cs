namespace ServerRock.Core.Models;

/// <summary>Bounded provider-advertised metadata for one model.</summary>
public sealed record ProviderModelDescriptor(
    string Model,
    CapabilitySupport Vision,
    IReadOnlyList<string> Capabilities);
