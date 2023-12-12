using System.Text.Json;
using System.Text.Json.Nodes;
using Astrolabe.JSON;

namespace Astrolabe.Schemas;

public static class EntityExpressionExtensions
{
    public static bool EvalBool(this EntityExpression expression, JsonObject data, JsonPathSegments context)
    {
        return expression switch
        {
            FieldValueExpression expr => NodeEquals(context.Field(expr.Field).Traverse(data), expr.Value),
            _ => throw new NotImplementedException()
        };
        
    }

    private static bool NodeEquals(JsonNode? node, object? value)
    {
        if (node == null)
            return value == null;
        return (node, value) switch
        {
            (JsonArray a, JsonElement e) => a.Contains(JsonValue.Create(e)),
            ({} n, JsonElement e) => n.AsValue() == JsonValue.Create(e),
            _ => throw new NotImplementedException()
        };
    }
}