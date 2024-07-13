using System.Numerics;

namespace Astrolabe.Validation;

public interface RuleBuilder<T, TProp> : TypedPathExpr<T, TProp>
{
    Expr Path { get; }

    Expr Props { get; }

    Expr? Must { get; }
}

public record SimpleRuleBuilder<T, TProp>(Expr Path) : RuleBuilder<T, TProp>
{
    public Expr Expr => Path;
    public Expr Props => ExprValue.True;
    public Expr? Must => null;
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

    Expr Props { get; }
    Expr Must { get; }
}

public interface RuleAndBuilder<T, TProp> : Rule<T>, RuleBuilder<T, TProp>
{
    Expr Must { get; }
}

public record PathRules<T, TProp>(Expr Path, Expr Must, Expr Props)
    : RuleAndBuilder<T, TProp>,
        PathRule<T>
{
    public Expr Expr => Path;
}

public record RulesForEach<T>(Expr Path, Expr Index, Rule<T> Rule) : Rule<T>;

public record ResolvedRule<T>(DataPath Path, Expr Must, IDictionary<string, object?> Properties);

public static class RuleExtensions
{
    public static PathRules<T, TN> MapMust<T, TN>(
        this RuleBuilder<T, TN> ruleFor,
        Func<Expr?, Expr> apply
    )
    {
        return new PathRules<T, TN>(ruleFor.Path, apply(ruleFor.Must), ruleFor.Props);
    }

    public static RuleBuilder<T, TN> MapProps<T, TN>(
        this RuleBuilder<T, TN> ruleFor,
        Func<Expr, Expr> apply
    )
    {
        return new PathRules<T, TN>(ruleFor.Path, ruleFor.Must!, apply(ruleFor.Props));
    }

    public static List<DataPath> GetDataOrder<T>(this IEnumerable<ResolvedRule<T>> rules)
    {
        var dataOrder = new List<DataPath>();
        var processed = new HashSet<DataPath>();

        var ruleList = rules.ToList();
        var ruleLookup = ruleList.ToLookup(x => x.Path);
        ruleLookup.ToList().ForEach(x => AddRules(x.Key, x));
        return dataOrder;

        void AddRules(DataPath path, IEnumerable<ResolvedRule<T>> pathRules)
        {
            if (!processed.Add(path))
                return;
            pathRules.ToList().ForEach(x => AddExprPaths(x.Must));
            dataOrder.Add(path);
        }

        void AddPath(DataPath path)
        {
            if (!processed.Contains(path))
            {
                dataOrder.Add(path);
            }
        }

        void AddExprPaths(Expr e)
        {
            switch (e)
            {
                case ExprValue { FromPath: { } fp }:
                    AddRules(fp, ruleLookup[fp]);
                    break;
                case CallExpr { Args: var args }:
                    foreach (var expr in args)
                    {
                        AddExprPaths(expr);
                    }
                    break;
            }
        }
    }

    public static PathRules<T, TN> CallInbuilt<T, TN>(
        this RuleBuilder<T, TN> ruleFor,
        InbuiltFunction func,
        Expr arg2
    )
    {
        return ruleFor.MapMust(m => m.AndExpr(new CallExpr(func, [ruleFor.Get(), arg2])));
    }

    public static PathRules<T, TN> CallInbuilt<T, TN>(
        this RuleBuilder<T, TN> ruleFor,
        InbuiltFunction func,
        Expr arg1,
        Expr arg2
    )
    {
        return ruleFor.MapMust(m => m.AndExpr(new CallExpr(func, [arg1, arg2])));
    }

    public static PathRules<T, TN> Must<T, TN>(
        this RuleBuilder<T, TN> ruleFor,
        Func<Expr, BoolExpr> must
    )
    {
        var path = ruleFor.Path;
        return ruleFor.MapMust(m => m.AndExpr(must(ruleFor.Get()).Expr));
    }

    public static PathRules<T, TN> MustExpr<T, TN>(this RuleBuilder<T, TN> ruleFor, Expr must)
    {
        return ruleFor.MapMust(m => m.AndExpr(must));
    }

    public static PathRules<T, TN> Must<T, TN>(
        this RuleBuilder<T, TN> ruleFor,
        Func<NumberExpr, BoolExpr> must
    )
        where TN : struct, ISignedNumber<TN>
    {
        return ruleFor.MapMust(m => m.AndExpr(must(new NumberExpr(ruleFor.Get())).Expr));
    }

    public static PathRules<T, bool> Must<T>(
        this RuleBuilder<T, bool> ruleFor,
        Func<BoolExpr, BoolExpr> must
    )
    {
        return ruleFor.MapMust(m => m.AndExpr(must(new BoolExpr(ruleFor.Get())).Expr));
    }

    public static RuleBuilder<T, TN> WithMessage<T, TN>(
        this RuleBuilder<T, TN> ruleFor,
        string message
    )
    {
        return ruleFor.MapProps(x => x.WrapWithMessage(message.ToExpr()));
    }

    public static RuleBuilder<T, TN> WithProperty<T, TN>(
        this RuleBuilder<T, TN> ruleFor,
        string key,
        object? value
    )
    {
        return ruleFor.MapProps(x => x.WrapWithProperty(key.ToExpr(), value.ToExpr()));
    }

    public static PathRules<T, TN> Min<T, TN>(
        this RuleBuilder<T, TN> ruleFor,
        TN value,
        bool exclusive = false
    )
    {
        return ruleFor.MapMust(m =>
            m.AndExpr(
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
        return ruleFor.MapMust(m =>
            m.AndExpr(
                new CallExpr(
                    InbuiltFunction.IfElse,
                    [ifExpr.Expr, trueExpr(ruleFor).Must, falseExpr(ruleFor).Must]
                )
            )
        );
    }

    public static PathRules<T, TN> When<T, TN>(this RuleBuilder<T, TN> ruleFor, BoolExpr ifExpr)
    {
        return ruleFor.MapMust(m => new CallExpr(
            InbuiltFunction.IfElse,
            [ifExpr.Expr, m ?? true.ToExpr(), ExprValue.Null]
        ));
    }

    public static PathRules<T, TN> WhenExpr<T, TN>(this RuleBuilder<T, TN> ruleFor, Expr ifExpr)
    {
        return ruleFor.MapMust(m => new CallExpr(
            InbuiltFunction.IfElse,
            [ifExpr, m ?? true.ToExpr(), ExprValue.Null]
        ));
    }

    public static PathRules<T, TN> Max<T, TN>(
        this RuleBuilder<T, TN> ruleFor,
        TN value,
        bool exclusive = false
    )
    {
        return ruleFor.MapMust(m =>
            m.AndExpr(
                new CallExpr(
                    exclusive ? InbuiltFunction.Lt : InbuiltFunction.LtEq,
                    [ruleFor.Get(), value.ToExpr()]
                )
            )
        );
    }
}
