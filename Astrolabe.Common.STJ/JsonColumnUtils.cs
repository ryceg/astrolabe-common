using System.Text.Json;
using System.Text.Json.Nodes;

namespace Astrolabe.Common.STJ;

public static class JsonColumnUtils
{
    public static JsonObject ParseJson(string? strValue)
    {
        return string.IsNullOrEmpty(strValue) ? new JsonObject() : (JsonObject) JsonNode.Parse(strValue);
    }
    
    public static IDictionary<string, object?>? ParseJsonElement(string? strValue)
    {
        return string.IsNullOrEmpty(strValue) ? null : JsonDocument.Parse(strValue).Deserialize<IDictionary<string, object?>>();
    }
}