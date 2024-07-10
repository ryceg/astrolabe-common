using System.Numerics;
using System.Runtime.InteropServices;
using Astrolabe.JSON;

namespace Astrolabe.Validation;

public interface RuleBuilder<T, TProp> : TypedPathExpr<T, TProp>
{
    Expr Path { get; }

    Expr? Must { get; }
}

public record SimpleRuleBuilder<T, TProp>(Expr Path) : RuleBuilder<T, TProp>
{
    public Expr? Must => null;
    public Expr Expr => Path;
}

public interface Rule<T>;

public record MultiRule<T>(IEnumerable<Rule<T>> Rules) : Rule<T>
{
    public MultiRule<T> AddRule(Rule<T> rule)
    {
        return new MultiRule<T>(Rules.Append(rule));
    }

    public override string ToString()
    {
        return $"[\n{string.Join("\n", Rules)}\n]";
    }
}

public interface PathRule<T> : Rule<T>
{
    Expr Path { get; }

    Expr Must { get; }
}

public interface RuleAndBuilder<T, TProp> : Rule<T>, RuleBuilder<T, TProp>
{
    Expr Must { get; }
}

public record PathRules<T, TProp>(Expr Path, Expr Must) : RuleAndBuilder<T, TProp>, PathRule<T>
{
    public Expr Expr => Path;
}

public record RulesForEach<T>(Expr Path, Expr Index, Rule<T> Rule) : Rule<T>;

public record ResolvedRule<T>(JsonPathSegments Path, Expr Must);

public static class RuleExtensions
{
    public static PathRules<T, TN> CallInbuilt<T, TN>(
        this RuleBuilder<T, TN> ruleFor,
        InbuiltFunction func,
        Expr arg2
    )
    {
        var path = ruleFor.Path;
        return new PathRules<T, TN>(path, ruleFor.Must.AndExpr(new CallExpr(func, [ruleFor.Get(), arg2])));
    }

    public static PathRules<T, TN> CallInbuilt<T, TN>(
        this RuleBuilder<T, TN> ruleFor,
        InbuiltFunction func,
        Expr arg1,
        Expr arg2
    )
    {
        var path = ruleFor.Path;
        return new PathRules<T, TN>(path, ruleFor.Must.AndExpr(new CallExpr(func, [arg1, arg2])));
    }

    public static PathRules<T, TN> Must<T, TN>(
        this RuleBuilder<T, TN> ruleFor,
        Func<Expr, BoolExpr> must
    )
    {
        var path = ruleFor.Path;
        return new PathRules<T, TN>(path, ruleFor.Must.AndExpr(must(ruleFor.Get()).Expr));
    }

    public static PathRules<T, TN> MustExpr<T, TN>(this RuleBuilder<T, TN> ruleFor, Expr must)
    {
        var path = ruleFor.Path;
        return new PathRules<T, TN>(path, ruleFor.Must.AndExpr(must));
    }

    public static PathRules<T, TN> Must<T, TN>(
        this RuleBuilder<T, TN> ruleFor,
        Func<NumberExpr, BoolExpr> must
    )
        where TN : struct, ISignedNumber<TN>
    {
        var path = ruleFor.Path;
        return new PathRules<T, TN>(path, ruleFor.Must.AndExpr(must(new NumberExpr(ruleFor.Get())).Expr));
    }

    public static PathRules<T, bool> Must<T>(
        this RuleBuilder<T, bool> ruleFor,
        Func<BoolExpr, BoolExpr> must
    )
    {
        var path = ruleFor.Path;
        return new PathRules<T, bool>(
            path,
            ruleFor.Must.AndExpr(must(new BoolExpr(ruleFor.Get())).Expr)
        );
    }

    public static PathRules<T, TN> WithMessage<T, TN>(
        this RuleAndBuilder<T, TN> ruleFor,
        string message
    )
    {
        return new PathRules<T, TN>(
            ruleFor.Path,
            new CallExpr(InbuiltFunction.WithMessage, [ruleFor.Must, new StringValue(message)])
        );
    }

    public static PathRules<T, TN> Min<T, TN>(
        this RuleBuilder<T, TN> ruleFor,
        TN value,
        bool exclusive = false
    )
    {
        return new PathRules<T, TN>(
            ruleFor.Path,
            ruleFor.Must.AndExpr(
                new CallExpr(
                    exclusive ? InbuiltFunction.Gt : InbuiltFunction.GtEq,
                    [ruleFor.Get(), value.ToExpr()]
                )
            )
        );
    }

    public static PathRules<T, TN> IfElse<T, TN>(
        this RuleBuilder<T, TN> ruleFor,
        BoolExpr ifExpr,
        Func<RuleBuilder<T, TN>, PathRule<T>> trueExpr,
        Func<RuleBuilder<T, TN>, PathRule<T>> falseExpr
    )
    {
        return new PathRules<T, TN>(
            ruleFor.Path,
            ruleFor.Must.AndExpr(
                new CallExpr(
                    InbuiltFunction.IfElse,
                    [ifExpr.Expr, trueExpr(ruleFor).Must, falseExpr(ruleFor).Must]
                )
            )
        );
    }

    public static PathRules<T, TN> When<T, TN>(this RuleBuilder<T, TN> ruleFor, BoolExpr ifExpr)
    {
        return new PathRules<T, TN>(
            ruleFor.Path,
            new CallExpr(
                InbuiltFunction.IfElse,
                [ifExpr.Expr, ruleFor.Must ?? true.ToExpr(), ExprValue.Null]
            )
        );
    }

    public static PathRules<T, TN> WhenExpr<T, TN>(this RuleBuilder<T, TN> ruleFor, Expr ifExpr)
    {
        return new PathRules<T, TN>(
            ruleFor.Path,
            new CallExpr(
                InbuiltFunction.IfElse,
                [ifExpr, ruleFor.Must ?? true.ToExpr(), ExprValue.Null]
            )
        );
    }

    public static PathRules<T, TN> Max<T, TN>(
        this RuleBuilder<T, TN> ruleFor,
        TN value,
        bool exclusive = false
    )
    {
        return new PathRules<T, TN>(
            ruleFor.Path,
            ruleFor.Must.AndExpr(
                new CallExpr(
                    exclusive ? InbuiltFunction.Lt : InbuiltFunction.LtEq,
                    [ruleFor.Get(), value.ToExpr()]
                )
            )
        );
    }
}
