using System.Linq.Expressions;
using System.Numerics;
using System.Text.Json;
using Astrolabe.Common;

namespace Astrolabe.Validation;

public class TypedPath<T>(PathExpr path)
{
    protected PathExpr MakePathExpr(string propertyName)
    {
        return new PathExpr(JsonNamingPolicy.CamelCase.ConvertName(propertyName).ToExpr(), path);
    }
    
    public NumberExpr Num<TN>(Expression<Func<T, TN?>> func) where TN : struct, ISignedNumber<TN>
    {
        var childPath = MakePathExpr(func.GetPropertyInfo().Name);
        return new NumberExpr(new GetData(childPath));
    }

}

public class BoolExpr(Expr expr) : WrappedExpr
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

    public static implicit operator BoolExpr(bool b)
    {
        return new BoolExpr(b.ToExpr());
    }
}
