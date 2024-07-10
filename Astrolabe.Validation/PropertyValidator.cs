using System.Linq.Expressions;
using NotImplementedException = System.NotImplementedException;

namespace Astrolabe.Validation;

public class PropertyValidator<T, T2>(Expr path, NumberExpr? index) : TypedPathExpr<T, T2>
{
    public NumberExpr Index => index!;

    public NumberExpr RunningIndex => new(new RunningIndex(Index.Expr));

    
    // public TypedPathExpr<T, TNext> Prop<TNext>(Expression<Func<T2, TNext?>> prop)
    //     where TNext : struct
    // {
    //     var propName = JsonNamingPolicy.CamelCase.ConvertName(prop.GetPropertyInfo().Name);
    //     return new TypedWrappedPathExpr<T, TNext>(new CallExpr(InbuiltFunction.Dot, [Expr, propName.ToExpr()]));
    // }
    //
    // public TypedPathExpr<T, TNext> Prop<TNext>(Expression<Func<T2, TNext?>> prop)
    // {
    //     var propName = JsonNamingPolicy.CamelCase.ConvertName(prop.GetPropertyInfo().Name);
    //     return new TypedWrappedPathExpr<T, TNext>(new CallExpr(InbuiltFunction.Dot, [Expr, propName.ToExpr()]));
    // }

    public RuleBuilder<T, TN> RuleFor<TN>(Expression<Func<T2, TN?>> expr)
        where TN : struct
    {
        return new SimpleRuleBuilder<T, TN>(this.Prop(expr));
    }

    public RuleBuilder<T, TN> RuleFor<TN>(Expression<Func<T2, TN?>> expr)
    {
        return new SimpleRuleBuilder<T, TN>(this.Prop(expr));
    }

    public RulesForEach<T> RuleForEach<TC>(
        Expression<Func<T2, IEnumerable<TC>?>> expr,
        Func<PropertyValidator<T, TC>, Rule<T>> rules
    )
    {
        var indexExpr = IndexExpr.MakeNew().AsNumber();
        var arrayPath = this.Prop(expr);
        var childProps = new PropertyValidator<T, TC>(arrayPath.Indexed(indexExpr), indexExpr);
        return new RulesForEach<T>(arrayPath, indexExpr, rules(childProps));
    }

    // public NumberExpr Sum<TObj>(
    //     Expression<Func<T2, IEnumerable<TObj>>> expr,
    //     Func<TypedPathExpr<T2, TObj>, NumberExpr> map
    // )
    // {
    //     var arrayPath = MakePathExpr(expr.GetPropertyInfo().Name);
    //     var indexExpr = IndexExpr.MakeNew();
    //     var childProps = new TypedPath<TObj>(new PathExpr(indexExpr, arrayPath));
    //     return new NumberExpr(
    //         new CallExpr(
    //             InbuiltFunction.Sum,
    //             [new MapExpr(arrayPath, indexExpr, map(childProps).Expr)]
    //         )
    //     );
    // }
    //
    // public NumberExpr Count<TObj>(Expression<Func<T2, IEnumerable<TObj>>> expr)
    // {
    //     var arrayPath = MakePathExpr(expr.GetPropertyInfo().Name);
    //     return new NumberExpr(new CallExpr(InbuiltFunction.Count, [new GetData(arrayPath)]));
    // }
    //
    // public NumberExpr Get<TN>(Expression<Func<T2, TN?>> expr)
    // {
    //     var propertyInfo = expr.GetPropertyInfo();
    //     return new NumberExpr(new GetData(MakePathExpr(propertyInfo.Name)));
    // }
    public Expr Expr => path;
}
