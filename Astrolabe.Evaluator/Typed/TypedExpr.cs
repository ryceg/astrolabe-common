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
        return new SimpleTypedExpr<T2>(new DotExpr(Wrapped, TypedExprExtensions.FieldName(getter)));
    }

    public TypedExpr<T2> Prop<T2>(Expression<Func<T, T2>> getter)
    {
        return new SimpleTypedExpr<T2>(new DotExpr(Wrapped, TypedExprExtensions.FieldName(getter)));
    }

    public TypedArrayExpr<T2> ArrayProp<T2>(Expression<Func<T, IEnumerable<T2>>> getter)
    {
        return new SimpleTypedExpr<T2>(new DotExpr(Wrapped, TypedExprExtensions.FieldName(getter)));
    }

    public TypedElementExpr<T2> Elements<T2>(Expression<Func<T, IEnumerable<T2>>> getter)
    {
        return ArrayProp(getter).WithIndex();
    }
}

internal record SimpleTypedExpr<T>(Expr Wrapped, Expr? IndexExpr = null, Expr? ArrayExpr = null)
    : TypedArrayExpr<T>,
        TypedElementExpr<T>
{
    public TypedArrayExpr<T> Array => new SimpleTypedExpr<T>(ArrayExpr!);
    public NumberExpr Index => new(IndexExpr!);
}

public interface TypedArrayExpr<T> : TypedExpr<IEnumerable<T>>
{
    NumberExpr Sum()
    {
        return new NumberExpr(new CallExpr(InbuiltFunction.Sum, [Wrapped]));
    }
}

public interface TypedElementExpr<T> : TypedExpr<T>
{
    TypedArrayExpr<T> Array { get; }
    NumberExpr Index { get; }

    NumberExpr IndexTotal => new(Index.Wrapped.AsVar().Prepend("Total"));
}

public static class TypedExprExtensions
{
    public static ExprValue FieldName<T, T2>(Expression<Func<T, T2>> getExpr)
    {
        var propName = getExpr.GetPropertyInfo().Name;
        return ExprValue.From(JsonNamingPolicy.CamelCase.ConvertName(propName));
    }

    public static TypedElementExpr<T> WithIndex<T>(this TypedExpr<IEnumerable<T>> array)
    {
        var indexVar = VarExpr.MakeNew("i");
        return new SimpleTypedExpr<T>(
            new DotExpr(array.Wrapped, indexVar),
            indexVar,
            array.Wrapped
        );
    }

    public static TypedArrayExpr<T2> Map<T, T2>(
        this TypedExpr<IEnumerable<T>> array,
        Expression<Func<T, T2>> mapTo
    )
    {
        var elemPath = VarExpr.MakeNew("e");
        return new SimpleTypedExpr<T2>(
            new DotExpr(
                array.Wrapped,
                new LambdaExpr(elemPath, new DotExpr(elemPath, FieldName(mapTo)))
            )
        );
    }
}
