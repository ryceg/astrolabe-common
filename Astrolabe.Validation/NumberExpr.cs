namespace Astrolabe.Validation;

public class NumberExpr<T>(Expr expr) where T : struct
{
    public Expr Expr { get; } = expr;

    public override string ToString()
    {
        return $"Number({Expr})";
    }

    public static implicit operator NumberExpr<T>(T from)
    {
        return new NumberExpr<T>(from.ToExpr());
    }

    public static NumberExpr<T> operator +(NumberExpr<T> e1, NumberExpr<T> e2)
    {
        return new MathBinOpExpr(MathBinOp.Add, e1.Expr, e2.Expr);
    }
    
    public static NumberExpr<T> operator -(NumberExpr<T> e1, NumberExpr<T> e2)
    {
        return new MathBinOpExpr(MathBinOp.Minus, e1.Expr, e2.Expr);
    }

    public static NumberExpr<T> operator *(NumberExpr<T> e1, NumberExpr<T> e2)
    {
        return new MathBinOpExpr(MathBinOp.Multiply, e1.Expr, e2.Expr);
    }
    public static NumberExpr<T> operator /(NumberExpr<T> e1, NumberExpr<T> e2)
    {
        return new MathBinOpExpr(MathBinOp.Divide, e1.Expr, e2.Expr);
    }
    
    public static implicit operator NumberExpr<T>(MathBinOpExpr e)
    {
        return new NumberExpr<T>(e);
    }

    public static BoolExpr operator >(NumberExpr<T> e1, NumberExpr<T> e2)
    {
        return new CompareExpr(CompareType.Gt, e1.Expr, e2.Expr);
    }
    
    public static BoolExpr operator <(NumberExpr<T> e1, NumberExpr<T> e2)
    {
        return new CompareExpr(CompareType.Lt, e1.Expr, e2.Expr);
    }
    
    public static BoolExpr operator >=(NumberExpr<T> e1, NumberExpr<T> e2)
    {
        return new CompareExpr(CompareType.GtEq, e1.Expr, e2.Expr);
    }
    
    public static BoolExpr operator <=(NumberExpr<T> e1, NumberExpr<T> e2)
    {
        return new CompareExpr(CompareType.LtEq, e1.Expr, e2.Expr);
    }
    
    public static BoolExpr operator ==(NumberExpr<T> e1, NumberExpr<T> e2)
    {
        return new CompareExpr(CompareType.Eq, e1.Expr, e2.Expr);
    }

    public static BoolExpr operator !=(NumberExpr<T> e1, NumberExpr<T> e2)
    {
        return new CompareExpr(CompareType.Ne, e1.Expr, e2.Expr);
    }
}
