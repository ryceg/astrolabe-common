namespace Astrolabe.Validation;

public record RuleNode(string Type, object? Value);


public static class RuleNodeExtensions
{
    public static RuleNode ToNode<T>(this Rule<T> rule)
    {
        return rule switch
        {
            MultiRule<T> multiRule => new RuleNode("Rules", 
                multiRule.Rules.Select(x => x.ToNode())),
            PathRule<T> p => new RuleNode("Rule", 
                new [] {MakePath(p.Path), p.Must.ToNode(), p.Props.ToNode()}),
            RulesForEach<T> r => new RuleNode("ForEach", new[] {MakePath(r.Path), r.Index.ToNode(), r.Rule.ToNode()}),
            _ => throw new ArgumentOutOfRangeException(nameof(rule))
        };
    }

    public static object? ToNode(this Expr rule)
    {
        return rule switch
        {
            WrappedExpr we => we.Expr.ToNode(),
            ArrayExpr arrayExpr => new RuleNode("Array", arrayExpr.ValueExpr.Select(x => x.ToNode())),
            CallExpr callExpr => new RuleNode("Call", new object?[] {callExpr.Function.ToString(), callExpr.Args.Select(x => x.ToNode())}),
            GetExpr ge => new RuleNode("Get", MakePath(ge.Path)),
            MapExpr mapExpr => new RuleNode("Map", new [] {mapExpr.Array.ToNode(), MakePath(mapExpr.ElemPath), mapExpr.MapTo.ToNode()}),
            RunningIndex runningIndex => new RuleNode("Running", runningIndex.CountExpr.ToNode()),
            VarExpr varExpr => new RuleNode("Var", varExpr.IndexId),
            DotExpr de => MakePath(de),
            ExprValue exprValue => exprValue.Value
        };
    }

    public static IEnumerable<object?> MakePath(Expr expr)
    {
        return AddToPath([], expr);
    }

    public static IEnumerable<object?> AddToPath(IEnumerable<object?> path, object? value)
    {
        return value switch
        {
            IEnumerable<object?> v => path.Concat(v),
            FieldPath fp => AddToPath(path, fp.Parent).Append(fp.Field),
            IndexPath ip => AddToPath(path, ip.Parent).Append(ip.Index),
            EmptyPath => path,
            _ => path.Append(value)
        };
    }

    public static IEnumerable<object?> AddToPath(IEnumerable<object?> path, Expr dotExpr)
    {
        return dotExpr switch
        {
            DotExpr {Base:var b, Segment: var s} => AddToPath(AddToPath(path, b), s.ToNode()),
            _ => AddToPath(path, dotExpr.ToNode())
        };
    }
}

