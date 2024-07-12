using System.Linq.Expressions;
using System.Numerics;
using System.Text.Json;
using Astrolabe.Common;

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
    Sum,
    Count
}

public interface Expr;

public record GetExpr(Expr Path) : Expr;

public record DotExpr(Expr Base, Expr Segment) : Expr;

public record MapExpr(Expr Array, Expr ElemPath, Expr MapTo) : Expr;

public interface WrappedExpr : Expr
{
    Expr Expr { get; }
}

public interface TypedExpr<T> : WrappedExpr;

public record TypedWrappedExpr<T>(Expr Expr) : TypedExpr<T>;

public record ExprValue(object? Value, DataPath? FromPath) : Expr
{
    public static ExprValue Null => new(null, null);
    public static ExprValue False => new(false, null);
    public static ExprValue True => new(false, null);

    public ExprValue WithPath(DataPath segments)
    {
        return this with { FromPath = segments };
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

public record VarExpr(string Name, int IndexId) : Expr
{
    private static int _indexCount;

    public override string ToString()
    {
        return $"${Name}{IndexId}";
    }

    public static VarExpr MakeNew(string name)
    {
        return new VarExpr(name, ++_indexCount);
    }
}

public record RunningIndex(Expr CountExpr) : Expr;

public static class ValueExtensions
{
    public static ExprValue AsValue(this Expr expr)
    {
        return (ExprValue)expr;
    }

    public static ExprValue ToExpr(this object? v, DataPath? from = null)
    {
        if (v is ExprValue)
            throw new AggregateException("Already an expr");
        return new ExprValue(v, from);
    }

    public static IEnumerable<ExprValue> AsEnumerable(this ExprValue v)
    {
        return (v.Value as IEnumerable<ExprValue>)!;
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

    public static DataPath AsPath(this ExprValue v)
    {
        return (DataPath)v.Value!;
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

    public static Expr DotExpr(this Expr expr, Expr other)
    {
        return (expr, other) switch
        {
            (ExprValue { Value: DataPath ps }, ExprValue v) => ApplyDot(ps, v).ToExpr(),
            _ => new DotExpr(expr, other)
        };
    }

    public static Expr Unwrap(this Expr e)
    {
        return e is WrappedExpr we ? we.Expr.Unwrap() : e;
    }

    public static Expr WrapWithProperty(this Expr expr, Expr key, Expr value)
    {
        return new CallExpr(InbuiltFunction.WithProperty, [key, value, expr]);
    }

    public static Expr WrapWithMessage(this Expr expr, Expr message)
    {
        return new CallExpr(InbuiltFunction.WithMessage, [message, expr]);
    }

    public static DataPath ApplyDot(DataPath basePath, ExprValue segment)
    {
        return segment switch
        {
            { Value: string s } => new FieldPath(s, basePath),
            _ => new IndexPath(segment.AsInt(), basePath)
        };
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
        return expr.Expr.DotExpr(index).ToTypedPath<TRoot, TCurrent>();
    }

    public static TypedExpr<IEnumerable<TOut>> Map<TRoot, TCurrent, TOut>(
        this TypedPathExpr<TRoot, ICollection<TCurrent>> arrayPath,
        Func<TypedPathExpr<TRoot, TCurrent>, TypedExpr<TOut>> mapFunc
    )
    {
        var current = VarExpr.MakeNew("elem");
        return new MapExpr(
            arrayPath.Get(),
            current,
            mapFunc(new PropertyValidator<TRoot, TCurrent>(current, null))
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
        return new GetExpr(Expr).ToTyped<TCurrent>();
    }

    public TypedExpr<TN> Get<TN>(Expression<Func<TCurrent, TN?>> expr)
        where TN : struct
    {
        return this.Prop(expr).Get();
    }

    internal Expr UnsafeProp<TNext>(Expression<Func<TCurrent, TNext>> prop)
    {
        var propName = JsonNamingPolicy.CamelCase.ConvertName(prop.GetPropertyInfo().Name);
        return Expr.DotExpr(propName.ToExpr());
    }
}
