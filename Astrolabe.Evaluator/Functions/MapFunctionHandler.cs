namespace Astrolabe.Evaluator.Functions;

public class MapFunctionHandler : FunctionHandler
{
    public EnvironmentValue<(ValueExpr, List<ValueExpr>)> Evaluate(
        IList<EvalExpr> args,
        EvalEnvironment environment
    )
    {
        throw new NotImplementedException();
    }

    public EnvironmentValue<EvalExpr> Resolve(CallExpr callableExpr, EvalEnvironment environment)
    {
        var nextEnv = environment.ResolveExpr(callableExpr.Args[0]);
        return MapElem(nextEnv);

        EnvironmentValue<EvalExpr> MapElem(EnvironmentValue<EvalExpr> expand)
        {
            return expand.Value switch
            {
                ArrayExpr ae
                    => nextEnv
                        .Env.EvaluateEach(ae.ValueExpr, (e, expr) => MapElem(e.WithValue(expr)))
                        .Map(x => (EvalExpr)new ArrayExpr(x)),
                ValueExpr ev when ev.MaybeDataPath() is { } dp
                    => expand.Env.EvaluateData(dp) switch
                    {
                        (var next, { Value: ArrayValue av })
                            => next.EvaluateEach(
                                    Enumerable.Range(0, av.Count),
                                    (e, i) =>
                                        e.WithBasePath(new IndexPath(i, dp))
                                            .ResolveExpr(callableExpr.Args[1])
                                )
                                .Map(x => (EvalExpr)new ArrayExpr(x))
                                .WithBasePath(next.BasePath),
                        (var next, { Value: ObjectValue ov })
                            => next.WithBasePath(dp)
                                .ResolveExpr(callableExpr.Args[1])
                                .WithBasePath(next.BasePath)
                    },
                _ => throw new NotImplementedException()
            };
        }
    }
}
