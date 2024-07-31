namespace Astrolabe.Evaluator.Typed;

public class BoolExpr(EvalExpr expr) : TypedExpr<bool>
{
    public EvalExpr Wrapped => expr;

    public override string ToString()
    {
        return $"Bool({Wrapped})";
    }

    public static BoolExpr BinOp(string func, BoolExpr e1, BoolExpr e2)
    {
        return new BoolExpr(new CallExpr(func, [e1.Wrapped, e2.Wrapped]));
    }

    public static BoolExpr operator &(BoolExpr e1, BoolExpr e2)
    {
        return BinOp("and", e1, e2);
    }

    public static BoolExpr operator |(BoolExpr e1, BoolExpr e2)
    {
        return BinOp("or", e1, e2);
    }

    public static BoolExpr operator !(BoolExpr e1)
    {
        return new BoolExpr(new CallExpr("!", [e1.Wrapped]));
    }

    public static BoolExpr operator ==(BoolExpr e1, BoolExpr e2)
    {
        return BinOp("=", e1, e2);
    }

    public static BoolExpr operator !=(BoolExpr e1, BoolExpr e2)
    {
        return BinOp("!=", e1, e2);
    }

    public static implicit operator BoolExpr(bool b)
    {
        return new BoolExpr(ValueExpr.From(b));
    }
}
