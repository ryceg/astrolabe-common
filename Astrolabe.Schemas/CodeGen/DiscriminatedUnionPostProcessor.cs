using System.Reflection;
using Astrolabe.Annotation;
using NJsonSchema;
using NJsonSchema.Generation;

namespace Astrolabe.Schemas.CodeGen;

/// <summary>
/// Post-processes a generated schema to replace base types marked with [JsonBaseType]
/// with oneOf discriminated unions of their subtypes.
/// </summary>
public static class DiscriminatedUnionPostProcessor
{
    /// <summary>
    /// Automatically discovers all types with [JsonBaseType] in the assembly and generates
    /// their subtypes, adding them to the root schema.
    /// </summary>
    public static void GenerateAllSubtypes(JsonSchema rootSchema, JsonSchemaGenerator generator)
    {
        var assembly = typeof(ControlDefinition).Assembly;

        // Find all types with [JsonBaseType] attribute
        var baseTypes = assembly
            .GetTypes()
            .Where(t => t.GetCustomAttribute<JsonBaseTypeAttribute>(inherit: false) != null)
            .ToList();

        foreach (var baseType in baseTypes)
        {
            // Get all subtypes from [JsonSubType] attributes
            var subTypeAttrs = baseType
                .GetCustomAttributes<JsonSubTypeAttribute>(inherit: false)
                .ToList();

            foreach (var subTypeAttr in subTypeAttrs)
            {
                var subType = subTypeAttr.SubType;
                var subTypeName = subType.Name;

                // Generate the subtype schema if not already present
                if (!rootSchema.Definitions.ContainsKey(subTypeName))
                {
                    var subTypeSchema = generator.Generate(subType);
                    rootSchema.Definitions[subTypeName] = subTypeSchema;

                    // Also copy any nested definitions
                    if (subTypeSchema.Definitions != null)
                    {
                        foreach (var def in subTypeSchema.Definitions)
                        {
                            if (!rootSchema.Definitions.ContainsKey(def.Key))
                            {
                                rootSchema.Definitions[def.Key] = def.Value;
                            }
                        }
                    }
                }
            }
        }
    }

    public static void Process(
        JsonSchema rootSchema,
        SystemTextJsonSchemaGeneratorSettings settings
    )
    {
        var definitionsToReplace = new Dictionary<string, JsonSchema>();

        // Find all definitions that have JsonBaseType attribute
        foreach (var kvp in rootSchema.Definitions.ToList())
        {
            var definitionName = kvp.Key;
            var definitionSchema = kvp.Value;

            // Try to find the type for this definition
            var type = FindTypeByName(definitionName);
            if (type == null)
                continue;

            var jsonBaseTypeAttr = type.GetCustomAttribute<JsonBaseTypeAttribute>();
            if (jsonBaseTypeAttr == null)
                continue;

            // Get all subtypes
            var subTypeAttrs = type.GetCustomAttributes<JsonSubTypeAttribute>().ToList();
            if (!subTypeAttrs.Any())
                continue;

            // Create a new schema with oneOf
            var unionSchema = new JsonSchema
            {
                Type = JsonObjectType.None,
                Description = $"{definitionName} (discriminated by '{jsonBaseTypeAttr.TypeField}')",
            };

            // Add discriminator with mapping
            var discriminator = new OpenApiDiscriminator
            {
                PropertyName = jsonBaseTypeAttr.TypeField,
            };

            unionSchema.DiscriminatorObject = discriminator;

            // Add references to each subtype (they should already be in definitions)
            foreach (var subTypeAttr in subTypeAttrs)
            {
                var subType = subTypeAttr.SubType;
                var subTypeName = subType.Name;
                var discriminatorValue = subTypeAttr.Discriminator;

                // The subtype should already exist in definitions (NJsonSchema generates all referenced types)
                if (!rootSchema.Definitions.ContainsKey(subTypeName))
                {
                    // If not found, skip (shouldn't happen)
                    continue;
                }

                // Get the actual schema from definitions
                var subTypeSchema = rootSchema.Definitions[subTypeName];

                // Create a reference to it
                var refSchema = new JsonSchema { Reference = subTypeSchema };

                unionSchema.OneOf.Add(refSchema);

                // Add discriminator mapping (NJsonSchema will serialize the JsonSchema reference to a path string)
                discriminator.Mapping[discriminatorValue] = subTypeSchema;
            }

            // Mark for replacement
            definitionsToReplace[definitionName] = unionSchema;
        }

        // Replace the definitions
        foreach (var kvp in definitionsToReplace)
        {
            rootSchema.Definitions[kvp.Key] = kvp.Value;
        }
    }

    private static Type? FindTypeByName(string typeName)
    {
        // Search in Astrolabe.Schemas assembly
        var assembly = typeof(ControlDefinition).Assembly;
        return assembly.GetType($"Astrolabe.Schemas.{typeName}");
    }
}
