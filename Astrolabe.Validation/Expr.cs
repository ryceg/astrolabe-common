namespace Astrolabe.Validation;

public enum CompareType
{
    Eq,
    Lt,
    LtEq,
    Gt,
    GtEq,
    Ne
}

public enum LogicType
{
    And,
    Or,
    Not
}

public enum MathBinOp
{
    Add,
    Minus,
    Multiply,
    Divide
}

public interface Expr
{
}

public class BoolExpr(Expr expr) : Expr
{
    public Expr Expr { get; } = expr;

    public override string ToString()
    {
        return $"Bool({expr})";
    }

    public static BoolExpr operator &(BoolExpr e1, BoolExpr e2)
    {
        return new LogicOpExpr(LogicType.And, e1, e2);
    }

    public static BoolExpr operator |(BoolExpr e1, BoolExpr e2)
    {
        return new LogicOpExpr(LogicType.Or, e1, e2);
    }

    public static BoolExpr operator !(BoolExpr e1)
    {
        return new LogicOpExpr(LogicType.Not, e1, null);
    }

    public static BoolExpr operator ==(BoolExpr e1, BoolExpr e2)
    {
        return new CompareExpr(CompareType.Eq, e1, e2);
    }

    public static BoolExpr operator !=(BoolExpr e1, BoolExpr e2)
    {
        return new CompareExpr(CompareType.Ne, e1, e2);
    }
    
    public static implicit operator BoolExpr(CompareExpr expr)
    {
        return new BoolExpr(expr);
    }

    public static implicit operator BoolExpr(LogicOpExpr expr)
    {
        return new BoolExpr(expr);
    }

}

public class NumberExpr(Expr expr) : Expr
{
    public Expr Expr { get; } = expr;

    public override string ToString()
    {
        return $"Number({expr})";
    }

    public static NumberExpr operator +(NumberExpr e1, NumberExpr e2)
    {
        return new MathBinOpExpr(MathBinOp.Add, e1, e2);
    }

    public static NumberExpr operator +(NumberExpr e1, int e2)
    {
        return new MathBinOpExpr(MathBinOp.Add, e1, e2.Expr());
    }

    public static implicit operator NumberExpr(MathBinOpExpr e)
    {
        return new NumberExpr(e);
    }

    public static BoolExpr operator >(NumberExpr e1, NumberExpr e2)
    {
        return new CompareExpr(CompareType.Gt, e1, e2);
    }

    public static BoolExpr operator >(NumberExpr e1, int e2)
    {
        return new CompareExpr(CompareType.Gt, e1, e2.Expr());
    }

    public static BoolExpr operator <(NumberExpr e1, NumberExpr e2)
    {
        return new CompareExpr(CompareType.Lt, e1, e2);
    }

    public static BoolExpr operator <(NumberExpr e1, int e2)
    {
        return new CompareExpr(CompareType.Lt, e1, e2.Expr());
    }

    public static BoolExpr operator >=(NumberExpr e1, NumberExpr e2)
    {
        return new CompareExpr(CompareType.GtEq, e1, e2);
    }

    public static BoolExpr operator <=(NumberExpr e1, NumberExpr e2)
    {
        return new CompareExpr(CompareType.LtEq, e1, e2);
    }
    
    public static BoolExpr operator ==(NumberExpr e1, NumberExpr e2)
    {
        return new CompareExpr(CompareType.Eq, e1, e2);
    }

    public static BoolExpr operator !=(NumberExpr e1, NumberExpr e2)
    {
        return new CompareExpr(CompareType.Ne, e1, e2);
    }
}

public static class ConstantExtensions
{
    public static NumberExpr Expr(this int i)
    {
        return new NumberExpr(new ConstantExpr(i));
    }

    public static NumberExpr Expr(this double d)
    {
        return new NumberExpr(new ConstantExpr(d));
    }

}

public record ConstantExpr(object Value) : Expr;

public record PathExpr(bool Config, string Path) : Expr;

public record LogicOpExpr(LogicType LogicType, BoolExpr E1, BoolExpr? E2) : Expr;

public record CompareExpr(CompareType CompareType, Expr E1, Expr E2) : Expr;

public record MathBinOpExpr(MathBinOp MathBinOp, NumberExpr E1, NumberExpr E2) : Expr;

