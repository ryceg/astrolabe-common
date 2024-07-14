using System.Text.Json;
using System.Text.Json.Serialization;

namespace Astrolabe.Validation;

public class ExprJsonConverter : JsonConverter<Expr>
{
    public override Expr? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, Expr value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case ExprValue { Value: DataPath dp }:
                JsonSerializer.Serialize(writer, new { Path = dp.ToPathString() }, options);
                break;
            case ExprValue { Value: var v }:
                JsonSerializer.Serialize(writer, v, options);
                break;
            case DotExpr dotExpr:
                writer.WriteStartArray();
                WritePath(dotExpr);
                writer.WriteEndArray();
                break;
            case VarExpr varExpr:
                JsonSerializer.Serialize(writer, new { Var = varExpr.ToString() }, options);
                break;
            case MapExpr mapExpr:
                JsonSerializer.Serialize(writer, new { Map = mapExpr.Array }, options);
                break;
            case ArrayExpr arrayExpr:
                JsonSerializer.Serialize(writer, new { Array = arrayExpr.ValueExpr }, options);
                break;
            case CallExpr { Function: var f, Args: var a }:
                JsonSerializer.Serialize(writer, new { Call = f.ToString(), Args = a }, options);
                break;
            default:
                throw new NotImplementedException();
        }

        return;

        void WritePath(Expr expr)
        {
            switch (expr)
            {
                case DotExpr { Base: ExprValue { Value: EmptyPath }, Segment: var s }:
                    WritePath(s);
                    break;
                case DotExpr { Base: var b, Segment: var s }:
                    WritePath(b);
                    WritePath(s);
                    break;
                case ExprValue { Value: DataPath dp }:
                    JsonSerializer.Serialize(writer, new { Path = dp.ToPathString() }, options);
                    break;
                default:
                    Write(writer, expr, options);
                    break;
            }
        }
    }
}
