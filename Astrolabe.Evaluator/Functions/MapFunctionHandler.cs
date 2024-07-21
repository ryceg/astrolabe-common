namespace Astrolabe.Evaluator.Functions;

public class MapFunctionHandler : FunctionHandler
{
    public EnvironmentValue<(ExprValue, List<ExprValue>)> Evaluate(
        IList<Expr> args,
        EvalEnvironment environment
    )
    {
        throw new NotImplementedException();
    }

    public EnvironmentValue<Expr> Resolve(CallableExpr callableExpr, EvalEnvironment environment)
    {
        var nextEnv = environment.ResolveExpr(callableExpr.Args[0]);
        return MapElem(nextEnv);

        EnvironmentValue<Expr> MapElem(EnvironmentValue<Expr> expand)
        {
            return expand.Value switch
            {
                CallExpr { Function: InbuiltFunction.Filter, Args: [var arg1] }
                    => MapElem(expand.Env.WithValue(arg1))
                        .Map(x => (Expr)new CallExpr(InbuiltFunction.Filter, [x])),
                CallExpr { Function: InbuiltFunction.IfElse, Args: [var arg1, var arg2, var arg3] }
                    => MapElem(expand.Env.WithValue(arg2))
                        .Map(x => (Expr)new CallExpr(InbuiltFunction.IfElse, [arg1, x, arg3])),
                ArrayExpr ae
                    => nextEnv
                        .Env.EvaluateEach(ae.ValueExpr, (e, expr) => MapElem(e.WithValue(expr)))
                        .Map(x => (Expr)new ArrayExpr(x)),
                ExprValue { Value: DataPath dp }
                    => expand.Env.EvaluateData(dp) switch
                    {
                        (var next, { Value: ArrayValue av })
                            => next.EvaluateEach(
                                    Enumerable.Range(0, av.Count),
                                    (e, i) =>
                                        e.WithBasePath(new IndexPath(i, dp))
                                            .ResolveExpr(callableExpr.Args[1])
                                )
                                .Map(x => (Expr)new ArrayExpr(x))
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
