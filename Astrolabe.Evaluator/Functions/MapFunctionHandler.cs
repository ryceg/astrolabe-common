namespace Astrolabe.Evaluator.Functions;

public static class MapFunctionHandler
{
    public static FunctionHandler Instance = new FunctionHandler(
        Resolve,
        (e, x) => throw new NotImplementedException()
    );

    public static EnvironmentValue<EvalExpr> Resolve(
        EvalEnvironment environment,
        CallExpr callableExpr
    )
    {
        var nextEnv = environment.ResolveExpr(callableExpr.Args[0]);
        return MapElem(nextEnv);

        EnvironmentValue<EvalExpr> MapElem(EnvironmentValue<EvalExpr> expand)
        {
            return expand.Value switch
            {
                ArrayExpr ae
                    => expand
                        .Env.EvaluateEach(ae.ValueExpr, (e, expr) => MapElem(e.WithValue(expr)))
                        .Map(x => (EvalExpr)new ArrayExpr(x)),
                PathExpr { Path: var dp } when expand.Env.GetData(dp) is var data
                    => data == null
                        ? expand.Env.WithExpr(ValueExpr.Null)
                        : expand
                            .Env.WithBasePath(dp)
                            .ResolveExpr(callableExpr.Args[1])
                            .WithBasePath(expand.Env.BasePath),
                _ => throw new NotImplementedException()
            };
        }
    }
}
