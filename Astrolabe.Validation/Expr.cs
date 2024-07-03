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

public interface BoolExpr : Expr
{
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

    public static virtual BoolExpr operator ==(BoolExpr e1, BoolExpr e2)
    {
        return new CompareExpr(CompareType.Eq, e1, e2);
    }

    public static virtual BoolExpr operator !=(BoolExpr e1, BoolExpr e2)
    {
        return new CompareExpr(CompareType.Ne, e1, e2);
    }

}

public interface NumberExpr : Expr
{
    public static NumberExpr operator +(NumberExpr e1, NumberExpr e2)
    {
        return new MathBinOpExpr(MathBinOp.Add, e1, e2);
    }

    public static NumberExpr operator +(NumberExpr e1, int e2)
    {
        return new MathBinOpExpr(MathBinOp.Add, e1, e2.Expr());
    }

    public static BoolExpr operator >(NumberExpr e1, NumberExpr e2)
    {
        return new CompareExpr(CompareType.Gt, e1, e2);
    }

    public static BoolExpr operator >(NumberExpr e1, int e2)
    {
        return new CompareExpr(CompareType.Gt, e1, new NumberConstant(e2, null));
    }

    public static BoolExpr operator <(NumberExpr e1, NumberExpr e2)
    {
        return new CompareExpr(CompareType.Lt, e1, e2);
    }

    public static BoolExpr operator <(NumberExpr e1, int e2)
    {
        return new CompareExpr(CompareType.Lt, e1, new NumberConstant(e2, null));
    }

    public static BoolExpr operator >=(NumberExpr e1, NumberExpr e2)
    {
        return new CompareExpr(CompareType.GtEq, e1, e2);
    }

    public static BoolExpr operator <=(NumberExpr e1, NumberExpr e2)
    {
        return new CompareExpr(CompareType.LtEq, e1, e2);
    }
    
    public static virtual BoolExpr operator ==(NumberExpr e1, NumberExpr e2)
    {
        return new CompareExpr(CompareType.Eq, e1, e2);
    }

    public static virtual BoolExpr operator !=(NumberExpr e1, NumberExpr e2)
    {
        return new CompareExpr(CompareType.Ne, e1, e2);
    }

}

public static class ConstantExtensions
{
    public static NumberExpr Expr(this int i)
    {
        return new NumberConstant(i, null);
    }

    public static NumberExpr Expr(this double d)
    {
        return new NumberConstant(null, d);
    }

}

public record NumberConstant(int? IntValue, double? DoubleValue) : NumberExpr;

public record NumberCast(Expr Expr) : NumberExpr;

public record BoolCast(Expr Expr) : BoolExpr;

public record PathExpr(bool Config, string Path) : Expr;

public record LogicOpExpr(LogicType LogicType, BoolExpr E1, BoolExpr? E2) : BoolExpr;

public record CompareExpr(CompareType CompareType, Expr E1, Expr E2) : BoolExpr;

public record MathBinOpExpr(MathBinOp MathBinOp, NumberExpr E1, NumberExpr E2) : NumberExpr;

