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

public record ExprValue(object? Value, JsonPathSegments? FromPath) : Expr
{
    public static ExprValue Null => new(null, null);
    public static ExprValue False => new(false, null);
    public static ExprValue True => new(false, null);

    public ExprValue WithPath(JsonPathSegments segents)
    {
        return this with { FromPath = segents };
    }

    public static double AsDouble(object? v)
    {
        return v switch
        {
            int i => i,
            long l => l,
            double d => d,
            _ => throw new ArgumentException("Value is not a number: " + (v ?? "null"))
        };
    }
}

public record ArrayExpr(IEnumerable<Expr> ValueExpr) : Expr
{
    public override string ToString()
    {
        return $"[{string.Join(", ", ValueExpr)}]";
    }
}

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
    public static ExprValue ToExpr(this object? v, JsonPathSegments? from = null)
    {
        if (v is ExprValue)
            throw new AggregateException("Already an expr");
        return new ExprValue(v, from);
    }

    public static IEnumerable<object?> AsEnumerable(this ExprValue v)
    {
        return (v.Value as IEnumerable)!.Cast<object?>();
    }

    public static IEnumerable<T> AsEnumerable<T>(this ExprValue v)
    {
        return (v.Value as IEnumerable)!.OfType<T>();
    }

    public static bool AsBool(this ExprValue v)
    {
        return (bool)v.Value!;
    }

    public static int AsInt(this ExprValue v)
    {
        return (int)v.Value!;
    }

    public static object AsEqualityCheck(this ExprValue v)
    {
        return v.Value switch
        {
            int i => (double)i,
            long l => (double)l,
            { } o => o,
            _ => throw new ArgumentException("Cannot be compared: " + v)
        };
    }

    public static long? MaybeLong(this ExprValue v)
    {
        return v.Value switch
        {
            int or long => (long)v.Value,
            _ => null
        };
    }

    public static bool IsEitherNull(this ExprValue v, ExprValue other)
    {
        return v.IsNull() || other.IsNull();
    }

    public static double AsDouble(this ExprValue v)
    {
        return ExprValue.AsDouble(v.Value);
    }

    public static JsonPathSegments AsPath(this ExprValue v)
    {
        return (JsonPathSegments)v.Value!;
    }

    public static bool IsNull(this ExprValue v)
    {
        return v.Value == null;
    }

    public static bool IsTrue(this ExprValue v)
    {
        return v.Value is true;
    }

    public static bool IsFalse(this ExprValue v)
    {
        return v.Value is false;
    }

    public static string AsString(this ExprValue v)
    {
        return (string)v.Value!;
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
        this TypedPathExpr<TRoot, TCurrent> expr,
        Expression<Func<TCurrent, TNext?>> prop
    )
        where TNext : struct
    {
        return expr.UnsafeProp(prop).ToTypedPath<TRoot, TNext>();
    }

    public static TypedPathExpr<TRoot, TNext> Prop<TRoot, TCurrent, TNext>(
        this TypedPathExpr<TRoot, TCurrent> expr,
        Expression<Func<TCurrent, TNext?>> prop
    )
    {
        return expr.UnsafeProp(prop).ToTypedPath<TRoot, TNext>();
    }

    public static TypedPathExpr<TRoot, TCurrent> Indexed<TRoot, TCurrent>(
        this TypedPathExpr<TRoot, IEnumerable<TCurrent>> expr,
        TypedExpr<int> index
    )
    {
        return new CallExpr(InbuiltFunction.Dot, [expr.Expr, index]).ToTypedPath<TRoot, TCurrent>();
    }

    public static TypedExpr<IEnumerable<TOut>> Map<TRoot, TCurrent, TOut>(
        this TypedPathExpr<TRoot, ICollection<TCurrent>> arrayPath,
        Func<TypedPathExpr<TRoot, TCurrent>, TypedExpr<TOut>> mapFunc
    )
    {
        var current = VarExpr.MakeNew();
        ;
        return new CallExpr(
            InbuiltFunction.Map,
            [
                arrayPath.Get(),
                current,
                mapFunc(new PropertyValidator<TRoot, TCurrent>(current, null))
            ]
        ).ToTyped<IEnumerable<TOut>>();
    }

    public static TypedExpr<TCurrent> Sum<TCurrent>(this TypedExpr<IEnumerable<TCurrent>> arrayExpr)
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
}
