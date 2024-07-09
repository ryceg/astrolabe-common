using System.Numerics;

namespace Astrolabe.Validation;

public class NumberExpr(Expr expr) : WrappedExpr
{
    public Expr Expr { get; } = expr;

    public override string ToString()
    {
        return $"Number({Expr})";
    }

    public static implicit operator NumberExpr(int from)
    {
        return new NumberExpr(from.ToExpr());
    }

    public static NumberExpr BinOp(InbuiltFunction func, NumberExpr e1, NumberExpr e2)
    {
        return new NumberExpr(new CallExpr(func, [e1.Expr, e2.Expr]));
    }

    public static BoolExpr BinBoolOp(InbuiltFunction func, NumberExpr e1, NumberExpr e2) 
    {
        return new BoolExpr(new CallExpr(func, [e1.Expr, e2.Expr]));
    }

    public static NumberExpr operator +(NumberExpr e1, NumberExpr e2)
    {
        return BinOp(InbuiltFunction.Add, e1, e2);
    }
    
    public static NumberExpr operator -(NumberExpr e1, NumberExpr e2)
    {
        return BinOp(InbuiltFunction.Minus, e1, e2);
    }

    public static NumberExpr operator *(NumberExpr e1, NumberExpr e2)
    {
        return BinOp(InbuiltFunction.Multiply, e1, e2);
    }
    public static NumberExpr operator /(NumberExpr e1, NumberExpr e2)
    {
        return BinOp(InbuiltFunction.Divide, e1, e2);
    }

    public static BoolExpr operator >(NumberExpr e1, NumberExpr e2)
    {
        return BinBoolOp(InbuiltFunction.Gt, e1, e2);
    }
    
    public static BoolExpr operator <(NumberExpr e1, NumberExpr e2)
    {
        return BinBoolOp(InbuiltFunction.Lt, e1, e2);
    }
    
    public static BoolExpr operator >=(NumberExpr e1, NumberExpr e2)
    {
        return BinBoolOp(InbuiltFunction.GtEq, e1, e2);
    }
    
    public static BoolExpr operator <=(NumberExpr e1, NumberExpr e2)
    {
        return BinBoolOp(InbuiltFunction.LtEq, e1, e2);
    }
    
    public static BoolExpr operator ==(NumberExpr e1, NumberExpr e2)
    {
        return BinBoolOp(InbuiltFunction.Eq, e1, e2);
    }
    
    public static BoolExpr operator !=(NumberExpr e1, NumberExpr e2)
    {
        return BinBoolOp(InbuiltFunction.Ne, e1, e2);
    }
}
