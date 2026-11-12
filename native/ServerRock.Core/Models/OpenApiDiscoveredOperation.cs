namespace ServerRock.Core.Models;

/// <summary>
/// A bounded, schema-only description discovered from an OpenAPI document.
/// Discovery never creates a network client or executes the operation.
/// </summary>
public sealed record OpenApiDiscoveredOperation(
    string Name,
    string Method,
    string Path,
    string Description,
    string InputSchemaJson,
    bool IsMutating,
    IReadOnlyList<string> RequiredCapabilities);
