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
            var indexFilter = filterResolved.AsValue().AsInt();
            return arrayPath switch
            {
                ExprValue { Value: DataPath dp }
                    => filterEnv.WithExpr(ExprValue.From(new IndexPath(indexFilter, dp))),
                ArrayExpr ae => filterEnv.WithExpr(ae.ValueExpr.ToList()[indexFilter]),
                _ => throw new NotImplementedException($"{arrayPath}[{filterResolved}]")
            };
        }

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
            var resolvedMap = pathEnv.ResolveExpr(dotExpr.Segment);
            var (segEnv, segValue) = resolvedMap;

            var lambdaExpr = segValue as LambdaExpr;
            if (lambdaExpr == null)
            {
                var elemVar = VarExpr.MakeNew("_");
                lambdaExpr = new LambdaExpr(elemVar, new DotExpr(elemVar, segValue));
            }
            if (pathValue is ExprValue { Value: DataPath dp })
            {
                return segEnv.EvaluateData(dp).Value.Value switch
                {
                    null => segEnv.WithNull().AsExpr(),
                    ObjectValue _ when segValue is ExprValue { Value: DataPath dp2 }
                        => segEnv.WithExpr(ExprValue.From(dp.Concat(dp2))),
                    ArrayValue av
                        => MapArray(
                            av.Count,
                            resolvedMap,
                            i => ExprValue.From(new IndexPath(i, dp))
                        )
                };
            }

            if (pathValue is ArrayExpr ae)
            {
                var arrayVals = ae.ValueExpr.ToList();
                return MapArray(ae.ValueExpr.Count(), resolvedMap, i => arrayVals[i]);
            }

            EnvironmentValue<Expr> MapArray(
                int count,
                EnvironmentValue<Expr> mapEnvValue,
                Func<int, Expr> elemExpr
            )
            {
                var range = Enumerable.Range(0, count);
                var (env, mapExpr) = mapEnvValue;

                return env.EvaluateAll(range, (e, i) => MapLambdaElement(lambdaExpr, e, i).Single())
                    .Map(x => (Expr)new ArrayExpr(x));

                EnvironmentValue<Expr> MapLambdaElement(LambdaExpr lambda, EvalEnvironment e, int i)
                {
                    var (varExpr, v) = lambda;
                    return e.WithReplacement(varExpr, elemExpr(i))
                        .WithReplacement(new VarExpr(varExpr.Name + "_index"), ExprValue.From(i))
                        .MapReplacement(
                            new VarExpr(varExpr.Name + "_count"),
                            total => ExprValue.From((total?.AsValue().AsInt() ?? 0) + 1)
                        )
                        .ResolveExpr(v);
                }
            }

            throw new NotImplementedException($"{pathValue}.{segValue}");
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
