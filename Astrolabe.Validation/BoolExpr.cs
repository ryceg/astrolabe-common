namespace Astrolabe.Validation;

public class BoolExpr(Expr expr)
{
    public Expr Expr { get; } = expr;

    public override string ToString()
    {
        return $"Bool({Expr})";
    }
    
    public static BoolExpr BinOp(InbuiltFunction func, BoolExpr e1, BoolExpr e2)
    {
        return new BoolExpr(new CallExpr(func, [e1.Expr, e2.Expr]));
    }
    
    public static BoolExpr operator &(BoolExpr e1, BoolExpr e2)
    {
        return BinOp(InbuiltFunction.And, e1, e2);
    }

    public static BoolExpr operator |(BoolExpr e1, BoolExpr e2)
    {
        return BinOp(InbuiltFunction.Or, e1, e2);
    }

    public static BoolExpr operator !(BoolExpr e1)
    {
        return new BoolExpr(new CallExpr(InbuiltFunction.Not, [e1.Expr]));
    }

    public static BoolExpr operator ==(BoolExpr e1, BoolExpr e2)
    {
        return BinOp(InbuiltFunction.Eq, e1, e2);
    }

    public static BoolExpr operator !=(BoolExpr e1, BoolExpr e2)
    {
        return BinOp(InbuiltFunction.Ne, e1, e2);
    }
}
