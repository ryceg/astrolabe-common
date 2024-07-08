using System.Text.Json.Nodes;

namespace Astrolabe.Validation;

public enum InbuiltFunction
{
    Eq,
    Lt,
    LtEq,
    Gt,
    GtEq,
    Ne,
    And,
    Or,
    Not,
    Add,
    Minus,
    Multiply,
    Divide
}


public interface Expr;

public interface ExprValue : Expr
{
    public static readonly NullValue Null = new();
}

public record NullValue : ExprValue;

public record BoolValue(bool Value) : ExprValue;

public record NumberValue(long? LongValue, double? DoubleValue) : ExprValue
{
    public double AsDouble()
    {
        return DoubleValue ?? LongValue!.Value;
    }
}

public record StringValue(string Value) : ExprValue;

public record JsonValueValue(JsonValue Value) : ExprValue;


public record CallExpr(InbuiltFunction Function, ICollection<Expr> Args) : Expr
{
    public override string ToString()
    {
        return $"{Function}({string.Join(", ", Args)})";
    }
}

public record GetData(PathExpr Path) : Expr;

public class IndexExpr : Expr;

public static class ValueExtensions
{
    public static ExprValue ToExpr(this bool b)
    {
        return new BoolValue(b);
    }
    
    public static ExprValue ToExpr(this long l)
    {
        return new NumberValue(null, l);
    }
    
    public static ExprValue ToExpr(this int i)
    {
        return new NumberValue(i, null);
    }
    
    public static ExprValue ToExpr(this double d)
    {
        return new NumberValue(null, d);
    }

    public static ExprValue ToExpr(this string s)
    {
        return new StringValue(s);
    }

    public static bool AsBool(this ExprValue v)
    {
        return ((BoolValue)v).Value;
    }
}
public record PathExpr(Expr Segment, PathExpr? Parent)
{
    public override string ToString()
    {
        return Parent != null ? $"{Parent}.{Segment}" : Segment.ToString()!;
    }

    public PathExpr Index(int number)
    {
        return new PathExpr(number.ToExpr(), this);
    }

    public static implicit operator PathExpr(string path)
    {
        return new PathExpr(path.ToExpr(), null);
    }
    
    public static PathExpr IndexPath(int index, PathExpr? parent)
    {
        return new PathExpr(index.ToExpr(), parent);
    }
}
