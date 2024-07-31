namespace Astrolabe.Evaluator.Functions;

public static class MapFunctionHandler
{
    public static readonly FunctionHandler Instance = FunctionHandler.ResolveOnly(Resolve);

    public static EnvironmentValue<EvalExpr> MapElem(
        EnvironmentValue<EvalExpr> expand,
        EvalExpr right
    )
    {
        return expand.Value switch
        {
            OptionalExpr oe
                => MapElem(expand.Map(_ => oe.Value), right).Map(x => oe with { Value = x }),
            ArrayExpr ae
                => expand
                    .Env.EvalSelect(ae.Values, (e, expr) => MapElem(e.WithValue(expr), right))
                    .Map(x => (EvalExpr)new ArrayExpr(x)),
            PathExpr { Path: var dp } when expand.Env.GetData(dp) is var data
                => data == null
                    ? expand.Env.WithValue(ValueExpr.Null)
                    : expand
                        .Env.WithBasePath(dp)
                        .ResolveExpr(right)
                        .WithBasePath(expand.Env.BasePath),
            _ => throw new NotImplementedException()
        };
    }

    private static EnvironmentValue<EvalExpr> Resolve(
        EvalEnvironment environment,
        CallExpr callableExpr
    )
    {
        var nextEnv = environment.ResolveExpr(callableExpr.Args[0]);
        return MapElem(nextEnv, callableExpr.Args[1]);
    }
}
