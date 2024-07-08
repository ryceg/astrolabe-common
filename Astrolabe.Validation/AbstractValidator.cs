using System.Linq.Expressions;
using System.Numerics;
using System.Text.Json;
using Astrolabe.Common;

namespace Astrolabe.Validation;

public class AbstractValidator<T>() : PropertyValidator<T, T>(null)
{
    public readonly List<Rule<T>> Rules = [];
    
    public void AddRules(ICollection<Rule<T>> rules)
    {
        Rules.AddRange(rules);
    }
    
    public PathExpr? ParentPath => null;
}