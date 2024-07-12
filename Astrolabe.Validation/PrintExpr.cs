namespace Astrolabe.Validation;

public static class PrintExpr
{
    public static string Print(this Expr expr)
    {
        return expr switch
        {
            WrappedExpr wrapped => wrapped.Expr.Print(),
            GetExpr { Path: var path } => path.Print(),
            ExprValue { Value: null } => "null",
            ExprValue { Value: EmptyPath } => "$",
            ExprValue { Value: DataPath dp } => dp.ToPathString(),
            ArrayExpr arrayExpr
                => $"[{string.Join(", ", arrayExpr.ValueExpr.Select(x => x.Print()))}]",
            ExprValue { Value: var v, FromPath: FieldPath fp } => $"({fp.Field}:{v})",
            ExprValue { Value: var v } => $"{v}",
            CallExpr { Function: InbuiltFunction.IfElse, Args: var a }
                when a.ToList() is [var ifE, var t, var f]
                => $"{ifE.Print()} ? {t.Print()} : {f.Print()}",
            CallExpr callExpr
                when InfixFunc(callExpr.Function) is { } op
                    && callExpr.Args.ToList() is [var v1, var v2]
                => $"{v1.Print()}{op}{v2.Print()}",
            CallExpr callExpr
                => $"{callExpr.Function}({string.Join(", ", callExpr.Args.Select(x => x.Print()))})",
            _ => expr.ToString()!,
        };
    }

    public static string? InfixFunc(InbuiltFunction func)
    {
        return func switch
        {
            InbuiltFunction.Eq => " = ",
            InbuiltFunction.Lt => " < ",
            InbuiltFunction.LtEq => " <= ",
            InbuiltFunction.Gt => " > ",
            InbuiltFunction.GtEq => " >= ",
            InbuiltFunction.Ne => " <> ",
            InbuiltFunction.And => " and ",
            InbuiltFunction.Or => " or ",
            InbuiltFunction.Add => " + ",
            InbuiltFunction.Minus => " - ",
            InbuiltFunction.Multiply => " * ",
            InbuiltFunction.Divide => " / ",
            _ => null
        };
    }

    public static string Print<T>(this ResolvedRule<T> rule)
    {
        return $"ResolvedRule " + rule.Path + " " + rule.Must.Print();
    }

    public static string Print<T>(this Rule<T> rule)
    {
        return rule switch
        {
            MultiRule<T> multiRule
                => $"[\n{string.Join("\n", multiRule.Rules.Select(x => x.Print()))}\n]",
            PathRule<T> pathRule => $"Rule {pathRule.Path.Print()}: {pathRule.Must.Print()}",
            RulesForEach<T> rulesForEach
                => $"RulesForEach {rulesForEach.Path.Print()} {rulesForEach.Index.Print()} {rulesForEach.Rule.Print()}",
            _ => throw new ArgumentOutOfRangeException(nameof(rule))
        };
    }
}
