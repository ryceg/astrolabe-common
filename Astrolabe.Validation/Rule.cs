namespace Astrolabe.Validation;

public record RuleFor<T>(PathExpr Path);


public record Rule(PathExpr Path, BoolExpr Must);

public static class RuleExtensions
{
    public static Rule Must<TN>(this RuleFor<NumberExpr<TN>> ruleFor, Func<NumberExpr<TN>, BoolExpr> must) 
        where TN : struct
    {
        return new Rule(ruleFor.Path, must(new NumberExpr<TN>(new GetData(ruleFor.Path))));
    }
    
    public static Rule Must(this RuleFor<BoolExpr> ruleFor, Func<BoolExpr, BoolExpr> must)
    {
        return new Rule(ruleFor.Path, must(new BoolExpr(new GetData(ruleFor.Path))));
    }

}
