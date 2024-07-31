namespace Astrolabe.Evaluator.Typed;

public class NumberExpr(EvalExpr expr) : TypedExpr<int>, TypedExpr<double>, TypedExpr<long>
{
    public EvalExpr Wrapped => expr;

    public override string ToString()
    {
        return $"Number({Wrapped})";
    }

    public static implicit operator NumberExpr(int from)
    {
        return new NumberExpr(ValueExpr.From(from));
    }

    public static NumberExpr BinOp(string func, NumberExpr e1, NumberExpr e2)
    {
        return new NumberExpr(new CallExpr(func, [e1.Wrapped, e2.Wrapped]));
    }

    public static BoolExpr BinBoolOp(string func, NumberExpr e1, NumberExpr e2)
    {
        return new BoolExpr(new CallExpr(func, [e1.Wrapped, e2.Wrapped]));
    }

    public static NumberExpr operator +(NumberExpr e1, NumberExpr e2)
    {
        return BinOp("+", e1, e2);
    }

    public static NumberExpr operator -(NumberExpr e1, NumberExpr e2)
    {
        return BinOp("-", e1, e2);
    }

    public static NumberExpr operator *(NumberExpr e1, NumberExpr e2)
    {
        return BinOp("*", e1, e2);
    }

    public static NumberExpr operator /(NumberExpr e1, NumberExpr e2)
    {
        return BinOp("/", e1, e2);
    }

    public static BoolExpr operator >(NumberExpr e1, NumberExpr e2)
    {
        return BinBoolOp(">", e1, e2);
    }

    public static BoolExpr operator <(NumberExpr e1, NumberExpr e2)
    {
        return BinBoolOp("<", e1, e2);
    }

    public static BoolExpr operator >=(NumberExpr e1, NumberExpr e2)
    {
        return BinBoolOp(">=", e1, e2);
    }

    public static BoolExpr operator <=(NumberExpr e1, NumberExpr e2)
    {
        return BinBoolOp("<=", e1, e2);
    }

    public static BoolExpr operator ==(NumberExpr e1, NumberExpr e2)
    {
        return BinBoolOp("=", e1, e2);
    }

    public static BoolExpr operator !=(NumberExpr e1, NumberExpr e2)
    {
        return BinBoolOp("!=", e1, e2);
    }
}
