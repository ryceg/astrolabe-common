using System.Collections;
using System.Linq.Expressions;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Astrolabe.Common;
using Astrolabe.JSON;

namespace Astrolabe.Validation;

public enum InbuiltFunction
{
    Eq,
    Lt,
    LtEq,
    Gt,
    GtEq,
    Ne,
    And,
    Or,
    Not,
    Add,
    Minus,
    Multiply,
    Divide,
    WithMessage,
    WithProperty,
    IfElse,
    Get,
    Dot,
    Map,
    Sum,
    Count
}

public interface Expr;

public interface WrappedExpr : Expr
{
    Expr Expr { get; }
}

public interface TypedExpr<T> : WrappedExpr;

public record TypedWrappedExpr<T>(Expr Expr) : TypedExpr<T>;

public interface ExprValue : Expr
{
    public static readonly NullValue Null = new();
}

public record NullValue : ExprValue
{
    public override string ToString()
    {
        return "null";
    }
}

public record BoolValue(bool Value) : ExprValue
{
    public override string ToString()
    {
        return Value.ToString();
    }
}

public record NumberValue(long? LongValue, double? DoubleValue) : ExprValue
{
    public override string ToString()
    {
        return (LongValue ?? DoubleValue).ToString()!;
    }

    public long ToTruncated()
    {
        return LongValue ?? (long)DoubleValue!;
    }

    public double AsDouble()
    {
        return DoubleValue ?? LongValue!.Value;
    }

    public static implicit operator NumberValue(long l)
    {
        return new NumberValue(l, null);
    }
}

public record StringValue(string Value) : ExprValue
{
    public override string ToString()
    {
        return Value;
    }
}

public record ArrayExpr(IEnumerable<Expr> ValueExpr) : Expr
{
    public override string ToString()
    {
        return $"[{string.Join(", ", ValueExpr)}]";
    }
}

public record ArrayValue(IEnumerable<ExprValue> Values) : ExprValue
{
    public override string ToString()
    {
        return $"[{string.Join(", ", Values)}]";
    }
}

public record ObjectValue(JsonObject JsonObject) : ExprValue;

public record PathValue(JsonPathSegments Path) : ExprValue;

public record CallExpr(InbuiltFunction Function, ICollection<Expr> Args) : Expr
{
    public override string ToString()
    {
        return $"{Function}({string.Join(", ", Args)})";
    }
}

public record IndexExpr(int IndexId) : Expr
{
    private static int _indexCount;

    public override string ToString()
    {
        return $"[i{IndexId}]";
    }

    public static IndexExpr MakeNew()
    {
        return new IndexExpr(++_indexCount);
    }
}

public record VarExpr(int IndexId) : Expr
{
    private static int _indexCount;

    public override string ToString()
    {
        return $"[v{IndexId}]";
    }

    public static VarExpr MakeNew()
    {
        return new VarExpr(++_indexCount);
    }
}

public record RunningIndex(Expr CountExpr) : Expr;

public static class ValueExtensions
{
    public static ExprValue ToExpr(this object? v)
    {
        return v switch
        {
            null => ExprValue.Null,
            bool b => new BoolValue(b),
            string s => new StringValue(s),
            int i => new NumberValue(i, null),
            long l => new NumberValue(l, null),
            double d => new NumberValue(null, d),
        };
    }

    public static bool AsBool(this ExprValue v)
    {
        return ((BoolValue)v).Value;
    }

    public static bool IsNull(this ExprValue v)
    {
        return v switch
        {
            NullValue => true,
            _ => false
        };
    }

    public static bool IsTrue(this ExprValue v)
    {
        return v switch
        {
            BoolValue bv => bv.Value,
            _ => false
        };
    }

    public static bool IsFalse(this ExprValue v)
    {
        return v switch
        {
            BoolValue bv => !bv.Value,
            _ => false
        };
    }

    public static string AsString(this ExprValue v)
    {
        return ((StringValue)v).Value;
    }

    public static Expr AndExpr(this Expr? expr, Expr other)
    {
        return expr == null ? other : new CallExpr(InbuiltFunction.And, [expr, other]);
    }
}

public static class TypedExprExtensions
{
    public static NumberExpr AsNumber(this Expr expr)
    {
        return new NumberExpr(expr);
    }

    public static TypedExpr<T> ToTyped<T>(this Expr expr)
    {
        return new TypedWrappedExpr<T>(expr);
    }
    
    public static TypedPathExpr<T, T2> ToTypedPath<T, T2>(this Expr expr)
    {
        return new TypedWrappedPathExpr<T, T2>(expr);
    }

    public static TypedPathExpr<TRoot, TNext> Prop<TRoot, TCurrent, TNext>(
        this TypedPathExpr<TRoot, TCurrent> expr, Expression<Func<TCurrent, TNext?>> prop)
        where TNext : struct
    {
        return expr.UnsafeProp(prop).ToTypedPath<TRoot, TNext>();
    }

    public static TypedPathExpr<TRoot, TNext> Prop<TRoot, TCurrent, TNext>(
        this TypedPathExpr<TRoot, TCurrent> expr, Expression<Func<TCurrent, TNext?>> prop)
    {
        return expr.UnsafeProp(prop).ToTypedPath<TRoot, TNext>();
    }

    public static TypedPathExpr<TRoot, TCurrent> Indexed<TRoot, TCurrent>(
        this TypedPathExpr<TRoot, IEnumerable<TCurrent>> expr, TypedExpr<int> index)
    {
        return new CallExpr(InbuiltFunction.Dot, [expr.Expr, index]).ToTypedPath<TRoot, TCurrent>();
    }

    public static TypedExpr<IEnumerable<TOut>> Map<TRoot, TCurrent, TOut>(
        this TypedPathExpr<TRoot, ICollection<TCurrent>> arrayPath, 
        Func<TypedPathExpr<TRoot, TCurrent>, TypedExpr<TOut>> mapFunc)
    {
        var current = VarExpr.MakeNew();;
        return new CallExpr(InbuiltFunction.Map, [arrayPath.Get(), current,
            mapFunc(new PropertyValidator<TRoot, TCurrent>(
                current, null))]).ToTyped<IEnumerable<TOut>>();
    }

    public static TypedExpr<TCurrent> Sum<TCurrent>(
        this TypedExpr<IEnumerable<TCurrent>> arrayExpr)
    where TCurrent : INumber<TCurrent>
    {
        return new CallExpr(InbuiltFunction.Sum, [arrayExpr.Expr]).ToTyped<TCurrent>();
    }

}

public record TypedWrappedPathExpr<TRoot, TCurrent>(Expr Expr) : TypedPathExpr<TRoot, TCurrent>;
    
public interface TypedPathExpr<TRoot, TCurrent> : WrappedExpr
{
    public TypedExpr<TCurrent> Get()
    {
        return new CallExpr(InbuiltFunction.Get, [Expr]).ToTyped<TCurrent>();
    }

    public TypedExpr<TN> Get<TN>(Expression<Func<TCurrent, TN?>> expr)
        where TN : struct
    {
        return this.Prop(expr).Get();
    }

    internal Expr UnsafeProp<TNext>(Expression<Func<TCurrent, TNext>> prop)
    {
        var propName = JsonNamingPolicy.CamelCase.ConvertName(prop.GetPropertyInfo().Name);
        return new CallExpr(InbuiltFunction.Dot, [Expr, propName.ToExpr()]);
    }
    
    // public TypedPathExpr<TRoot, TNext> Prop<TNext>(Expression<Func<TCurrent, TNext?>> prop)
    //     where TNext : struct
    // {
    //     return UnsafeProp(prop).ToTypedPath<TRoot, TNext>();
    // }
    //
    // public TypedPathExpr<TRoot, TNext> Prop<TNext>(Expression<Func<TCurrent, TNext?>> prop)
    // {
    //     return UnsafeProp(prop).ToTypedPath<TRoot, TNext>();
    // }

}
