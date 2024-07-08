using Astrolabe.JSON;

namespace Astrolabe.Validation;

public record RuleBuilder<T, TProp>(PathExpr Path);


public interface Rule<T>;
public record SingleRule<T>(PathExpr Path, Expr Must): Rule<T>;
public record RulesForEach<T>(PathExpr Path, Expr Index, IEnumerable<Rule<T>> Rules): Rule<T>;

public record ResolvedRule<T>(JsonPathSegments Path, Expr Must);

public static class RuleExtensions
{
    public static SingleRule<T> Must<T, TN>(this RuleBuilder<T, NumberExpr<TN>> ruleFor, Func<NumberExpr<TN>, BoolExpr> must) 
        where TN : struct
    {
        return new SingleRule<T>(ruleFor.Path, must(new NumberExpr<TN>(new GetData(ruleFor.Path))).Expr);
    }
    
    public static SingleRule<T> Must<T>(this RuleBuilder<T, BoolExpr> ruleFor, Func<BoolExpr, BoolExpr> must)
    {
        return new SingleRule<T>(ruleFor.Path, must(new BoolExpr(new GetData(ruleFor.Path))).Expr);
    }

    public static SingleRule<T> Constrained<T, TProp>(this RuleBuilder<T, TProp> ruleFor)
    {
        return new SingleRule<T>(ruleFor.Path, new ConstraintExpr(ruleFor.Path));
    }
    
    public static SingleRule<T> Constraint<T, TN>(this RuleBuilder<T, NumberExpr<TN>> ruleFor, double lower, double upper) 
        where TN : struct
    {
        return new SingleRule<T>(ruleFor.Path, new DefineConstraintExpr(ruleFor.Path, new BoolValue(true), lower.ToExpr(), upper.ToExpr(), 
            new BoolValue(false), new BoolValue(true)));
    }
}
