using System.Text.Json;
using System.Text.Json.Nodes;
using SkaiDesktop.Core.Models;

namespace SkaiDesktop.Core.Services;

/// <summary>
/// Parses OpenAPI 3 documents into reviewable operation descriptors. This is
/// deliberately discovery-only: URLs, authentication, execution, and grants
/// remain outside this parser and must be handled by a future reviewed adapter.
/// </summary>
public static class OpenApiToolDiscovery
{
    public const int MaxDocumentCharacters = 1_000_000;
    public const int MaxOperations = 256;
    private const int MaxSchemaCharacters = 32_000;
    private static readonly string[] Methods = ["get", "head", "options", "post", "put", "patch", "delete"];

    public static IReadOnlyList<OpenApiDiscoveredOperation> Parse(string documentJson, string sourceId)
    {
        ValidateText(sourceId, 128, "OpenAPI source identity");
        if (sourceId.Contains("?", StringComparison.Ordinal) || sourceId.Contains("#", StringComparison.Ordinal) || sourceId.Contains("://", StringComparison.Ordinal))
            throw new ArgumentException("The OpenAPI source identity must not contain URL query, fragment, or scheme data.", nameof(sourceId));
        if (string.IsNullOrWhiteSpace(documentJson) || documentJson.Length > MaxDocumentCharacters || HasUnsafeControl(documentJson))
            throw new InvalidDataException("The OpenAPI document is empty, oversized, or contains control characters.");

        JsonDocument document;
        try { document = JsonDocument.Parse(documentJson, new JsonDocumentOptions { MaxDepth = 32 }); }
        catch (JsonException exception) { throw new InvalidDataException("The OpenAPI document is not valid JSON.", exception); }
        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("openapi", out var version) ||
                version.ValueKind != JsonValueKind.String || !(version.GetString() ?? "").StartsWith("3.", StringComparison.Ordinal))
                throw new InvalidDataException("Only OpenAPI 3.x documents are supported for schema discovery.");
            if (!root.TryGetProperty("paths", out var paths) || paths.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("The OpenAPI document does not contain a paths object.");

            var operations = new List<OpenApiDiscoveredOperation>();
            foreach (var path in paths.EnumerateObject())
            {
                ValidateText(path.Name, 512, "OpenAPI path");
                if (!path.Name.StartsWith("/", StringComparison.Ordinal) || path.Value.ValueKind != JsonValueKind.Object) continue;
                foreach (var operation in path.Value.EnumerateObject())
                {
                    var method = operation.Name.ToLowerInvariant();
                    if (!Methods.Contains(method, StringComparer.Ordinal) || operation.Value.ValueKind != JsonValueKind.Object) continue;
                    if (operations.Count >= MaxOperations) throw new InvalidDataException($"The OpenAPI document contains more than {MaxOperations} operations.");
                    var operationId = operation.Value.TryGetProperty("operationId", out var id) && id.ValueKind == JsonValueKind.String
                        ? id.GetString()
                        : null;
                    var name = string.IsNullOrWhiteSpace(operationId) ? method.ToUpperInvariant() + " " + path.Name : operationId!;
                    ValidateText(name, 128, "OpenAPI operation name");
                    var description = ReadDescription(operation.Value);
                    var schema = BuildInputSchema(operation.Value, root);
                    var mutating = method is "post" or "put" or "patch" or "delete";
                    operations.Add(new OpenApiDiscoveredOperation(name, method.ToUpperInvariant(), path.Name, description, schema,
                        mutating,
                        [mutating ? McpCapabilities.InvokeWrite : McpCapabilities.InvokeRead]));
                }
            }
            return operations;
        }
    }

    private static string ReadDescription(JsonElement operation)
    {
        foreach (var property in new[] { "summary", "description" })
            if (operation.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String)
            {
                var text = value.GetString()?.Trim() ?? "";
                if (text.Length > 4_000 || text.Any(char.IsControl)) throw new InvalidDataException("An OpenAPI operation description is invalid.");
                if (text.Length > 0) return text;
            }
        return "Discovered OpenAPI operation; review its schema and permissions before use.";
    }

    private static string BuildInputSchema(JsonElement operation, JsonElement root)
    {
        var parameterProperties = new JsonObject();
        var required = new JsonArray();
        if (operation.TryGetProperty("parameters", out var parameters) && parameters.ValueKind == JsonValueKind.Array)
            foreach (var parameter in parameters.EnumerateArray())
            {
                if (parameter.ValueKind != JsonValueKind.Object || !parameter.TryGetProperty("name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String) continue;
                var name = nameElement.GetString() ?? "";
                if (name.Length is 0 or > 128 || name.Any(char.IsControl)) continue;
                var schema = parameter.TryGetProperty("schema", out var schemaElement) && schemaElement.ValueKind == JsonValueKind.Object
                    ? ResolveLocalSchema(schemaElement, root)
                    : new JsonObject { ["type"] = "string" };
                parameterProperties[name] = schema;
                if (parameter.TryGetProperty("required", out var requiredElement) && requiredElement.ValueKind == JsonValueKind.True) required.Add(name);
            }

        if (operation.TryGetProperty("requestBody", out var requestBody) && requestBody.ValueKind == JsonValueKind.Object &&
            requestBody.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Object &&
            content.TryGetProperty("application/json", out var jsonContent) && jsonContent.ValueKind == JsonValueKind.Object &&
            jsonContent.TryGetProperty("schema", out var bodySchema) && bodySchema.ValueKind == JsonValueKind.Object)
        {
            var body = ResolveLocalSchema(bodySchema, root);
            parameterProperties["body"] = body;
            if (requestBody.TryGetProperty("required", out var bodyRequired) && bodyRequired.ValueKind == JsonValueKind.True) required.Add("body");
        }

        var result = new JsonObject { ["type"] = "object", ["properties"] = parameterProperties };
        if (required.Count > 0) result["required"] = required;
        var json = result.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        if (json.Length > MaxSchemaCharacters) throw new InvalidDataException("The discovered OpenAPI input schema is oversized.");
        return json;
    }

    private static JsonNode? ResolveLocalSchema(JsonElement schema, JsonElement root, int depth = 0)
    {
        if (depth > 8) throw new InvalidDataException("The OpenAPI schema reference depth exceeds the bounded limit.");
        if (schema.TryGetProperty("$ref", out var reference) && reference.ValueKind == JsonValueKind.String)
        {
            var value = reference.GetString() ?? "";
            const string prefix = "#/components/schemas/";
            if (!value.StartsWith(prefix, StringComparison.Ordinal))
                throw new InvalidDataException("Only local OpenAPI component schema references are supported.");
            var name = value[prefix.Length..];
            if (name.Length is 0 or > 128 || name.Any(char.IsControl) || name.Contains('/', StringComparison.Ordinal))
                throw new InvalidDataException("The OpenAPI component schema reference is invalid.");
            if (!root.TryGetProperty("components", out var components) || components.ValueKind != JsonValueKind.Object ||
                !components.TryGetProperty("schemas", out var schemas) || schemas.ValueKind != JsonValueKind.Object ||
                !schemas.TryGetProperty(name, out var target) || target.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("The OpenAPI component schema reference could not be resolved locally.");
            return ResolveLocalSchema(target, root, depth + 1);
        }
        return JsonNode.Parse(schema.GetRawText());
    }

    private static void ValidateText(string value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength || HasUnsafeControl(value))
            throw new ArgumentException($"The {label} is invalid.");
    }

    private static bool HasUnsafeControl(string value) => value.Any(character => char.IsControl(character) && character is not '\r' and not '\n' and not '\t');
}
