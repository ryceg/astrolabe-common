using System.Linq.Expressions;
using System.Numerics;
using Astrolabe.Common;

namespace Astrolabe.Validation;

public class AbstractValidator<T>
{
    public void AddRules(ICollection<BoolExpr> rules)
    {
        
    }
    public NumberExpr RuleFor<TN>(Expression<Func<T, TN?>> expr) where TN : struct, ISignedNumber<TN> 
    {
        var propertyInfo = expr.GetPropertyInfo();
        return new PathExpr(false, propertyInfo.Name);
    }
    
    public NumberExpr RuleFor<TN>(Expression<Func<T, TN>> expr) where TN : struct, ISignedNumber<TN> 
    {
        var propertyInfo = expr.GetPropertyInfo();
        return new PathExpr(false, propertyInfo.Name);
    }

}