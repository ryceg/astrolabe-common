namespace Astrolabe.Evaluator.Functions;

public class FilterFunctionHandler : FunctionHandler
{
    public EnvironmentValue<(ExprValue, List<ExprValue>)> Evaluate(
        IList<Expr> args,
        EvalEnvironment environment
    )
    {
        var arrayValue = environment.Evaluate(args[0]);
        return arrayValue.Value.Value switch
        {
            ArrayValue av
                => arrayValue.Map<(ExprValue, List<ExprValue>)>(_ =>
                    (
                        ExprValue.From(ArrayValue.From(av.Values.OfType<object>())),
                        [arrayValue.Value]
                    )
                ),
            var v => throw new NotImplementedException()
        };
    }

    public EnvironmentValue<Expr> Resolve(CallableExpr callableExpr, EvalEnvironment environment)
    {
        var nextEnv = environment.ResolveExpr(callableExpr.Args[0]);
        return (
            nextEnv.Value switch
            {
                ArrayExpr ae
                    => nextEnv
                        .Env.EvaluateEach(ae.ValueExpr, (e, expr) => FilterElem(e.WithValue(expr)))
                        .Map(x => (Expr)new ArrayExpr(x)),
                _ => FilterElem(nextEnv)
            }
        ).Map(x => (Expr)new CallExpr(InbuiltFunction.Filter, [x]));

        EnvironmentValue<Expr> FilterElem(EnvironmentValue<Expr> expand)
        {
            return expand.Value switch
            {
                ExprValue { Value: DataPath dp }
                    => expand.Env.EvaluateData(dp) switch
                    {
                        (var next, { Value: ArrayValue av })
                            => next.EvaluateEach(
                                    Enumerable.Range(0, av.Count),
                                    (e, i) =>
                                        e.WithBasePath(new IndexPath(i, dp))
                                            .ResolveExpr(callableExpr.Args[1])
                                            .Map(x => new CallExpr(
                                                InbuiltFunction.IfElse,
                                                [
                                                    x,
                                                    new ExprValue(new IndexPath(i, dp)),
                                                    ExprValue.Null
                                                ]
                                            ))
                                )
                                .Map(x => (Expr)new ArrayExpr(x))
                                .WithBasePath(next.BasePath),
                        var other => throw new NotImplementedException()
                    },
                _ => throw new NotImplementedException()
            };
        }
    }

    //     EnvironmentValue<Expr> DoFilter(FilterExpr filterExpr)
    //         {
    //             var (arrayEnv, arrayPath) = environment.ResolveExpr(filterExpr.Base);
    //             var (filterEnv, filterResolved) = arrayEnv.ResolveExpr(filterExpr.Filter);
    //             return ResolveIndexFilter(filterEnv, arrayPath, filterResolved);
    //         }
    //
    //         EnvironmentValue<Expr> ResolveIndexFilter(
    //             EvalEnvironment env,
    //             Expr arrayPath,
    //             Expr filterExpr
    //         )
    //         {
    //             var indexFilter = filterExpr.AsValue().AsInt();
    //             return arrayPath switch
    //             {
    //                 ExprValue { Value: DataPath dp }
    //                     => env.WithExpr(ExprValue.From(new IndexPath(indexFilter, dp))),
    //                 ArrayExpr ae => env.WithExpr(ae.ValueExpr.ToList()[indexFilter]),
    //                 _ => throw new NotImplementedException($"{arrayPath}[{filterExpr}]")
    //             };
    //         }
    //
    //
    //         EnvironmentValue<Expr> DoDot(DotExpr dotExpr)
    //         {
    //             var (pathEnv, pathValue) = environment.ResolveExpr(dotExpr.Base);
    //             var (env, segment) = pathEnv.ResolveExpr(dotExpr.Segment);
    //
    //             switch (pathValue)
    //             {
    //                 case ExprValue { Value: DataPath dp }:
    //                 {
    //                     var dataValue = env.EvaluateData(dp).Value;
    //                     return dataValue.Value switch
    //                     {
    //                         null => env.WithNull().AsExpr(),
    //                         ObjectValue _ when segment is ExprValue ev && ev.MaybeDataPath() is { } dp2
    //                             => env.WithExpr(ExprValue.From(dp.Concat(dp2))),
    //                         ArrayValue when segment is ExprValue ev && ev.MaybeInteger() is { } ind
    //                             => ResolveIndexFilter(env, pathValue, ev),
    //                         ArrayValue av
    //                             when segment is ExprValue ev
    //                                 && ev.MaybeDataPath() is { } dp2
    //                                 && VarExpr.MakeNew("i") is var varExpr
    //                             => ResolveArray(
    //                                 env,
    //                                 varExpr,
    //                                 varExpr,
    //                                 av.Count,
    //                                 ind => ExprValue.From(new IndexPath(ind, dp).Concat(dp2))
    //                             ),
    //                         ArrayValue av when segment is LambdaExpr lambdaExpr
    //                             => ResolveArray(
    //                                 env,
    //                                 lambdaExpr.Variable,
    //                                 lambdaExpr.Value,
    //                                 av.Count,
    //                                 i => ExprValue.From(new IndexPath(i, dp))
    //                             ),
    //                         _ => throw new NotImplementedException()
    //                     };
    //                 }
    //                 case ArrayExpr ae:
    //                 {
    //                     var arrayVals = ae.ValueExpr.ToList();
    //                     return segment switch
    //                     {
    //                         LambdaExpr lambdaExpr
    //                             => ResolveArray(
    //                                 env,
    //                                 lambdaExpr.Variable,
    //                                 lambdaExpr.Value,
    //                                 ae.ValueExpr.Count(),
    //                                 i => arrayVals[i]
    //                             ),
    //                         ExprValue ev
    //                             when ev.MaybeDataPath() is { } dp2
    //                                 && VarExpr.MakeNew("i") is var varExpr
    //                             => ResolveArray(
    //                                     env,
    //                                     varExpr,
    //                                     varExpr,
    //                                     arrayVals.Count,
    //                                     i => new DotExpr(arrayVals[i], ExprValue.From(dp2))
    //                                 )
    //                                 .ResolveExpr()
    //                     };
    //                 }
    //                 default:
    //                     throw new NotImplementedException($"{pathValue}.{segment}");
    //             }
    //         }
    //
    // }
    //
    //     private static EnvironmentValue<Expr> ResolveArray(
    //         EvalEnvironment env,
    //         VarExpr varExpr,
    //         Expr expr,
    //         int count,
    //         Func<int, Expr> elemExpr
    //     )
    //     {
    //         var range = Enumerable.Range(0, count);
    //         var indexVar = varExpr.Append("_index");
    //         var countVar = varExpr.Append("_count");
    //         var prevVar = env.GetReplacement(varExpr);
    //         var prevIndex = env.GetReplacement(indexVar);
    //         return env.EvaluateAll(range, (e, i) => ResolveElement(e, i).Single())
    //             .WithReplacement(varExpr, prevVar)
    //             .WithReplacement(indexVar, prevIndex)
    //             .Then(e => e.Env.WithValue((Expr)new ArrayExpr(e.Value)));
    //
    //         EnvironmentValue<Expr> ResolveElement(EvalEnvironment e, int i)
    //         {
    //             return e.WithReplacement(varExpr, elemExpr(i))
    //                 .WithReplacement(indexVar, ExprValue.From(i))
    //                 .MapReplacement(
    //                     countVar,
    //                     total => ExprValue.From((total?.AsValue().AsInt() ?? 0) + 1)
    //                 )
    //                 .ResolveExpr(expr);
    //         }
    //     }
    //
    // }
}
