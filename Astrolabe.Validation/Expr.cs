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

}

public record LongValue(long Value) : Value;

public record DoubleValue(double Value) : Value;

public record StringValue(string Value) : Value;
public record CallExpr(InbuiltFunction Function, ICollection<Expr> Args) : Expr;

public record GetData(PathExpr Path, bool Config = false) : Expr;

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
    public static implicit operator PathExpr(string path)
    {
        return new PathExpr(path.ToExpr(), null);
    }
    
    public static PathExpr IndexPath(int index, PathExpr? parent)
    {
        return new PathExpr(index.ToExpr(), parent);
    }
}
