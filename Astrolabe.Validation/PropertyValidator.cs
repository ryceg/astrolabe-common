using System.Linq.Expressions;
using System.Numerics;
using System.Text.Json;
using Astrolabe.Common;

namespace Astrolabe.Validation;

public class PropertyValidator<T, T2>(PathExpr? parentPath)
{
    public PathExpr? ParentPath { get; } = parentPath;

    public NumberExpr Index => new(parentPath!.Segment);

    public NumberExpr RunningIndex => new NumberExpr(new RunningIndex(Index.Expr));

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
    
    public RulesForEach<T> RuleForEach<TC>(Expression<Func<T2, IEnumerable<TC>>> expr, Func<PropertyValidator<T, TC>, Rule<T>> rules)
    {
        var arrayPath = MakePathExpr(expr.GetPropertyInfo().Name);
        var indexExpr = new IndexExpr();
        var childProps = new PropertyValidator<T, TC>(new PathExpr(indexExpr, arrayPath));
        return new RulesForEach<T>(arrayPath, indexExpr, rules(childProps));
    }

    public NumberExpr Sum<TObj>(Expression<Func<T2, IEnumerable<TObj>>> expr, Func<TypedPath<TObj>, NumberExpr> map)
    {
        var arrayPath = MakePathExpr(expr.GetPropertyInfo().Name);
        var indexExpr = new IndexExpr();
        var childProps = new TypedPath<TObj>(new PathExpr(indexExpr, arrayPath));
        return new NumberExpr(new CallExpr(InbuiltFunction.Sum, [new MapExpr(arrayPath, indexExpr, map(childProps).Expr)]));
    }
    
    public NumberExpr Count<TObj>(Expression<Func<T2, IEnumerable<TObj>>> expr)
    {
        var arrayPath = MakePathExpr(expr.GetPropertyInfo().Name);
        return new NumberExpr(new CallExpr(InbuiltFunction.Count, [new GetData(arrayPath)]));
    }

    public object RunningCount()
    {
        throw new NotImplementedException();
    }
}
