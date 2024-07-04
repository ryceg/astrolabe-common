using System.Linq.Expressions;
using System.Numerics;
using Astrolabe.Common;

namespace Astrolabe.Validation;

public class AbstractValidator<T>
{
    public readonly List<Rule> Rules = [];
    
    public void AddRules(ICollection<Rule> rules)
    {
        Rules.AddRange(rules);
    }
    public RuleFor<NumberExpr<TN>> RuleFor<TN>(Expression<Func<T, TN?>> expr) where TN : struct, ISignedNumber<TN> 
    {
        var propertyInfo = expr.GetPropertyInfo();
        return new RuleFor<NumberExpr<TN>>((PathExpr)propertyInfo.Name);
    }
    
    public RuleFor<NumberExpr<TN>> RuleFor<TN>(Expression<Func<T, TN>> expr) where TN : struct, ISignedNumber<TN> 
    {
        var propertyInfo = expr.GetPropertyInfo();
        return new RuleFor<NumberExpr<TN>>((PathExpr) propertyInfo.Name);
    }
    
    public RuleFor<BoolExpr> RuleFor(Expression<Func<T, bool>> expr) 
    {
        var propertyInfo = expr.GetPropertyInfo();
        return new RuleFor<BoolExpr>((PathExpr) propertyInfo.Name);
    }

    public RuleFor<BoolExpr> RuleFor(Expression<Func<T, bool?>> expr) 
    {
        var propertyInfo = expr.GetPropertyInfo();
        return new RuleFor<BoolExpr>((PathExpr) propertyInfo.Name);
    }

}