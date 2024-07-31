namespace Astrolabe.Evaluator.Functions;

public static class FilterFunctionHandler
{
    public static readonly FunctionHandler Instance = FunctionHandler.ResolveOnly(
        (e, c) =>
        {
            return c.Args switch
            {
                [var left, var right] when e.ResolveExpr(left) is var leftVal
                    => leftVal.Value switch
                    {
                        ArrayExpr ae
                            => e.EvalSelect(
                                    ae.Values,
                                    (e2, value) =>
                                        MapFunctionHandler
                                            .MapElem(e2.WithValue(value), right)
                                            .Map(x => new OptionalExpr(value, x))
                                )
                                .Map(x => new ArrayExpr(x))
                    }
            };

            // const [left, right] = call.args;
            // const [nextEnv, leftValue] = resolve(env, left);
            // if (leftValue.type === "array") {
            //     const filteredArray = mapAllEnv(nextEnv, leftValue.values, (e, v) =>
            //             mapEnv(resolveElem([e, v], right), (x) => optionalExpr(v, x)),
            //     );
            //     return mapEnv(filteredArray, (x) => arrayExpr(x));
            // }
            // throw new Error(
            //     "Function not implemented." + JSON.stringify(leftValue.type),
            // );
        }
    );

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
                        .Env.EvalSelect(ae.Values, (e, expr) => FilterElem(e.WithValue(expr)))
                        .Map(x => new ArrayExpr(x)),
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
