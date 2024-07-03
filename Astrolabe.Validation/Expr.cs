using System.Numerics;

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

public class NumberConstant(int? intValue, double? doubleValue) : NumberExpr
{
    public int? IntValue { get; } = intValue;
    public double? DoubleValue { get; } = doubleValue;
}

public class PathExpr(bool config, string path) : BoolExpr, NumberExpr
{
    public bool Config { get; } = config;
    public string Path { get; } = path;
}

public class LogicOpExpr(LogicType logicType, BoolExpr e1, BoolExpr? e2) : BoolExpr
{
    public LogicType LogicType { get; } = logicType;
    public BoolExpr E1 { get; } = e1;
    public BoolExpr? E2 { get; } = e2;
}

public class CompareExpr(CompareType compareType, Expr e1, Expr e2) : BoolExpr
{
    public CompareType CompareType { get; } = compareType;
    public Expr E1 { get; } = e1;
    public Expr E2 { get; } = e2;
}

public class MathBinOpExpr(MathBinOp compareType, NumberExpr e1, NumberExpr e2) : NumberExpr
{
    public MathBinOp CompareType { get; } = compareType;
    public NumberExpr E1 { get; } = e1;
    public NumberExpr E2 { get; } = e2;
}

