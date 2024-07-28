namespace Astrolabe.Evaluator.Typed;

public class BoolExpr(EvalExpr expr) : TypedExpr<bool>
{
    public EvalExpr Wrapped => expr;

    public override string ToString()
    {
        return $"Bool({Wrapped})";
    }

    public static BoolExpr BinOp(InbuiltFunction func, BoolExpr e1, BoolExpr e2)
    {
        return new BoolExpr(CallExpr.Inbuilt(func, [e1.Wrapped, e2.Wrapped]));
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
        return new BoolExpr(CallExpr.Inbuilt(InbuiltFunction.Not, [e1.Wrapped]));
    }

    public static BoolExpr operator ==(BoolExpr e1, BoolExpr e2)
    {
        return BinOp(InbuiltFunction.Eq, e1, e2);
    }

    public static BoolExpr operator !=(BoolExpr e1, BoolExpr e2)
    {
        return BinOp(InbuiltFunction.Ne, e1, e2);
    }

    public static implicit operator BoolExpr(bool b)
    {
        return new BoolExpr(ValueExpr.From(b));
    }
}
