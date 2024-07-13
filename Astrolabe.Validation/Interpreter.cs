using System.Collections;
using System.Collections.Immutable;

namespace Astrolabe.Validation;

using EvaluatedExpr = EvaluatedResult<ExprValue>;

public static class Interpreter
{
    public static EvaluatedResult<Expr> ResolveExpr(this EvalEnvironment environment, Expr expr)
    {
        if (environment.Replacements.TryGetValue(expr, out var already))
            return environment.WithExpr(already);
        return expr switch
        {
            WrappedExpr we => environment.ResolveExpr(we.Expr),
            ExprValue v => environment.WithExpr(v),
            VarExpr v => environment.WithExpr(v),
            DotExpr dotExpr => DoDot(dotExpr),
            GetExpr getExpr => DoGet(getExpr),
            CallExpr callExpr => DoCall(callExpr),
            MapExpr mapExpr => DoMap(mapExpr)
        };

        EvaluatedResult<Expr> DoMap(MapExpr mapExpr)
        {
            var (arrayEnv, arrayExpr) = environment.ResolveExpr(mapExpr.Array);
            var arrayVal = arrayExpr.AsValue();
            if (arrayVal.IsNull())
                return arrayEnv.WithExpr(ExprValue.Null);
            var arrayValue = arrayVal.AsEnumerable();
            var elemVar = mapExpr.ElemPath.Unwrap();
            var results = arrayEnv.EvaluateAll(
                arrayValue,
                (e, v) =>
                    e.WithReplacement(elemVar, v.FromPath!.ToExpr())
                        .ResolveExpr(mapExpr.MapTo)
                        .Single()
            );

            return arrayEnv.WithExpr(new ArrayExpr(results.Result));
        }

        EvaluatedResult<Expr> DoCall(CallExpr callExpr)
        {
            var allArgs = environment.EvaluateAll(
                callExpr.Args,
                (e, ex) => e.ResolveExpr(ex).Single()
            );
            return allArgs.Env.WithExpr(callExpr with { Args = allArgs.Result.ToList() });
        }

        EvaluatedResult<Expr> DoGet(GetExpr p)
        {
            var v1 = environment.ResolveExpr(p.Path);
            var segments = v1.Result.AsValue().AsPath();
            return v1.Env.WithExpr(v1.Env.GetData(segments));
        }

        EvaluatedResult<Expr> DoDot(DotExpr dotExpr)
        {
            var (pathEnv, pathValue) = environment.ResolveExpr(dotExpr.Base);
            var (segEnv, segValue) = pathEnv.ResolveExpr(dotExpr.Segment);
            var basePath = pathValue.AsValue().AsPath();
            var resolved = ValueExtensions.ApplyDot(basePath, segValue.AsValue()).ToExpr();
            return segEnv.WithExpr(resolved);
        }
    }

    public static EvaluatedResult<RuleFailure<T>?> EvaluateFailures<T>(
        this EvalEnvironment environment,
        ResolvedRule<T> rule
    )
    {
        var (outEnv, result) = environment.Evaluate(rule.Must);
        RuleFailure<T>? failure = null;
        if (result.IsFalse())
        {
            failure = new RuleFailure<T>(outEnv.Failures, outEnv.Message.AsString(), rule);
        }

        var resetEnv = outEnv with
        {
            Properties = ImmutableDictionary<string, object?>.Empty,
            Message = ExprValue.Null,
            Failures = [],
            FailedData = result.IsFalse() ? outEnv.FailedData.Add(rule.Path) : outEnv.FailedData
        };
        return resetEnv.WithResult(failure);
    }

    public static EvaluatedExpr Evaluate(this EvalEnvironment environment, Expr expr)
    {
        if (environment.Replacements.TryGetValue(expr, out var already))
            return environment.WithResult(already);
        return expr switch
        {
            ExprValue { FromPath: { } fp } when environment.FailedData.Contains(fp)
                => environment.WithResult(ExprValue.Null),
            CallExpr callExpr => EvalCallExpr(callExpr),
            ArrayExpr arrayExpr
                => arrayExpr
                    .ValueExpr.Aggregate(
                        environment.WithEmpty<ExprValue>(),
                        (acc, e) => acc.Env.Evaluate(e).AppendTo(acc)
                    )
                    .Map(x => x.ToExpr()),
            ExprValue v => environment.WithResult(v),
            _ => throw new ArgumentOutOfRangeException(expr.ToString())
        };

        EvaluatedExpr EvalCallExpr(CallExpr callExpr)
        {
            var argsList = callExpr.Args.ToList();
            if (argsList.Count == 2)
            {
                var (env1, v1) = environment.Evaluate(argsList[0]);
                if (callExpr.Function == InbuiltFunction.WithMessage)
                {
                    return env1.WithMessage(v1).Evaluate(argsList[1]);
                }

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

                EvaluatedExpr DoDot()
                {
                    var basePath = v1.AsPath();
                    return env2.WithValue(ValueExtensions.ApplyDot(basePath, v2));
                }

                EvaluatedExpr DoAnd()
                {
                    if (v1.IsEitherNull(v2))
                        return env2.WithNull();
                    return !v1.AsBool()
                        ? new EvaluatedExpr(env2, false.ToExpr())
                        : new EvaluatedExpr(env2, v2);
                }

                EvaluatedExpr DoOr()
                {
                    return v1.IsEitherNull(v2)
                        ? env2.WithNull()
                        : env2.WithValue(v1.AsBool() || v2.AsBool());
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
                    return env2.AddFailureIf(boolResult, callExpr.Function, actual, failValue)
                        .WithValue(boolResult);
                }
            }

            if (argsList.Count == 1)
            {
                var (env1, v1) = environment.Evaluate(argsList[0]);
                return callExpr.Function switch
                {
                    InbuiltFunction.Count => DoAggregate(x => x.Count()),
                    InbuiltFunction.Sum => DoAggregate(x => x.Sum(v => v.AsDouble())),
                    InbuiltFunction.Not when v1.AsBool() is var b
                        => new EvaluatedExpr(env1, (!b).ToExpr())
                };

                EvaluatedExpr DoAggregate<T>(Func<IEnumerable<ExprValue>, T> aggFunc)
                {
                    var asList = v1.AsEnumerable().ToList();
                    return env1.WithExprValue(
                        asList.Any(x => x.IsNull()) ? ExprValue.Null : aggFunc(asList).ToExpr()
                    );
                }
            }

            if (argsList.Count == 3)
            {
                return callExpr.Function switch
                {
                    InbuiltFunction.IfElse
                        => environment.Evaluate(argsList[0]).IfElse(argsList[1], argsList[2]),
                    InbuiltFunction.WithProperty => DoProperty()
                };

                EvaluatedExpr DoProperty()
                {
                    var keyResult = environment.Evaluate(argsList[0]);
                    var valueResult = keyResult.Env.Evaluate(argsList[1]);
                    return valueResult
                        .Env.WithProperty(keyResult.Result.AsString(), valueResult.Result.Value)
                        .Evaluate(argsList[2]);
                }
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
            return (
                op switch
                {
                    InbuiltFunction.Add => l1 + l2,
                    InbuiltFunction.Minus => l1 - l2,
                    InbuiltFunction.Multiply => l1 * l2,
                    InbuiltFunction.Divide => (double)l1 / l2,
                }
            ).ToExpr();
        }

        var d1 = o1.AsDouble();
        var d2 = o2.AsDouble();
        return (
            op switch
            {
                InbuiltFunction.Add => d1 + d2,
                InbuiltFunction.Minus => d1 - d2,
                InbuiltFunction.Multiply => d1 * d2,
                InbuiltFunction.Divide => d1 / d2,
            }
        ).ToExpr();
    }

    public static EvaluatedResult<IEnumerable<ResolvedRule<T>>> EvaluateRule<T>(
        this EvalEnvironment environment,
        Rule<T> rule
    )
    {
        return rule switch
        {
            RulesForEach<T> rulesForEach => DoRulesForEach(rulesForEach),
            PathRule<T> pathRule => DoPathRule(pathRule),
            MultiRule<T> multi => DoMultiRule(multi)
        };

        EvaluatedResult<IEnumerable<ResolvedRule<T>>> DoMultiRule(MultiRule<T> multiRule)
        {
            return environment.EvaluateAll(multiRule.Rules, EvaluateRule);
        }

        EvaluatedResult<IEnumerable<ResolvedRule<T>>> DoPathRule(PathRule<T> pathRule)
        {
            var (pathEnv, segments) = environment.ResolveExpr(pathRule.Path);
            var (mustEnv, must) = pathEnv.ResolveExpr(pathRule.Must);
            var (propsEnv, props) = mustEnv.ResolveExpr(pathRule.Props);
            var propsResult = propsEnv.Evaluate(props);
            return propsEnv
                .WithResult(
                    new ResolvedRule<T>(
                        ((ExprValue)segments).AsPath(),
                        must,
                        propsResult.Env.Properties
                    )
                )
                .Single();
        }

        EvaluatedResult<IEnumerable<ResolvedRule<T>>> DoRulesForEach(RulesForEach<T> rules)
        {
            var (pathEnv, collectionSeg) = environment.ResolveExpr(rules.Path);
            var indexExpr = rules.Index.Unwrap();
            var runningIndexExpr = new RunningIndex(indexExpr);
            var runningIndexOffset = pathEnv.Replacements.TryGetValue(
                runningIndexExpr,
                out var current
            )
                ? current.AsInt()
                : 0;
            var nextEnv = pathEnv.WithReplacement(runningIndexExpr, runningIndexOffset.ToExpr());

            var dataCollection = nextEnv.GetData(collectionSeg.AsValue().AsPath());
            if (dataCollection.Value is IEnumerable array)
            {
                return nextEnv.EvaluateAll(
                    Enumerable.Range(0, array.Cast<object>().Count()),
                    (env, index) =>
                    {
                        var envWithIndex = env.WithReplacement(indexExpr, index.ToExpr());
                        return envWithIndex
                            .EvaluateRule(rules.Rule)
                            .WithReplacement(
                                runningIndexExpr,
                                (runningIndexOffset + index + 1).ToExpr()
                            );
                    }
                );
            }

            throw new ArgumentException($"Not an array: {dataCollection?.GetType()}");
        }
    }
}
