using System.Text.Json;
using System.Text.Json.Nodes;
using Astrolabe.JSON.Extensions;

namespace Astrolabe.Schemas;

public static class ControlDefinitionJson
{
    public static readonly JsonSerializerOptions Options = new JsonSerializerOptions().AddStandardOptions();

    public static IEnumerable<ControlDefinition> FromArray(JsonArray array)
    {
        return array.Deserialize<IEnumerable<ControlDefinition>>(Options)!;
    }
}