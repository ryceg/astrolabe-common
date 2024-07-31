namespace Astrolabe.Evaluator.Functions;

public static class FilterFunctionHandler
{
    public static readonly FunctionHandler Instance =
        new(Resolve, (e, x) => throw new NotImplementedException());

    public static EnvironmentValue<EvalExpr> Resolve(
        EvalEnvironment environment,
        CallExpr callableExpr
    )
    {
        var nextEnv = environment.ResolveExpr(callableExpr.Args[0]);
        return (
            nextEnv.Value switch
            {
                ArrayExpr ae
                    => nextEnv
                        .Env.EvaluateEach(ae.ValueExpr, (e, expr) => FilterElem(e.WithValue(expr)))
                        .Map(x => (EvalExpr)new ArrayExpr(x)),
                _ => FilterElem(nextEnv)
            }
        ).Map(x => (EvalExpr)new CallExpr("[", [x]));

        EnvironmentValue<EvalExpr> FilterElem(EnvironmentValue<EvalExpr> expand)
        {
            return expand.Value switch
            {
                _ => throw new NotImplementedException()
            };
        }
    }
}
