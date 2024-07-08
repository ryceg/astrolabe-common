using System.Numerics;

namespace Astrolabe.Validation;

public class NumberExpr<T>(Expr expr) where T : struct
{
    public Expr Expr { get; } = expr;

    public override string ToString()
    {
        return $"Number({Expr})";
    }

    public static implicit operator NumberExpr<T>(int from)
    {
        return new NumberExpr<T>(from.ToExpr());
    }

    public static NumberExpr<T> BinOp(InbuiltFunction func, NumberExpr<T> e1, NumberExpr<T> e2)
    {
        return new NumberExpr<T>(new CallExpr(func, [e1.Expr, e2.Expr]));
    }

    public static BoolExpr BinBoolOp(InbuiltFunction func, NumberExpr<T> e1, NumberExpr<T> e2)
    {
        return new BoolExpr(new CallExpr(func, [e1.Expr, e2.Expr]));
    }

    public static NumberExpr<T> operator +(NumberExpr<T> e1, NumberExpr<T> e2)
    {
        return BinOp(InbuiltFunction.Add, e1, e2);
    }
    
    public static NumberExpr<T> operator -(NumberExpr<T> e1, NumberExpr<T> e2)
    {
        return BinOp(InbuiltFunction.Minus, e1, e2);
    }

    public static NumberExpr<T> operator *(NumberExpr<T> e1, NumberExpr<T> e2)
    {
        return BinOp(InbuiltFunction.Multiply, e1, e2);
    }
    public static NumberExpr<T> operator /(NumberExpr<T> e1, NumberExpr<T> e2)
    {
        return BinOp(InbuiltFunction.Divide, e1, e2);
    }

    public static BoolExpr operator >(NumberExpr<T> e1, NumberExpr<T> e2)
    {
        return BinBoolOp(InbuiltFunction.Gt, e1, e2);
    }
    
    public static BoolExpr operator <(NumberExpr<T> e1, NumberExpr<T> e2)
    {
        return BinBoolOp(InbuiltFunction.Lt, e1, e2);
    }
    
    public static BoolExpr operator >=(NumberExpr<T> e1, NumberExpr<T> e2)
    {
        return BinBoolOp(InbuiltFunction.GtEq, e1, e2);
    }
    
    public static BoolExpr operator <=(NumberExpr<T> e1, NumberExpr<T> e2)
    {
        return BinBoolOp(InbuiltFunction.LtEq, e1, e2);
    }
    
    public static BoolExpr operator ==(NumberExpr<T> e1, NumberExpr<T> e2)
    {
        return BinBoolOp(InbuiltFunction.Eq, e1, e2);
    }

    public static BoolExpr operator !=(NumberExpr<T> e1, NumberExpr<T> e2)
    {
        return BinBoolOp(InbuiltFunction.Ne, e1, e2);
    }
}
