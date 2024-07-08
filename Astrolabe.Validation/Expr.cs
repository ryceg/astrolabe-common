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

public interface Value : Expr;

public record NullValue : Value
{
    public static readonly NullValue Instance = new NullValue();
}

public record BoolValue(bool Value) : Value
{
    public static bool operator true(BoolValue bv)
    {
        return bv.Value;
    }

    public static bool operator false(BoolValue bv)
    {
        return !bv.Value;
    }
    
    public static BoolValue operator &(BoolValue bv1, BoolValue bv2)
    {
        return new BoolValue(bv1.Value && bv2.Value);
    }

    public static BoolValue operator |(BoolValue bv1, BoolValue bv2)
    {
        return new BoolValue(bv1.Value || bv2.Value);
    }

    public override string ToString()
    {
        return Value ? "true" : "false";
    }
}

public record LongValue(long Value) : Value
{
    public override string ToString()
    {
        return Value.ToString();
    }
}

public record DoubleValue(double Value) : Value
{
    public override string ToString()
    {
        return Value.ToString();
    }
}

public record StringValue(string Value) : Value
{
    public override string ToString()
    {
        return Value;
    }
}

public record CallExpr(InbuiltFunction Function, ICollection<Expr> Args) : Expr
{
    public override string ToString()
    {
        return $"{Function}({string.Join(", ", Args)})";
    }
}

public record GetData(PathExpr Path) : Expr;

public record ConstraintExpr(PathExpr Path) : Expr;

public record DefineConstraintExpr(PathExpr Path, Expr Applies, Expr Lower, Expr Upper, Expr LowerExclusive, Expr UpperExclusive) : Expr;

public record IndexExpr : Expr;

public record ValidationRange(
    double? Min,
    double? Max,
    bool MinExclusive = false,
    bool MaxExclusive = false,
    string? MinErrorMessage = null,
    string? MaxErrorMessage = null
) : Value;

public static class ValueExtensions
{
    public static BoolValue ToExpr(this bool b)
    {
        return new BoolValue(b);
    }
    
    public static LongValue ToExpr(this long b)
    {
        return new LongValue(b);
    }
    
    public static LongValue ToExpr(this int b)
    {
        return new LongValue(b);
    }
    
    public static DoubleValue ToExpr(this double b)
    {
        return new DoubleValue(b);
    }

    public static StringValue ToExpr(this string b)
    {
        return new StringValue(b);
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
