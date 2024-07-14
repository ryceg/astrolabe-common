namespace Astrolabe.Validation.Typed;

public interface TypedRuleWrapper
{
    Rule ToRule();
}

public interface TypedRule<T> : TypedRuleWrapper;

public record TypedPathRule<T>(SingleRule Single) : TypedRule<T>
{
    Rule TypedRuleWrapper.ToRule() => Single;

    public TypedPathRule<T> WithProperty(string key, object? value)
    {
        return WithRuleExpr(r => r.WithProp(ExprValue.From(key), new ExprValue(value)));
    }

    public TypedExpr<T> Get()
    {
        return new SimpleTypedExpr<T>(Single.Path);
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

public record TypedRules<T>(IEnumerable<TypedRuleWrapper> Rules) : TypedRule<T>
{
    Rule TypedRuleWrapper.ToRule() => new MultiRule(Rules.Select(x => x.ToRule()));

    public TypedRules<T> AddRule(TypedRuleWrapper rule)
    {
        return new TypedRules<T>(Rules.Append(rule));
    }
}

public record TypedForEachRule<T>(ForEachRule ForEach) : TypedRule<T>
{
    Rule TypedRuleWrapper.ToRule() => ForEach;
}

public static class TypedRuleExtensions
{
    // public static PathRules<T, TN> CallInbuilt<T, TN>(
    //     this RuleBuilder<T, TN> ruleFor,
    //     InbuiltFunction func,
    //     Expr arg2
    // )
    // {
    //     return ruleFor.MapMust(m => m.AndExpr(new CallExpr(func, [ruleFor.Get(), arg2])));
    // }
    //
    // public static PathRules<T, TN> CallInbuilt<T, TN>(
    //     this RuleBuilder<T, TN> ruleFor,
    //     InbuiltFunction func,
    //     Expr arg1,
    //     Expr arg2
    // )
    // {
    //     return ruleFor.MapMust(m => m.AndExpr(new CallExpr(func, [arg1, arg2])));
    // }
    //
    // public static PathRules<T, TN> Must<T, TN>(
    //     this RuleBuilder<T, TN> ruleFor,
    //     Func<Expr, BoolExpr> must
    // )
    // {
    //     var path = ruleFor.Path;
    //     return ruleFor.MapMust(m => m.AndExpr(must(ruleFor.Get()).Expr));
    // }
    //
    // public static PathRules<T, TN> MustExpr<T, TN>(this RuleBuilder<T, TN> ruleFor, Expr must)
    // {
    //     return ruleFor.MapMust(m => m.AndExpr(must));
    // }
    //
    // public static PathRules<T, TN> Must<T, TN>(
    //     this RuleBuilder<T, TN> ruleFor,
    //     Func<NumberExpr, BoolExpr> must
    // )
    //     where TN : struct, ISignedNumber<TN>
    // {
    //     return ruleFor.MapMust(m => m.AndExpr(must(new NumberExpr(ruleFor.Get())).Expr));
    // }
    //
    // public static PathRules<T, bool> Must<T>(
    //     this RuleBuilder<T, bool> ruleFor,
    //     Func<BoolExpr, BoolExpr> must
    // )
    // {
    //     return ruleFor.MapMust(m => m.AndExpr(must(new BoolExpr(ruleFor.Get())).Expr));
    // }
    //
    // public static RuleBuilder<T, TN> WithMessage<T, TN>(
    //     this RuleBuilder<T, TN> ruleFor,
    //     string message
    // )
    // {
    //     return ruleFor.MapProps(x => x.WrapWithMessage(message.ToExpr()));
    // }
    //
    // public static RuleBuilder<T, TN> WithProperty<T, TN>(
    //     this RuleBuilder<T, TN> ruleFor,
    //     string key,
    //     object? value
    // )
    // {
    //     return ruleFor.MapProps(x => x.WrapWithProperty(key.ToExpr(), value.ToExpr()));
    // }
    //
    // public static PathRules<T, TN> Min<T, TN>(
    //     this RuleBuilder<T, TN> ruleFor,
    //     TN value,
    //     bool exclusive = false
    // )
    // {
    //     return ruleFor.MapMust(m =>
    //         m.AndExpr(
    //             new CallExpr(
    //                 exclusive ? InbuiltFunction.Gt : InbuiltFunction.GtEq,
    //                 [ruleFor.Get(), value.ToExpr()]
    //             )
    //         )
    //     );
    // }
    //
    // public static PathRules<T, TN> IfElse<T, TN>(
    //     this RuleBuilder<T, TN> ruleFor,
    //     BoolExpr ifExpr,
    //     Func<RuleBuilder<T, TN>, PathRule<T>> trueExpr,
    //     Func<RuleBuilder<T, TN>, PathRule<T>> falseExpr
    // )
    // {
    //     return ruleFor.MapMust(m =>
    //         m.AndExpr(
    //             new CallExpr(
    //                 InbuiltFunction.IfElse,
    //                 [ifExpr.Expr, trueExpr(ruleFor).Must, falseExpr(ruleFor).Must]
    //             )
    //         )
    //     );
    // }
    //
    // public static PathRules<T, TN> When<T, TN>(this RuleBuilder<T, TN> ruleFor, BoolExpr ifExpr)
    // {
    //     return ruleFor.MapMust(m => new CallExpr(
    //         InbuiltFunction.IfElse,
    //         [ifExpr.Expr, m ?? true.ToExpr(), ExprValue.Null]
    //     ));
    // }
    //
    // public static PathRules<T, TN> WhenExpr<T, TN>(this RuleBuilder<T, TN> ruleFor, Expr ifExpr)
    // {
    //     return ruleFor.MapMust(m => new CallExpr(
    //         InbuiltFunction.IfElse,
    //         [ifExpr, m ?? true.ToExpr(), ExprValue.Null]
    //     ));
    // }
    //
    // public static PathRules<T, TN> Max<T, TN>(
    //     this RuleBuilder<T, TN> ruleFor,
    //     TN value,
    //     bool exclusive = false
    // )
    // {
    //     return ruleFor.MapMust(m =>
    //         m.AndExpr(
    //             new CallExpr(
    //                 exclusive ? InbuiltFunction.Lt : InbuiltFunction.LtEq,
    //                 [ruleFor.Get(), value.ToExpr()]
    //             )
    //         )
    //     );
    // }
}
