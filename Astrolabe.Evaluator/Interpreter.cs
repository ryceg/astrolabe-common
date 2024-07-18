namespace Astrolabe.Evaluator;

using EvaluatedExprValue = EnvironmentValue<ExprValue>;

public static class Interpreter
{
    public static EnvironmentValue<Expr> ResolveExpr(this EnvironmentValue<Expr> environment)
    {
        return environment.Env.ResolveExpr(environment.Value);
    }

    public static EnvironmentValue<Expr> ResolveExpr(this EvalEnvironment environment, Expr expr)
    {
        var already = environment.GetReplacement(expr);
        if (already != null)
            return environment.WithValue(already);
        return expr switch
        {
            ExprValue or VarExpr or LambdaExpr => environment.WithExpr(expr),
            ArrayExpr ae
                => environment
                    .EvaluateEach(ae.ValueExpr, ResolveExpr)
                    .Map(x => (Expr)new ArrayExpr(x.ToList())),
            LetExpr v
                => environment
                    .EvaluateForEach(
                        v.Vars,
                        (acc, nextVar) =>
                            acc.ResolveExpr(nextVar.Item2)
                                .Then(e => e.WithReplacement(nextVar.Item1, e.Value))
                                .Env
                    )
                    .ResolveExpr(v.In),
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
            return ResolveIndexFilter(filterEnv, arrayPath, filterResolved);
        }

        EnvironmentValue<Expr> ResolveIndexFilter(
            EvalEnvironment env,
            Expr arrayPath,
            Expr filterExpr
        )
        {
            var indexFilter = filterExpr.AsValue().AsInt();
            return arrayPath switch
            {
                ExprValue { Value: DataPath dp }
                    => env.WithExpr(ExprValue.From(new IndexPath(indexFilter, dp))),
                ArrayExpr ae => env.WithExpr(ae.ValueExpr.ToList()[indexFilter]),
                _ => throw new NotImplementedException($"{arrayPath}[{filterExpr}]")
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
            var (env, segment) = pathEnv.ResolveExpr(dotExpr.Segment);

            switch (pathValue)
            {
                case ExprValue { Value: DataPath dp }:
                {
                    var dataValue = env.EvaluateData(dp).Value;
                    return dataValue.Value switch
                    {
                        null => env.WithNull().AsExpr(),
                        ObjectValue _ when segment is ExprValue ev && ev.MaybeDataPath() is { } dp2
                            => env.WithExpr(ExprValue.From(dp.Concat(dp2))),
                        ArrayValue when segment is ExprValue ev && ev.MaybeInteger() is { } ind
                            => ResolveIndexFilter(env, pathValue, ev),
                        ArrayValue av
                            when segment is ExprValue ev
                                && ev.MaybeDataPath() is { } dp2
                                && VarExpr.MakeNew("i") is var varExpr
                            => ResolveArray(
                                env,
                                varExpr,
                                varExpr,
                                av.Count,
                                ind => ExprValue.From(new IndexPath(ind, dp).Concat(dp2))
                            ),
                        ArrayValue av when segment is LambdaExpr lambdaExpr
                            => ResolveArray(
                                env,
                                lambdaExpr.Variable,
                                lambdaExpr.Value,
                                av.Count,
                                i => ExprValue.From(new IndexPath(i, dp))
                            ),
                        _ => throw new NotImplementedException()
                    };
                }
                case ArrayExpr ae:
                {
                    var arrayVals = ae.ValueExpr.ToList();
                    return segment switch
                    {
                        LambdaExpr lambdaExpr
                            => ResolveArray(
                                env,
                                lambdaExpr.Variable,
                                lambdaExpr.Value,
                                ae.ValueExpr.Count(),
                                i => arrayVals[i]
                            ),
                        ExprValue ev
                            when ev.MaybeDataPath() is { } dp2
                                && VarExpr.MakeNew("i") is var varExpr
                            => ResolveArray(
                                    env,
                                    varExpr,
                                    varExpr,
                                    arrayVals.Count,
                                    i => new DotExpr(arrayVals[i], ExprValue.From(dp2))
                                )
                                .ResolveExpr()
                    };
                }
                default:
                    throw new NotImplementedException($"{pathValue}.{segment}");
            }
        }
    }

    private static EnvironmentValue<Expr> ResolveArray(
        EvalEnvironment env,
        VarExpr varExpr,
        Expr expr,
        int count,
        Func<int, Expr> elemExpr
    )
    {
        var range = Enumerable.Range(0, count);
        var indexVar = varExpr.Append("_index");
        var countVar = varExpr.Append("_count");
        var prevVar = env.GetReplacement(varExpr);
        var prevIndex = env.GetReplacement(indexVar);
        return env.EvaluateAll(range, (e, i) => ResolveElement(e, i).Single())
            .WithReplacement(varExpr, prevVar)
            .WithReplacement(indexVar, prevIndex)
            .Then(e => e.Env.WithValue((Expr)new ArrayExpr(e.Value)));

        EnvironmentValue<Expr> ResolveElement(EvalEnvironment e, int i)
        {
            return e.WithReplacement(varExpr, elemExpr(i))
                .WithReplacement(indexVar, ExprValue.From(i))
                .MapReplacement(
                    countVar,
                    total => ExprValue.From((total?.AsValue().AsInt() ?? 0) + 1)
                )
                .ResolveExpr(expr);
        }
    }

    public static EvaluatedExprValue Evaluate(this EnvironmentValue<Expr> envExpr)
    {
        return envExpr.Env.Evaluate(envExpr.Value);
    }

    public static EvaluatedExprValue ResolveAndEvaluate(this EvalEnvironment env, Expr expr)
    {
        return env.ResolveExpr(expr).Evaluate();
    }

    public static EvaluatedExprValue Evaluate(this EvalEnvironment environment, Expr expr)
    {
        var already = environment.GetReplacement(expr);
        if (already != null)
            return environment.WithValue(already.AsValue());
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
