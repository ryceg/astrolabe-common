using System.Linq.Expressions;
using System.Text.Json;
using Astrolabe.Common;

namespace Astrolabe.Validation.Typed;

public interface WrappedExpr
{
    Expr Wrapped { get; }
}

public interface TypedExpr<T> : WrappedExpr
{
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

    public TypedRule<T> RuleForEach<T2>(
        Expression<Func<T, IEnumerable<T2>>> getArray,
        Func<TypedElementExpr<T2>, TypedRuleWrapper> makeRule
    )
    {
        var typedArray = ArrayProp(getArray);
        var indexed = typedArray.WithIndex();
        return new TypedForEachRule<T>(
            new ForEachRule(typedArray.Wrapped, indexed.Index.Wrapped, makeRule(indexed).ToRule())
        );
    }

    public TypedPathRule<T> RuleFor()
    {
        return new TypedPathRule<T>(new SingleRule(Wrapped, ExprValue.True, ExprValue.True));
    }
}

public record SimpleTypedExpr<T>(Expr Wrapped, Expr? IndexExpr = null)
    : TypedArrayExpr<T>,
        TypedElementExpr<T>
{
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
        return new SimpleTypedExpr<T>(new DotExpr(array.Wrapped, indexVar), indexVar);
    }

    public static TypedArrayExpr<T2> Map<T, T2>(
        this TypedExpr<IEnumerable<T>> array,
        Expression<Func<T, T2>> mapTo
    )
    {
        var elemPath = VarExpr.MakeNew("e");
        return new SimpleTypedExpr<T2>(
            new MapExpr(array.Wrapped, elemPath, new DotExpr(elemPath, FieldName(mapTo)))
        );
    }
}
