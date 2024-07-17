namespace Astrolabe.Evaluator;

using EvaluatedExprValue = EnvironmentValue<ExprValue>;

public static class Interpreter
{
    public static EnvironmentValue<Expr> ResolveExpr(this EvalEnvironment environment, Expr expr)
    {
        if (environment.TryGetReplacement(expr, out var already))
            return already;
        return expr switch
        {
            ExprValue or VarExpr or LambdaExpr => environment.WithExpr(expr),
            LetExpr v
                => environment.ResolveExpr(v.Value).WithReplacement(v.Variable).ResolveExpr(v.In),
            DotExpr dotExpr => DoDot(dotExpr),
            FilterExpr filterExpr => DoFilter(filterExpr),
            ResolveEval resolveExpr
                => environment.ResolveExpr(resolveExpr.Expr).Evaluate().AsExpr(),
            CallableExpr callExpr => DoCall(callExpr),
        };

        EnvironmentValue<Expr> DoFilter(FilterExpr filterExpr)
        {
            var (arrayEnv, arrayPath) = environment.ResolveExpr(filterExpr.Base);
            var (filterEnv, filterResolved) = arrayEnv.ResolveExpr(filterExpr.Filter);
            return (arrayPath, filterResolved) switch
            {
                (ExprValue { Value: DataPath dp }, ExprValue index)
                    => filterEnv.WithExpr(ExprValue.From(new IndexPath(index.AsInt(), dp))),
                _ => throw new NotImplementedException($"{arrayPath}[{filterResolved}]")
            };
        }

        // EnvironmentValue<Expr> DoMap(MapExpr mapExpr)
        // {
        //     var (arrayEnv, arrayPath) = environment.ResolveExpr(mapExpr.Array);
        //     var arrayExpr = arrayEnv.Evaluate(arrayPath).Value;
        //     var arrayVal = arrayExpr.AsValue();
        //     if (arrayVal.IsNull())
        //         return arrayEnv.WithExpr(ExprValue.Null);
        //     var arrayValue = arrayVal.AsArray();
        //     var elemVar = mapExpr.ElemPath;
        //     var arrayBasePath = arrayPath.AsValue().AsPath();
        //     var results = arrayEnv.EvaluateAll(
        //         Enumerable.Range(0, arrayValue.Count),
        //         (e, v) =>
        //             e.WithReplacement(elemVar, new ExprValue(new IndexPath(v, arrayBasePath)))
        //                 .ResolveExpr(mapExpr.MapTo)
        //                 .Single()
        //     );
        //
        //     return arrayEnv.WithExpr(new ArrayExpr(results.Value));
        // }

        EnvironmentValue<Expr> DoCall(CallableExpr callExpr)
        {
            var resolvedCall = environment
                .EvaluateAll(callExpr.Args, (e, ex) => e.ResolveExpr(ex).Single())
                .Map(callExpr.WithArgs);
            return resolvedCall.Env.ResolveCall(resolvedCall.Value);
        }

        EnvironmentValue<Expr> DoDot(DotExpr dotExpr)
        {
            var (pathEnv, pathValue) = environment.ResolveExpr(dotExpr.Base);
            var (segEnv, segValue) = pathEnv.ResolveExpr(dotExpr.Segment);
            var resolved = (pathValue, segValue) switch
            {
                (ExprValue { Value: DataPath dp }, ExprValue { Value: DataPath dp2 })
                    => PathAndPath(dp, dp2),
                _ => throw new NotImplementedException($"{pathValue}.{segValue}")
            };
            return resolved;

            EnvironmentValue<Expr> PathAndPath(DataPath dp, DataPath dp2)
            {
                return segEnv.EvaluateData(dp).Value.Value switch
                {
                    null => segEnv.WithNull().AsExpr(),
                    ObjectValue _ => segEnv.WithExpr(ExprValue.From(dp.Concat(dp2))),
                    ArrayValue av
                        => segEnv.WithExpr(
                            new ArrayExpr(
                                Enumerable
                                    .Range(0, av.Count)
                                    .Select(i => ExprValue.From(new IndexPath(i, dp).Concat(dp2)))
                            )
                        )
                };
            }
        }
    }

    public static EvaluatedExprValue Evaluate(this EnvironmentValue<Expr> envExpr)
    {
        return envExpr.Env.Evaluate(envExpr.Value);
    }

    public static EvaluatedExprValue Evaluate(this EvalEnvironment environment, Expr expr)
    {
        if (environment.TryGetReplacement(expr, out var already))
            return already.Evaluate();
        return expr switch
        {
            ExprValue { Value: DataPath dp } => environment.EvaluateData(dp),
            CallExpr callExpr => environment.EvaluateCall(callExpr),
            CallEnvExpr callEnvExpr => environment.EvaluateCall(callEnvExpr),
            ArrayExpr arrayExpr => EvalArray(arrayExpr),
            ExprValue v => environment.WithValue(v),
            _ => throw new ArgumentOutOfRangeException(expr.ToString())
        };

        EvaluatedExprValue EvalArray(ArrayExpr arrayExpr)
        {
            var elements = arrayExpr.ValueExpr.Aggregate(
                environment.WithEmpty<ExprValue>(),
                (acc, e) => acc.Env.Evaluate(e).AppendTo(acc)
            );
            return elements.Map(x =>
            {
                var rawElements = ArrayValue.From(x.Select(v => v.Value));
                return ExprValue.From(rawElements);
            });
        }
    }
}
