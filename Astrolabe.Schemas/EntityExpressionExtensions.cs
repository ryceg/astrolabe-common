using System.Text.Json;
using System.Text.Json.Nodes;
using Astrolabe.JSON;
using Jsonata.Net.Native;
using Jsonata.Net.Native.Json;
using Jsonata.Net.Native.SystemTextJson;

namespace Astrolabe.Schemas;

public delegate bool ExprEvalBool(EntityExpression expression, JsonObject data, JsonPathSegments context);

public static class EntityExpressionExtensions
{
    
    public static bool DefaultEvalBool(this EntityExpression expression, JsonObject data, JsonPathSegments context)
    {
        return expression switch
        {
            DataMatchExpression expr => NodeEquals(context.Field(expr.Field).Traverse(data), expr.Value),
            JsonataExpression expr => RunJsonata(expr.Expression),
            _ => throw new ArgumentOutOfRangeException(nameof(expression), expression, null)
        };
        
        bool RunJsonata(string expr)
        {
            var result = new JsonataQuery(expr).Eval(
                JsonataExtensions.FromSystemTextJson(JsonDocument.Parse(data.ToJsonString())));
            if (result.Type == JTokenType.Boolean)
            {
                return (bool)result;
            }
            return false;
        }
    }

    private static bool NodeEquals(JsonNode? node, object? value)
    {
        if (node == null)
            return value == null;
        return (node, value) switch
        {
            (JsonArray a, JsonElement e) => a.Contains(JsonValue.Create(e)),
            ({} n, JsonElement e) => JsonNode.DeepEquals(n.AsValue(), JsonValue.Create(e)),
            _ => throw new NotImplementedException()
        };
    }
}