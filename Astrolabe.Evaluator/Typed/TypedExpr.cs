using System.Linq.Expressions;
using System.Text.Json;
using Astrolabe.Common;

namespace Astrolabe.Evaluator.Typed;

public interface WrappedExpr
{
    Expr Wrapped { get; }
}

public static class TypedExpr
{
    public static TypedExpr<TRoot> Root<TRoot>() => new SimpleTypedExpr<TRoot>(ExprValue.EmptyPath);

    public static TypedExpr<T> ForPathExpr<T>(Expr expr) => new SimpleTypedExpr<T>(expr);
}

public interface TypedExpr<T> : WrappedExpr
{
    public TypedExpr<T> Resolve()
    {
        return new SimpleTypedExpr<T>(new ResolveEval(Wrapped));
    }

    public TypedExpr<T2> Prop<T2>(Expression<Func<T, T2?>> getter)
        where T2 : struct
    {
        return new SimpleTypedExpr<T2>(TypedExprExtensions.FieldName(getter));
    }

    public TypedExpr<T2> Prop<T2>(Expression<Func<T, T2>> getter)
    {
        return new SimpleTypedExpr<T2>(TypedExprExtensions.FieldName(getter));
    }

    public TypedElementExpr<T2> Elements<T2>(Expression<Func<T, IEnumerable<T2>>> getter)
    {
        return new SimpleTypedExpr<T2>(TypedExprExtensions.FieldName(getter), VarExpr.MakeNew("i"));
    }
}

internal record SimpleTypedExpr<T>(Expr Wrapped, Expr? IndexExpr = null) : TypedElementExpr<T>
{
    public NumberExpr Index => new(IndexExpr!);
}

public interface TypedElementExpr<T> : TypedExpr<T>
{
    NumberExpr Index { get; }
}

public static class TypedExprExtensions
{
    public static ExprValue FieldName<T, T2>(Expression<Func<T, T2>> getExpr)
    {
        var propName = getExpr.GetPropertyInfo().Name;
        return ExprValue.From(
            new FieldPath(JsonNamingPolicy.CamelCase.ConvertName(propName), DataPath.Empty)
        );
    }
}
