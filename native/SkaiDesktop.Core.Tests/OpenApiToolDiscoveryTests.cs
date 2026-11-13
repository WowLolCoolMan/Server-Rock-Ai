using SkaiDesktop.Core.Models;
using SkaiDesktop.Core.Services;
using static TestSupport;

internal static class OpenApiToolDiscoveryTests
{
    public static void Run()
    {
        const string document = """
        {
          "openapi": "3.0.3",
          "components": { "schemas": { "Note": { "type": "object", "properties": { "text": { "type": "string" } } } } },
          "paths": {
            "/notes/{id}": {
              "get": {
                "operationId": "getNote",
                "parameters": [
                  { "name": "id", "in": "path", "required": true, "schema": { "type": "string" } }
                ]
              }
            },
            "/notes": {
              "post": {
                "summary": "Create a note",
                "requestBody": {
                  "required": true,
                  "content": {
                    "application/json": {
                      "schema": { "$ref": "#/components/schemas/Note" }
                    }
                  }
                }
              }
            }
          }
        }
        """;
        var operations = OpenApiToolDiscovery.Parse(document, "local-test");
        var read = operations.Single(item => item.Name == "getNote");
        var write = operations.Single(item => item.Name == "POST /notes");
        Check(operations.Count == 2 && read.Method == "GET" && !read.IsMutating && read.RequiredCapabilities.Contains(McpCapabilities.InvokeRead),
            "OpenAPI discovery produces bounded read-only operation descriptors");
        Check(write.IsMutating && write.RequiredCapabilities.Contains(McpCapabilities.InvokeWrite) && write.InputSchemaJson.Contains("body", StringComparison.Ordinal) && write.InputSchemaJson.Contains("text", StringComparison.Ordinal),
            "OpenAPI discovery classifies mutating operations and resolves local JSON body schemas");

        try
        {
            OpenApiToolDiscovery.Parse("{\"openapi\":\"2.0\",\"paths\":{}}", "legacy");
            throw new Exception("Expected OpenAPI 2 documents to be rejected.");
        }
        catch (InvalidDataException)
        {
            Check(true, "OpenAPI discovery rejects unsupported document versions");
        }

        try
        {
            OpenApiToolDiscovery.Parse("{\"openapi\":\"3.0.3\",\"paths\":{\"/x\":{\"post\":{}}}}", "https://example.test?token=secret");
            throw new Exception("Expected source identities with query data to be rejected.");
        }
        catch (ArgumentException)
        {
            Check(true, "OpenAPI discovery keeps source identity bounded and credential-free");
        }

        try
        {
            OpenApiToolDiscovery.Parse("{\"openapi\":\"3.0.3\",\"paths\":{\"/x\":{\"post\":{\"requestBody\":{\"content\":{\"application/json\":{\"schema\":{\"$ref\":\"https://example.test/schema.json\"}}}}}}}}}", "local-test");
            throw new Exception("Expected external references to be rejected.");
        }
        catch (InvalidDataException)
        {
            Check(true, "OpenAPI discovery rejects external schema references without network access");
        }
    }
}
