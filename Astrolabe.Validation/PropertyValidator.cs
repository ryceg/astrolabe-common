using System.Linq.Expressions;
using System.Numerics;
using System.Text.Json;
using Astrolabe.Common;

namespace Astrolabe.Validation;

public class PropertyValidator<T, T2>(PathExpr? parentPath)
{
    public PathExpr? ParentPath { get; } = parentPath;

    private RuleBuilder<T, TProp> RuleFor<TProp>(string propertyName)
    {
        return new RuleBuilder<T, TProp>(MakePathExpr(propertyName));
    }

    protected PathExpr MakePathExpr(string propertyName)
    {
        return new PathExpr(JsonNamingPolicy.CamelCase.ConvertName(propertyName).ToExpr(), ParentPath);
    }

    public RuleBuilder<T, NumberExpr<TN>> RuleFor<TN>(Expression<Func<T2, TN?>> expr) where TN : struct, ISignedNumber<TN> 
    {
        var propertyInfo = expr.GetPropertyInfo();
        return RuleFor<NumberExpr<TN>>(propertyInfo.Name);
    }

    
    public RuleBuilder<T, NumberExpr<TN>> RuleFor<TN>(Expression<Func<T2, TN>> expr) where TN : struct, ISignedNumber<TN> 
    {
        var propertyInfo = expr.GetPropertyInfo();
        return RuleFor<NumberExpr<TN>>(propertyInfo.Name);
    }
    
    public RuleBuilder<T, BoolExpr> RuleFor(Expression<Func<T2, bool>> expr) 
    {
        var propertyInfo = expr.GetPropertyInfo();
        return RuleFor<BoolExpr>(propertyInfo.Name);
    }

    public RuleBuilder<T, BoolExpr> RuleFor(Expression<Func<T2, bool?>> expr) 
    {
        var propertyInfo = expr.GetPropertyInfo();
        return RuleFor<BoolExpr>(propertyInfo.Name);
    }
    
    public RulesForEach<T> RulesFor<TC>(Expression<Func<T2, IEnumerable<TC>>> expr, Func<PropertyValidator<T, TC>, IEnumerable<Rule<T>>> rules)
    {
        var parentPath = MakePathExpr(expr.GetPropertyInfo().Name);
        var indexExpr = new IndexExpr();
        var childProps = new PropertyValidator<T, TC>(new PathExpr(indexExpr, parentPath));
        return new RulesForEach<T>(parentPath, indexExpr, rules(childProps));
    }

}
