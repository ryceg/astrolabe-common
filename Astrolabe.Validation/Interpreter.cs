using System.Collections;
using System.Text.Json;
using System.Text.Json.Nodes;
using Astrolabe.JSON;

namespace Astrolabe.Validation;

using EvaluatedExpr = EvaluatedResult<ExprValue>;

public static class Interpreter
{
    public static Expr ResolveExpr(Expr expr, EvalEnvironment environment)
    {
        if (environment.Replacements.TryGetValue(expr, out var already))
            return already;
        return expr switch
        {
            ExprValue v => v,
            VarExpr v => v,
            WrappedExpr we => ResolveExpr(we.Expr, environment),
            CallExpr callExpr
                => callExpr with
                {
                    Args = callExpr.Args.Select(x => ResolveExpr(x, environment)).ToList()
                }
        };
    }

    public static EvaluatedResult<RuleFailure<T>?> EvaluateFailures<T>(
        this EvalEnvironment environment,
        ResolvedRule<T> rule
    )
    {
        var propsEnv = environment.Evaluate(rule.Props);
        return propsEnv
            .Env.Evaluate(rule.Must)
            .Map(
                (x, ev) =>
                    x.IsFalse()
                        ? new RuleFailure<T>(
                            ev.Failures,
                            ev.Message.AsString(),
                            rule,
                            ev.Properties
                        )
                        : null
            );
    }

    public static EvaluatedExpr Evaluate(this EvalEnvironment environment, Expr expr)
    {
        if (environment.Replacements.TryGetValue(expr, out var already))
            return environment.WithResult(already);
        return expr switch
        {
            CallExpr callExpr => EvalCallExpr(callExpr),
            ArrayExpr arrayExpr
                => arrayExpr
                    .ValueExpr.Aggregate(
                        environment.WithEmpty<ExprValue>(),
                        (acc, e) => acc.Env.Evaluate(e).AppendTo(acc)
                    )
                    .Map(x => x.Select(v => v.Value).ToExpr()),
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
                    InbuiltFunction.Dot => DoDot(),
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
                    InbuiltFunction.Get => DoGet(),
                    InbuiltFunction.Count => DoAggregate(x => x.Count()),
                    InbuiltFunction.Sum => DoAggregate(x => x.Sum(ExprValue.AsDouble)),
                    InbuiltFunction.Not when v1.AsBool() is var b
                        => new EvaluatedExpr(env1, (!b).ToExpr())
                };

                EvaluatedExpr DoAggregate<T>(Func<IEnumerable<object?>, T> aggFunc)
                {
                    var asList = v1.AsEnumerable().ToList();
                    return env1.WithExprValue(
                        asList.Any(x => x == null) ? ExprValue.Null : aggFunc(asList).ToExpr()
                    );
                }

                EvaluatedExpr DoGet()
                {
                    var segments = v1.AsPath();
                    var outNode = segments.Traverse(env1.Data);
                    return env1.WithExprValue(ToValue(outNode).WithPath(segments));
                }
            }

            if (argsList.Count == 3)
            {
                return callExpr.Function switch
                {
                    InbuiltFunction.IfElse
                        => environment.Evaluate(argsList[0]).IfElse(argsList[1], argsList[2]),
                    InbuiltFunction.Map => DoMap(),
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

                EvaluatedExpr DoMap()
                {
                    var arrayEnv = environment.Evaluate(argsList[0]);
                    if (arrayEnv.Result.IsNull())
                        return arrayEnv.Map(_ => ExprValue.Null);
                    var arrayValue = arrayEnv.Result.AsEnumerable<JsonObject>();
                    var results = arrayEnv.Env.EvaluateAll(
                        arrayValue,
                        (e, v) =>
                            (e with { Data = v })
                                .WithReplacement(argsList[1], JsonPathSegments.Empty.ToExpr())
                                .Evaluate(argsList[2])
                                .Singleton()
                    );

                    return arrayEnv.Env.WithValue(results.Result);
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
            var (nextEnv, segments) = EvalPath(environment, pathRule.Path);
            return new EvaluatedResult<IEnumerable<ResolvedRule<T>>>(
                nextEnv,
                [
                    new ResolvedRule<T>(
                        segments,
                        ResolveExpr(pathRule.Must, nextEnv),
                        ResolveExpr(pathRule.Props, nextEnv)
                    )
                ]
            );
        }

        EvaluatedResult<IEnumerable<ResolvedRule<T>>> DoRulesForEach(RulesForEach<T> rules)
        {
            var (pathEnv, collectionSeg) = EvalPath(environment, rules.Path);
            var runningIndexExpr = new RunningIndex(rules.Index);
            var runningIndexOffset = pathEnv.Replacements.TryGetValue(
                runningIndexExpr,
                out var current
            )
                ? current.AsInt()
                : 0;
            var nextEnv = pathEnv.WithReplacement(runningIndexExpr, runningIndexOffset.ToExpr());

            var dataCollection = collectionSeg.Traverse(environment.Data);
            if (dataCollection is JsonArray array)
            {
                return nextEnv.EvaluateAll(
                    Enumerable.Range(0, array.Count),
                    (env, index) =>
                    {
                        var envWithIndex = env.WithReplacement(rules.Index, index.ToExpr());
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

    private static EvaluatedResult<JsonPathSegments> EvalPath(EvalEnvironment env, Expr pathExpr)
    {
        return env.Evaluate(ResolveExpr(pathExpr, env)).Map(x => x.AsPath());
    }

    public static ExprValue ToValue(JsonNode? node)
    {
        return node switch
        {
            null => ExprValue.Null,
            JsonArray ja => ja.ToExpr(),
            JsonObject jo => jo.ToExpr(),
            JsonValue v
                => v.GetValue<object>() switch
                {
                    JsonElement e
                        => e.ValueKind switch
                        {
                            JsonValueKind.False => false.ToExpr(),
                            JsonValueKind.True => true.ToExpr(),
                            JsonValueKind.String => e.GetString().ToExpr(),
                            JsonValueKind.Number
                                => e.TryGetInt64(out var l)
                                    ? l.ToExpr()
                                    : e.TryGetDouble(out var d)
                                        ? d.ToExpr()
                                        : ExprValue.Null,
                            _ => throw new ArgumentOutOfRangeException($"{e.ValueKind}-{e}")
                        },
                    var objValue => objValue.ToExpr()
                },
        };
    }
}
