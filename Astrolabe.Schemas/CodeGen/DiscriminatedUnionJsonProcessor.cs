using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Astrolabe.Annotation;

namespace Astrolabe.Schemas.CodeGen;

/// <summary>
/// Post-processes the generated JSON schema to convert discriminated union base types
/// into oneOf schemas with proper discriminators.
/// </summary>
public static class DiscriminatedUnionJsonProcessor
{
    public static string Process(string schemaJson)
    {
        var jsonDoc = JsonNode.Parse(schemaJson);
        if (jsonDoc is not JsonObject rootObject)
            return schemaJson;

        var definitions = rootObject["definitions"] as JsonObject;
        if (definitions == null)
            return schemaJson;

        var definitionsToReplace = new Dictionary<string, JsonObject>();

        // Find all base types with JsonBaseType attribute
        foreach (var defProp in definitions.ToList())
        {
            var definitionName = defProp.Key;
            var type = FindTypeByName(definitionName);
            if (type == null)
                continue;

            var jsonBaseTypeAttr = type.GetCustomAttribute<JsonBaseTypeAttribute>(inherit: false);
            if (jsonBaseTypeAttr == null)
                continue;

            var subTypeAttrs = type.GetCustomAttributes<JsonSubTypeAttribute>(inherit: false).ToList();
            if (!subTypeAttrs.Any())
                continue;

            // Create a oneOf schema
            var oneOfArray = new JsonArray();
            foreach (var subTypeAttr in subTypeAttrs)
            {
                var subTypeName = subTypeAttr.SubType.Name;
                oneOfArray.Add(new JsonObject
                {
                    ["$ref"] = $"#/definitions/{subTypeName}"
                });
            }

            var unionSchema = new JsonObject
            {
                ["type"] = JsonValue.Create("object"),
                ["discriminator"] = new JsonObject
                {
                    ["propertyName"] = jsonBaseTypeAttr.TypeField
                },
                ["oneOf"] = oneOfArray
            };

            definitionsToReplace[definitionName] = unionSchema;
        }

        // Replace the definitions
        foreach (var kvp in definitionsToReplace)
        {
            definitions[kvp.Key] = kvp.Value;
        }

        // Flatten all nested definitions to top level
        foreach (var def in definitions.ToList())
        {
            if (def.Value is JsonObject defObj && defObj.ContainsKey("definitions"))
            {
                var nestedDefs = defObj["definitions"] as JsonObject;
                if (nestedDefs != null)
                {
                    foreach (var nestedDef in nestedDefs.ToList())
                    {
                        // Add to top-level definitions if not already present
                        if (!definitions.ContainsKey(nestedDef.Key))
                        {
                            definitions[nestedDef.Key] = nestedDef.Value;
                        }
                    }
                }
            }
        }

        // Now replace all nested references
        var allDefNames = definitions.Select(d => d.Key).ToHashSet();

        foreach (var def in definitions.ToList())
        {
            if (def.Value is JsonObject defObj)
            {
                ReplaceNestedSchemas(defObj, allDefNames);
            }
        }

        // Remove nested definitions objects AFTER all replacements are done
        foreach (var def in definitions.ToList())
        {
            if (def.Value is JsonObject defObj && defObj.ContainsKey("definitions"))
            {
                defObj.Remove("definitions");
            }
        }

        var result = jsonDoc.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        System.IO.File.AppendAllText("c:/temp/debug.txt", $"Result contains 'DataControlDefinition/definitions/RenderOptions': {result.Contains("DataControlDefinition/definitions/RenderOptions")}\n");
        return result;
    }

    private static void ReplaceNestedSchemas(JsonObject obj, HashSet<string> allDefinitionNames, string path = "")
    {
        foreach (var prop in obj.ToList())
        {
            var currentPath = path + "/" + prop.Key;
            // Check if this property is a $ref
            if (prop.Key == "$ref" && prop.Value is JsonValue refValue)
            {
                var refPath = refValue.GetValue<string>();
                // Check if it's a reference to a nested definition like #/definitions/DataControlDefinition/definitions/ControlDisableType
                // Pattern: #/definitions/{ParentType}/definitions/{NestedType}
                if (refPath.Contains("/definitions/") && refPath.LastIndexOf("/definitions/") > 0)
                {
                    var lastDefIndex = refPath.LastIndexOf("/definitions/");
                    var typeName = refPath.Substring(lastDefIndex + "/definitions/".Length);
                    if (allDefinitionNames.Contains(typeName))
                    {
                        // Replace with a top-level reference
                        var newRef = $"#/definitions/{typeName}";
                        obj["$ref"] = JsonValue.Create(newRef);
                    }
                }
            }
            else if (prop.Value is JsonObject nestedObj)
            {
                // Check if this is an inline schema for a type that should be a reference
                if (nestedObj.ContainsKey("title") && nestedObj["title"] is JsonValue titleValue)
                {
                    var title = titleValue.GetValue<string>();
                    if (allDefinitionNames.Contains(title))
                    {
                        // Replace with a $ref
                        obj[prop.Key] = new JsonObject
                        {
                            ["$ref"] = $"#/definitions/{title}"
                        };
                        continue;
                    }
                }

                // Recursively process all nested objects
                ReplaceNestedSchemas(nestedObj, allDefinitionNames, currentPath);
            }
            else if (prop.Value is JsonArray array)
            {
                System.IO.File.AppendAllText("c:/temp/debug.txt", $"[{currentPath}] Processing array with {array.Count} items\n");
                for (int i = 0; i < array.Count; i++)
                {
                    if (array[i] is JsonObject arrayObj)
                    {
                        ReplaceNestedSchemas(arrayObj, allDefinitionNames, currentPath + $"[{i}]");
                    }
                }
            }
        }
    }

    private static Type? FindTypeByName(string typeName)
    {
        var assembly = typeof(ControlDefinition).Assembly;
        return assembly.GetType($"Astrolabe.Schemas.{typeName}");
    }
}
