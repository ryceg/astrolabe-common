using System.Numerics;
using Astrolabe.Annotation;

namespace Astrolabe.Validation;

[JsonString]
public enum RuleType
{
    Single,
    Multi,
    ForEach
}

[JsonBaseType("type", typeof(SingleRule))]
[JsonSubType("Rule", typeof(SingleRule))]
[JsonSubType("Rules", typeof(MultiRule))]
public abstract record Rule(RuleType Type);

public record SingleRule(Expr Path, Expr Props, Expr Must) : Rule(RuleType.Single)
{
    public SingleRule WithProp(Expr key, Expr value)
    {
        return this with
        {
            Props = new CallExpr(InbuiltFunction.WithProperty, [key, value, Props])
        };
    }

    public SingleRule AndMust(Expr andMust)
    {
        return this with { Must = Must.AndExpr(andMust) };
    }

    public SingleRule When(Expr whenExpr)
    {
        return this with
        {
            Must = new CallExpr(InbuiltFunction.IfElse, [whenExpr, Must, ExprValue.Null,])
        };
    }

    public SingleRule WithMessage(Expr message)
    {
        return this with { Props = new CallExpr(InbuiltFunction.WithMessage, [message, Props]) };
    }
}

public record MultiRule(IEnumerable<Rule> Rules) : Rule(RuleType.Multi);

public record ForEachRule(Expr Path, Expr Index, Rule Rule) : Rule(RuleType.ForEach);

public record ResolvedRule(DataPath Path, Expr Must, IDictionary<string, object?> Properties);

public static class RuleExtensions
{
    public static List<DataPath> GetDataOrder(this IEnumerable<ResolvedRule> rules)
    {
        var dataOrder = new List<DataPath>();
        var processed = new HashSet<DataPath>();

        var ruleList = rules.ToList();
        var ruleLookup = ruleList.ToLookup(x => x.Path);
        ruleLookup.ToList().ForEach(x => AddRules(x.Key, x));
        return dataOrder;

        void AddRules(DataPath path, IEnumerable<ResolvedRule> pathRules)
        {
            if (!processed.Add(path))
                return;
            pathRules.ToList().ForEach(x => AddExprPaths(x.Must));
            dataOrder.Add(path);
        }

        void AddPath(DataPath path)
        {
            if (!processed.Contains(path))
            {
                dataOrder.Add(path);
            }
        }

        void AddExprPaths(Expr e)
        {
            switch (e)
            {
                case ExprValue { Value: DataPath fp }:
                    AddRules(fp, ruleLookup[fp]);
                    break;
                case CallExpr { Args: var args }:
                    foreach (var expr in args)
                    {
                        AddExprPaths(expr);
                    }
                    break;
            }
        }
    }
}
