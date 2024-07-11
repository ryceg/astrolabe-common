using System.Linq.Expressions;
using NotImplementedException = System.NotImplementedException;

namespace Astrolabe.Validation;

public class PropertyValidator<T, T2>(Expr path, NumberExpr? index) : TypedPathExpr<T, T2>
{
    public NumberExpr Index => index!;

    public NumberExpr RunningIndex => new(new RunningIndex(Index.Expr));
    
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
        var indexExpr = VarExpr.MakeNew("i").AsNumber();
        var arrayPath = this.Prop(expr);
        var childProps = new PropertyValidator<T, TC>(arrayPath.Indexed(indexExpr), indexExpr);
        return new RulesForEach<T>(arrayPath, indexExpr, rules(childProps));
    }
    public Expr Expr => path;
}
