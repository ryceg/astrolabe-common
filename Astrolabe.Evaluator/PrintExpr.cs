namespace Astrolabe.Evaluator;

public static class PrintExpr
{
    public static string Print(this Expr expr)
    {
        return expr switch
        {
            ExprValue { Value: null } => "null",
            ExprValue { Value: EmptyPath } => "$",
            ExprValue { Value: DataPath dp } => dp.ToPathString(),
            ArrayExpr arrayExpr
                => $"[{string.Join(", ", arrayExpr.ValueExpr.Select(x => x.Print()))}]",
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
}
