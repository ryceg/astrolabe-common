namespace Astrolabe.Validation;

public class BoolExpr(Expr expr)
{
    public Expr Expr { get; } = expr;

    public override string ToString()
    {
        return $"Bool({Expr})";
    }

    public static BoolExpr operator &(BoolExpr e1, BoolExpr e2)
    {
        return new LogicOpExpr(LogicType.And, e1.Expr, e2.Expr);
    }

    public static BoolExpr operator |(BoolExpr e1, BoolExpr e2)
    {
        return new LogicOpExpr(LogicType.Or, e1.Expr, e2.Expr);
    }

    public static BoolExpr operator !(BoolExpr e1)
    {
        return new LogicOpExpr(LogicType.Not, e1.Expr, null);
    }

    public static BoolExpr operator ==(BoolExpr e1, BoolExpr e2)
    {
        return new CompareExpr(CompareType.Eq, e1.Expr, e2.Expr);
    }

    public static BoolExpr operator !=(BoolExpr e1, BoolExpr e2)
    {
        return new CompareExpr(CompareType.Ne, e1.Expr, e2.Expr);
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
