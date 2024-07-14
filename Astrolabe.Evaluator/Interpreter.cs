namespace Astrolabe.Evaluator;

using EvaluatedExpr = EnvironmentValue<ExprValue>;

public static class Interpreter
{
    public static EnvironmentValue<Expr> ResolveExpr(this EvalEnvironment environment, Expr expr)
    {
        if (environment.TryGetReplacement(expr, out var already))
            return already;
        return expr switch
        {
            ExprValue v => environment.WithExpr(v),
            VarExpr v => environment.WithExpr(v),
            DotExpr dotExpr => DoDot(dotExpr),
            ResolveEval resolveExpr
                => environment.ResolveExpr(resolveExpr.Expr).Evaluate().Map(x => (Expr)x),
            CallExpr callExpr when DoArgs(callExpr.Args) is var (argEnv, args)
                => argEnv.WithExpr(callExpr with { Args = args.ToList() }),
            CallEnvExpr callExpr when DoArgs(callExpr.Args) is var (argEnv, args)
                => argEnv.WithExpr(callExpr with { Args = args.ToList() }),
            MapExpr mapExpr => DoMap(mapExpr)
        };

        EnvironmentValue<Expr> DoMap(MapExpr mapExpr)
        {
            var (arrayEnv, arrayPath) = environment.ResolveExpr(mapExpr.Array);
            var arrayExpr = arrayEnv.Evaluate(arrayPath).Value;
            var arrayVal = arrayExpr.AsValue();
            if (arrayVal.IsNull())
                return arrayEnv.WithExpr(ExprValue.Null);
            var arrayValue = arrayVal.AsArray();
            var elemVar = mapExpr.ElemPath;
            var arrayBasePath = arrayPath.AsValue().AsPath();
            var results = arrayEnv.EvaluateAll(
                Enumerable.Range(0, arrayValue.Count),
                (e, v) =>
                    e.WithReplacement(elemVar, new ExprValue(new IndexPath(v, arrayBasePath)))
                        .ResolveExpr(mapExpr.MapTo)
                        .Single()
            );

            return arrayEnv.WithExpr(new ArrayExpr(results.Value));
        }

        EnvironmentValue<IEnumerable<Expr>> DoArgs(IEnumerable<Expr> args)
        {
            return environment.EvaluateAll(args, (e, ex) => e.ResolveExpr(ex).Single());
        }

        EnvironmentValue<Expr> DoDot(DotExpr dotExpr)
        {
            var (pathEnv, pathValue) = environment.ResolveExpr(dotExpr.Base);
            var (segEnv, segValue) = pathEnv.ResolveExpr(dotExpr.Segment);
            var basePath = pathValue.AsValue().AsPath();
            var resolved = ExprValue.From(ValueExtensions.ApplyDot(basePath, segValue.AsValue()));
            return segEnv.WithExpr(resolved);
        }
    }

    public static EvaluatedExpr Evaluate(this EnvironmentValue<Expr> envExpr)
    {
        return envExpr.Env.Evaluate(envExpr.Value);
    }

    public static EvaluatedExpr Evaluate(this EvalEnvironment environment, Expr expr)
    {
        if (environment.TryGetReplacement(expr, out var already))
            return already.Evaluate();
        return expr switch
        {
            ExprValue { Value: DataPath dp } => environment.EvaluateData(dp),
            CallExpr callExpr => EvalCallExpr(callExpr),
            CallEnvExpr callEnvExpr => environment.EvaluateCall(callEnvExpr).Evaluate(),
            ArrayExpr arrayExpr
                => arrayExpr
                    .ValueExpr.Aggregate(
                        environment.WithEmpty<ExprValue>(),
                        (acc, e) => acc.Env.Evaluate(e).AppendTo(acc)
                    )
                    .Map(x => ExprValue.From(ArrayValue.From(x.Select(v => v.AsValue().Value)))),
            ExprValue v => environment.WithValue(v),
            _ => throw new ArgumentOutOfRangeException(expr.ToString())
        };

        EvaluatedExpr EvalCallExpr(CallExpr callExpr)
        {
            var argsList = callExpr.Args.ToList();
            if (argsList.Count == 2)
            {
                var (env1, v1) = environment.Evaluate(argsList[0]);
                var (env2, v2) = env1.Evaluate(argsList[1]);
                return callExpr.Function switch
                {
                    InbuiltFunction.Eq => DoEquality(false),
                    InbuiltFunction.Ne => DoEquality(true),
                    InbuiltFunction.And => DoAnd(),
                    InbuiltFunction.Or => DoOr(),
                    InbuiltFunction.Add
                    or InbuiltFunction.Divide
                    or InbuiltFunction.Minus
                    or InbuiltFunction.Multiply
                        => new EvaluatedExpr(env2, DoMathOp(callExpr.Function, v1, v2)),
                    InbuiltFunction.Eq
                    or InbuiltFunction.Ne
                    or InbuiltFunction.Gt
                    or InbuiltFunction.Lt
                    or InbuiltFunction.GtEq
                    or InbuiltFunction.LtEq
                        => CheckFailure(DoCompare(callExpr.Function, v1, v2), v1, v2),
                    _
                        => throw new ArgumentException(
                            $"Unknown InbuiltFunction: {callExpr.Function}"
                        )
                };

                EvaluatedExpr DoAnd()
                {
                    if (v1.IsEitherNull(v2))
                        return env2.WithNull();
                    return !v1.AsBool()
                        ? new EvaluatedExpr(env2, ExprValue.False)
                        : new EvaluatedExpr(env2, v2);
                }

                EvaluatedExpr DoOr()
                {
                    return v1.IsEitherNull(v2)
                        ? env2.WithNull()
                        : env2.WithValue(ExprValue.From(v1.AsBool() || v2.AsBool()));
                }

                EvaluatedExpr DoEquality(bool not)
                {
                    return v1.IsEitherNull(v2)
                        ? env2.WithNull()
                        : CheckFailure(
                            v1.AsEqualityCheck()!.Equals(v2.AsEqualityCheck()) ^ not,
                            v1,
                            v2
                        );
                }

                EvaluatedExpr CheckFailure(bool? boolResult, ExprValue actual, ExprValue failValue)
                {
                    return env2.BooleanResult(boolResult, callExpr, [actual, failValue]);
                }
            }

            if (argsList.Count == 1)
            {
                var (env1, v1) = environment.Evaluate(argsList[0]);
                return callExpr.Function switch
                {
                    InbuiltFunction.Count => DoAggregate(x => x.Count()),
                    InbuiltFunction.Sum => DoAggregate(x => x.Sum(ExprValue.AsDouble)),
                    InbuiltFunction.Not when v1.AsBool() is var b
                        => new EvaluatedExpr(env1, ExprValue.From(!b))
                };

                EvaluatedExpr DoAggregate<T>(Func<IEnumerable<object?>, T> aggFunc)
                {
                    var asList = v1.AsArray().Values.Cast<object?>().ToList();
                    return env1.WithExprValue(
                        asList.Any(x => x == null) ? ExprValue.Null : new ExprValue(aggFunc(asList))
                    );
                }
            }

            if (argsList.Count == 3)
            {
                return callExpr.Function switch
                {
                    InbuiltFunction.IfElse
                        => environment.Evaluate(argsList[0]).IfElse(argsList[1], argsList[2]),
                };
            }

            throw new ArgumentException("Wrong number of arguments");
        }
    }

    public static bool? DoCompare(InbuiltFunction compareType, ExprValue o1, ExprValue o2)
    {
        if (o1.IsEitherNull(o2))
            return null;
        int diff;
        if ((o1.MaybeLong(), o2.MaybeLong()) is ({ } l1, { } l2))
        {
            diff = l1.CompareTo(l2);
        }
        else
        {
            var d1 = o1.AsDouble();
            var d2 = o2.AsDouble();
            diff = d1.CompareTo(d2);
        }

        return compareType switch
        {
            InbuiltFunction.Eq => diff == 0,
            InbuiltFunction.Ne => diff != 0,
            InbuiltFunction.Gt => diff > 0,
            InbuiltFunction.GtEq => diff >= 0,
            InbuiltFunction.Lt => diff < 0,
            InbuiltFunction.LtEq => diff <= 0,
        };
    }

    public static ExprValue DoMathOp(InbuiltFunction op, ExprValue o1, ExprValue o2)
    {
        if (o1.IsEitherNull(o2))
            return ExprValue.Null;
        if ((o1.MaybeLong(), o2.MaybeLong()) is ({ } l1, { } l2))
        {
            return ExprValue.From(
                op switch
                {
                    InbuiltFunction.Add => l1 + l2,
                    InbuiltFunction.Minus => l1 - l2,
                    InbuiltFunction.Multiply => l1 * l2,
                    InbuiltFunction.Divide => (double)l1 / l2,
                }
            );
        }

        var d1 = o1.AsDouble();
        var d2 = o2.AsDouble();
        return ExprValue.From(
            op switch
            {
                InbuiltFunction.Add => d1 + d2,
                InbuiltFunction.Minus => d1 - d2,
                InbuiltFunction.Multiply => d1 * d2,
                InbuiltFunction.Divide => d1 / d2,
            }
        );
    }
}
