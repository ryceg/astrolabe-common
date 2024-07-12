using System.Text.Json;
using System.Text.Json.Nodes;

namespace Astrolabe.Validation;

public class JsonDataLookup
{
    public static Func<DataPath, ExprValue> FromObject(JsonNode? data)
    {
        Dictionary<DataPath, JsonNode?> cache = new();

        return path => ToValue(GetNode(path), path);

        JsonNode? GetNode(DataPath dp)
        {
            if (cache.TryGetValue(dp, out var v))
                return v;
            var res = dp switch
            {
                EmptyPath => data,
                FieldPath fp => DoField(fp),
                IndexPath ip => DoIndex(ip)
            };
            cache[dp] = res;
            return res;

            JsonNode? DoField(FieldPath fp)
            {
                var parentNode = GetNode(fp.Parent);
                return parentNode == null ? parentNode : parentNode.AsObject()[fp.Field];
            }
            JsonNode? DoIndex(IndexPath ip)
            {
                var parentNode = GetNode(ip.Parent);
                return parentNode == null ? parentNode : parentNode.AsArray()[ip.Index];
            }
        }
    }

    private static ExprValue ToValue(JsonNode? node, DataPath from)
    {
        return node switch
        {
            null => ExprValue.Null.WithPath(from),
            JsonArray ja => ja.Select((v, i) => ToValue(v, new IndexPath(i, from))).ToExpr(from),
            JsonObject jo => jo.ToExpr(from),
            JsonValue v
                => v.GetValue<object>() switch
                {
                    JsonElement e
                        => e.ValueKind switch
                        {
                            JsonValueKind.False => false.ToExpr(from),
                            JsonValueKind.True => true.ToExpr(from),
                            JsonValueKind.String => e.GetString().ToExpr(from),
                            JsonValueKind.Number
                                => e.TryGetInt64(out var l)
                                    ? l.ToExpr(from)
                                    : e.TryGetDouble(out var d)
                                        ? d.ToExpr(from)
                                        : ExprValue.Null.WithPath(from),
                            _ => throw new ArgumentOutOfRangeException($"{e.ValueKind}-{e}")
                        },
                    var objValue => objValue.ToExpr(from)
                },
        };
    }
}
