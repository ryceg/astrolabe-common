using System.Text.Json;
using NJsonSchema;
using NJsonSchema.Generation;

namespace Astrolabe.Schemas.CodeGen;

/// <summary>
/// Utility class to generate JSON Schema for ControlDefinition types.
/// Run this to generate the form.schema.json file.
/// </summary>
public static class GenerateJsonSchema
{
    /// <summary>
    /// Generates a complete JSON Schema for Astrolabe form definitions.
    /// </summary>
    /// <returns>The JSON schema as a string</returns>
    public static string GenerateFormSchema()
    {
        var settings = new SystemTextJsonSchemaGeneratorSettings
        {
            SerializerOptions = ControlDefinitionJson.Options,
            SchemaType = SchemaType.JsonSchema,
            GenerateAbstractSchemas = false,
            FlattenInheritanceHierarchy = true, // Generate all concrete types
            GenerateEnumMappingDescription = true,
            DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull,
            AlwaysAllowAdditionalObjectProperties = false,
            GenerateAbstractProperties = false,
            SchemaNameGenerator = new DefaultSchemaNameGenerator()
        };

        var generator = new JsonSchemaGenerator(settings);

        // Generate the main schema
        var schema = generator.Generate(typeof(JsonFormDefinition));
        schema.Title = "Astrolabe Form Definition";
        schema.Description = "JSON Schema for validating Astrolabe form definition files";
        schema.SchemaVersion = "https://json-schema.org/draft/2020-12/schema";

        // Automatically discover and generate all discriminated union types and their subtypes
        DiscriminatedUnionPostProcessor.GenerateAllSubtypes(schema, generator);

        // Post-process to remove additionalProperties from types with JsonExtensionData
        RemoveExtensionDataPermissiveness(schema);

        // Convert to JSON
        var schemaJson = schema.ToJson();

        // Post-process the JSON to convert discriminated unions
        schemaJson = DiscriminatedUnionJsonProcessor.Process(schemaJson);

        return schemaJson;
    }

    public static async Task Main(string[] args)
    {
        var outputPath =
            args.Length > 0
                ? args[0]
                : Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "..",
                    "schemas",
                    "form.schema.json"
                );

        Console.WriteLine($"Generating JSON Schema for ControlDefinition...");
        Console.WriteLine($"Output path: {outputPath}");

        var schemaJson = GenerateFormSchema();

        // Ensure output directory exists
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Write schema to file
        await File.WriteAllTextAsync(outputPath, schemaJson);

        Console.WriteLine($"JSON Schema generated successfully!");
        Console.WriteLine($"Schema written to: {outputPath}");
    }

    public static void RemoveExtensionDataPermissiveness(JsonSchema schema)
    {
        // Remove additionalProperties that allow anything from the root
        if (
            schema.AdditionalPropertiesSchema != null
            && schema.AdditionalPropertiesSchema.IsAnyType
        )
        {
            schema.AdditionalPropertiesSchema = null;
            schema.AllowAdditionalProperties = false;
        }

        // Process all definitions
        if (schema.Definitions != null)
        {
            foreach (var definition in schema.Definitions.Values)
            {
                RemoveExtensionDataFromSchema(definition);
            }
        }
    }

    private static void RemoveExtensionDataFromSchema(JsonSchema schema)
    {
        // Remove permissive additionalProperties (those with oneOf allowing anything)
        if (schema.AdditionalPropertiesSchema != null)
        {
            var additionalProps = schema.AdditionalPropertiesSchema;
            if (additionalProps.OneOf?.Count > 0 && additionalProps.OneOf.Any(s => s.IsAnyType))
            {
                schema.AdditionalPropertiesSchema = null;
                schema.AllowAdditionalProperties = false;
            }
        }

        // Recursively process nested schemas
        if (schema.Properties != null)
        {
            foreach (var property in schema.Properties.Values)
            {
                RemoveExtensionDataFromSchema(property);
            }
        }

        if (schema.Items != null)
        {
            foreach (var item in schema.Items)
            {
                RemoveExtensionDataFromSchema(item);
            }
        }

        if (schema.OneOf != null)
        {
            foreach (var subSchema in schema.OneOf)
            {
                RemoveExtensionDataFromSchema(subSchema);
            }
        }

        if (schema.AllOf != null)
        {
            foreach (var subSchema in schema.AllOf)
            {
                RemoveExtensionDataFromSchema(subSchema);
            }
        }

        if (schema.AnyOf != null)
        {
            foreach (var subSchema in schema.AnyOf)
            {
                RemoveExtensionDataFromSchema(subSchema);
            }
        }
    }
}

/// <summary>
/// Root structure for form definition JSON files
/// </summary>
public class JsonFormDefinition
{
    [System.Text.Json.Serialization.JsonPropertyName("$schema")]
    public string? Schema { get; set; }

    public IEnumerable<ControlDefinition>? Controls { get; set; }
    public IEnumerable<SchemaField>? Fields { get; set; }

    // Config can be any object - allowing flexibility for form-specific configuration
    [System.ComponentModel.Description("Form-specific configuration object")]
    public Dictionary<string, object?>? Config { get; set; }
}
