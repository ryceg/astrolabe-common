namespace Astrolabe.Validation;

public record RuleFor<T>(PathExpr Path) where T : Expr;


public record Rule(PathExpr Path, BoolExpr Must);

public static class RuleExtensions
{
    public static Rule Must(this RuleFor<NumberExpr> ruleFor, Func<NumberExpr, BoolExpr> must)
    {
        return new Rule(ruleFor.Path, must(new NumberExpr(ruleFor.Path)));
    }
    
    public static Rule Must(this RuleFor<BoolExpr> ruleFor, Func<BoolExpr, BoolExpr> must)
    {
        return new Rule(ruleFor.Path, must(new BoolExpr(ruleFor.Path)));
    }

}
