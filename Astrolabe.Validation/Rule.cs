using System.Numerics;
using System.Runtime.InteropServices;
using Astrolabe.JSON;

namespace Astrolabe.Validation;

public interface RuleBuilder<T, TProp>;

public record SimpleRuleBuilder<T, TProp>(PathExpr Path) : RuleBuilder<T, TProp>;

public interface Rule<T>
{ 
    PathExpr Path { get; }
    IEnumerable<Expr> Musts { get; }
}

public interface RuleAndBuilder<T, TProp> : Rule<T>, RuleBuilder<T, TProp>;

public record Rules<T, TProp>(PathExpr Path, IEnumerable<Expr> Musts): RuleAndBuilder<T, TProp>;

public record RulesForEach<T>(PathExpr Path, Expr Index, IEnumerable<Rule<T>> Rules) : Rule<T>
{
    public IEnumerable<Expr> Musts => [];
}

public record ResolvedRule<T>(JsonPathSegments Path, Expr Must);

public static class RuleExtensions
{
    private static (PathExpr, IEnumerable<Expr>) GetBuilder<T, TProp>(this RuleBuilder<T, TProp> builder)
    {
        return builder switch
        {
            RuleAndBuilder<T, TProp> ruleAndBuilder => (ruleAndBuilder.Path, ruleAndBuilder.Musts),
            SimpleRuleBuilder<T, TProp> simpleRuleBuilder => (simpleRuleBuilder.Path, []),
            _ => throw new ArgumentOutOfRangeException(nameof(builder))
        };
    }
    public static Rules<T, TN> Must<T, TN>(this RuleBuilder<T, TN> ruleFor, Func<NumberExpr<TN>, BoolExpr> must) 
        where TN : struct, ISignedNumber<TN>
    {
        var (path, musts) = GetBuilder(ruleFor);
        return new Rules<T, TN>(path, musts.Append(must(new NumberExpr<TN>(new GetData(path))).Expr));
    }
    
    public static Rules<T, bool> Must<T>(this RuleBuilder<T, bool> ruleFor, Func<BoolExpr, BoolExpr> must)
    {
        var (path, musts) = GetBuilder(ruleFor);
        return new Rules<T, bool>(path, musts.Append(must(new BoolExpr(new GetData(path))).Expr));
    }
}
