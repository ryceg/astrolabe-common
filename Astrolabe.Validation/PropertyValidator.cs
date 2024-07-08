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
        return new SimpleRuleBuilder<T, TProp>(MakePathExpr(propertyName));
    }

    protected PathExpr MakePathExpr(string propertyName)
    {
        return new PathExpr(JsonNamingPolicy.CamelCase.ConvertName(propertyName).ToExpr(), ParentPath);
    }

    public RuleBuilder<T, TN> RuleFor<TN>(Expression<Func<T2, TN?>> expr) where TN : struct 
    {
        var propertyInfo = expr.GetPropertyInfo();
        return RuleFor<TN>(propertyInfo.Name);
    }

    
    public RuleBuilder<T, TN> RuleFor<TN>(Expression<Func<T2, TN>> expr) where TN : struct 
    {
        var propertyInfo = expr.GetPropertyInfo();
        return RuleFor<TN>(propertyInfo.Name);
    }
    
    public RulesForEach<T> RulesFor<TC>(Expression<Func<T2, IEnumerable<TC>>> expr, Func<PropertyValidator<T, TC>, IEnumerable<Rule<T>>> rules)
    {
        var parentPath = MakePathExpr(expr.GetPropertyInfo().Name);
        var indexExpr = new IndexExpr();
        var childProps = new PropertyValidator<T, TC>(new PathExpr(indexExpr, parentPath));
        return new RulesForEach<T>(parentPath, indexExpr, rules(childProps));
    }

}
