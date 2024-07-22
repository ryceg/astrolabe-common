using System.Linq.Expressions;
using Astrolabe.Evaluator;
using Astrolabe.Evaluator.Typed;

namespace Astrolabe.Validation.Typed;

public interface TypedRuleWrapper
{
    Rule ToRule();
}

public interface TypedRule<T> : TypedRuleWrapper;

public static class TypedRule
{
    public static TypedRule<T> ForAll<T>(params TypedRule<T>[] rules)
    {
        return new SimpleTypedRule<T>(MultiRule.For(rules.Select(x => x.ToRule()).ToArray()));
    }
}

public record SimpleTypedRule<T>(Rule Rule) : TypedRule<T>
{
    public Rule ToRule() => Rule;
}

public record TypedPathRule<T>(SingleRule Single) : TypedRule<T>
{
    public Rule ToRule() => Single;

    public TypedPathRule<T> WithProperty(string key, object? value)
    {
        return WithRuleExpr(r => r.WithProp(ExprValue.From(key), new ExprValue(value)));
    }

    public TypedExpr<T> Get()
    {
        return TypedExpr.ForPathExpr<T>(Single.Path);
    }

    private TypedPathRule<T> WithRuleExpr(Func<SingleRule, SingleRule> map)
    {
        return new TypedPathRule<T>(map(Single));
    }

    public TypedPathRule<T> WithMessage(string message)
    {
        return WithRuleExpr(r => r.WithMessage(ExprValue.From(message)));
    }

    public TypedPathRule<T> Must(Expr mustExpr)
    {
        return WithRuleExpr(x => x.AndMust(mustExpr));
    }

    public TypedPathRule<T> When(Expr whenExpr)
    {
        return WithRuleExpr(x => x.When(whenExpr));
    }
}

public static class TypedRuleExtensions
{
    public static ForEachRule RuleForEach<T>(
        this TypedElementExpr<T> expr,
        LetExpr? vars,
        Rule rule
    )
    {
        return new ForEachRule(expr.Array.Wrapped, expr.Index.Wrapped, vars, rule);
    }

    public static TypedPathRule<T> RuleFor<T>(this TypedExpr<T> expr)
    {
        return new TypedPathRule<T>(new SingleRule(expr.Wrapped, ExprValue.True, ExprValue.True));
    }
}
