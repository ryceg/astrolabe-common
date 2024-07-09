using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using Astrolabe.JSON;

namespace Astrolabe.Validation;

public static class Interpreter
{
    public static PathExpr ResolvePath(PathExpr pathExpr, EvalEnvironment environment)
    {
        return new PathExpr(ResolveExpr(pathExpr.Segment, environment),
            pathExpr.Parent != null ? ResolvePath(pathExpr.Parent, environment) : null);
    }

    public static Expr ResolveExpr(Expr expr, EvalEnvironment environment)
    {
        if (environment.Evaluated.TryGetValue(expr, out var already))
            return already;
        return expr switch
        {
            ExprValue v => v,
            GetData getData => new GetData(Path: ResolvePath(getData.Path, environment)),
            MapExpr mapExpr => ResolveMap(mapExpr),
            CallExpr callExpr => callExpr with
            {
                Args = callExpr.Args.Select(x => ResolveExpr(x, environment)).ToList()
            }
        };

        Expr ResolveMap(MapExpr mapExpr)
        {
            var (nextEnv, collectionSeg) = EvalPath(environment, mapExpr.Path, JsonPathSegments.Empty);
            var dataCollection = collectionSeg.Traverse(environment.Data);
            return dataCollection switch
            {
                JsonArray array => new ArrayExpr(Enumerable.Range(0, array.Count).Select(i =>
                {
                    var indexedEnv = nextEnv.WithExprValue(mapExpr.Index, i.ToExpr());
                    return ResolveExpr(mapExpr.Value, indexedEnv);
                }))
            };
        }
    }

    public static EvaluatedExpr Evaluate(this EvalEnvironment environment, Expr expr)
    {
        if (environment.Evaluated.TryGetValue(expr, out var already))
            return new EvaluatedExpr(environment, already);
        return expr switch
        {
            CallExpr callExpr => EvalCallExpr(callExpr),
            GetData getData => DoGetData(getData),
            ArrayExpr arrayExpr => arrayExpr.ValueExpr
                .Aggregate(environment.WithResult(Enumerable.Empty<ExprValue>()),
                    (acc, e) => acc.Env.Evaluate(e).AppendTo(acc))
                .ToValue(x => new ArrayValue(x)),
            ExprValue v => new EvaluatedExpr(environment, v),
            _ => throw new ArgumentOutOfRangeException(expr.ToString())
        };

        EvaluatedExpr DoGetData(GetData fromPath)
        {
            return GetValueFromPath(environment, fromPath.Path);
        }

        EvaluatedExpr EvalCallExpr(CallExpr callExpr)
        {
            var argsList = callExpr.Args.ToList();
            if (argsList.Count == 2)
            {
                var (env1, v1) = environment.Evaluate(argsList[0]);
                var (env2, v2) = env1.Evaluate(argsList[1]);
                if (v1 is NullValue || v2 is NullValue)
                    return new EvaluatedExpr(environment, ExprValue.Null);

                return callExpr.Function switch
                {
                    InbuiltFunction.Eq when v1 is BoolValue or StringValue => ApplyDefaultMessage(v1 == v2, v1, v2),
                    InbuiltFunction.Ne when v1 is BoolValue or StringValue => ApplyDefaultMessage(v1 != v2, v1, v2),
                    InbuiltFunction.And => DoAnd(),
                    InbuiltFunction.Or => DoOr(),
                    InbuiltFunction.Add or InbuiltFunction.Divide or InbuiltFunction.Minus or InbuiltFunction.Multiply
                        => new EvaluatedExpr(env2, DoMathOp(callExpr.Function, v1, v2)),
                    InbuiltFunction.Eq or InbuiltFunction.Ne or InbuiltFunction.Gt or InbuiltFunction.Lt
                        or InbuiltFunction.GtEq or InbuiltFunction.LtEq =>
                        ApplyDefaultMessage(DoCompare(callExpr.Function, v1, v2), v1, v2),
                    InbuiltFunction.WithMessage => DoWithMessage(),
                    _ => throw new ArgumentException($"Unknown InbuiltFunction: {callExpr.Function}")
                };

                EvaluatedExpr DoAnd()
                {
                    if (!v1.AsBool())
                        return new EvaluatedExpr(env2 with { Failure = env1.Failure },
                            false.ToExpr());
                    return new EvaluatedExpr(env2, v2);
                }

                EvaluatedExpr DoOr()
                {
                    return new EvaluatedExpr(
                        env2 with { Failure = env2.Failure ?? env1.Failure },
                        (v1.AsBool() || v2.AsBool()).ToExpr());
                }

                EvaluatedExpr DoWithMessage()
                {
                    return v1.AsBool()
                        ? new EvaluatedExpr(env2, v1)
                        : new EvaluatedExpr(env2 with { Failure = env2.Failure! with { Message = v2 } }, v1);
                }

                EvaluatedExpr ApplyDefaultMessage(bool boolResult, ExprValue actual, ExprValue failValue)
                {
                    return boolResult
                        ? new EvaluatedExpr(env2, boolResult.ToExpr())
                        : new EvaluatedExpr(
                            env2 with { Failure = new Failure(ExprValue.Null, callExpr.Function, actual, failValue) },
                            false.ToExpr());
                }
            }

            if (argsList.Count == 1)
            {
                var (env1, v1) = environment.Evaluate(argsList[0]);
                return callExpr.Function switch
                {
                    InbuiltFunction.Count => env1.WithValue(((ArrayValue)v1).Values.Count().ToExpr()),
                    InbuiltFunction.Sum => env1.WithValue(((ArrayValue)v1).Values.OfType<NumberValue>()
                        .Sum(x => x.AsDouble()).ToExpr()),
                    InbuiltFunction.Not when v1.AsBool() is var b => new EvaluatedExpr(env1, (!b).ToExpr())
                };
            }

            if (argsList.Count == 3)
            {
                return callExpr.Function switch
                {
                    InbuiltFunction.IfElse => environment.Evaluate(argsList[0]).IfElse(argsList[1], argsList[2])
                };
            }

            throw new ArgumentException("Wrong number of arguments");
        }
    }

    public static bool DoCompare(InbuiltFunction compareType, ExprValue o1, ExprValue o2)
    {
        int diff;
        if (o1 is NumberValue { LongValue: { } l1 } && o2 is NumberValue { LongValue: { } l2 })
        {
            diff = l1.CompareTo(l2);
        }
        else
        {
            double d1 = ((NumberValue)o1).AsDouble();
            double d2 = ((NumberValue)o2).AsDouble();
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
        if (o1 is NumberValue { LongValue: { } l1 } &&
            o2 is NumberValue { LongValue: { } l2 })
        {
            return (op switch
            {
                InbuiltFunction.Add => l1 + l2,
                InbuiltFunction.Minus => l1 - l2,
                InbuiltFunction.Multiply => l1 * l2,
                InbuiltFunction.Divide => (double)l1 / l2,
            }).ToExpr();
        }

        if (o1 is NumberValue nv1 && o2 is NumberValue nv2)
        {
            var d1 = nv1.AsDouble();
            var d2 = nv2.AsDouble();
            return (op switch
            {
                InbuiltFunction.Add => d1 + d2,
                InbuiltFunction.Minus => d1 - d2,
                InbuiltFunction.Multiply => d1 * d2,
                InbuiltFunction.Divide => d1 / d2,
            }).ToExpr();
        }

        throw new ArgumentException($"MathOp {op} {o1.GetType()}-{o2.GetType()}");
    }

    public static EvaluatedResult<T> WithResult<T>(this EvalEnvironment env, T result)
    {
        return new EvaluatedResult<T>(env, result);
    }

    public static EvaluatedExpr WithValue(this EvalEnvironment env, ExprValue value)
    {
        return new EvaluatedExpr(env, value);
    }

    public static EvaluatedExpr ToValue<T>(this EvaluatedResult<T> envResult, Func<T, ExprValue> mapValue)
    {
        return envResult.Env.WithValue(mapValue(envResult.Result));
    }

    public static EvaluatedResult<IEnumerable<T>> AppendTo<T>(this EvaluatedResult<IEnumerable<T>> envResult,
        EvaluatedResult<IEnumerable<T>> other)
    {
        return envResult with { Result = other.Result.Concat(envResult.Result) };
    }

    public static EvaluatedResult<IEnumerable<ExprValue>> AppendTo(this EvaluatedExpr acc,
        EvaluatedResult<IEnumerable<ExprValue>> other)
    {
        return acc.Env.WithResult(other.Result.Append(acc.Value));
    }

    public static EvaluatedResult<IEnumerable<ResolvedRule<T>>> EvaluateRule<T>(this EvalEnvironment environment,
        Rule<T> rule)
    {
        return rule switch
        {
            RulesForEach<T> rulesForEach => DoRulesForEach(rulesForEach),
            PathRule<T> pathRule => DoPathRule(pathRule),
        };

        EvaluatedResult<IEnumerable<ResolvedRule<T>>> DoPathRule(PathRule<T> pathRule)
        {
            var (nextEnv, segments) = EvalPath(environment, ResolvePath(pathRule.Path, environment),
                JsonPathSegments.Empty);
            return new EvaluatedResult<IEnumerable<ResolvedRule<T>>>(nextEnv,
                [new ResolvedRule<T>(segments, ResolveExpr(pathRule.Must, environment))]);
        }

        EvaluatedResult<IEnumerable<ResolvedRule<T>>> DoRulesForEach(RulesForEach<T> rules)
        {
            var (_nextEnv, collectionSeg) = EvalPath(environment, rules.Path, JsonPathSegments.Empty);
            var runningIndexExpr = new RunningIndex(rules.Index);
            var runningIndexOffset = environment.Evaluated.TryGetValue(runningIndexExpr, out var current)
                ? ((NumberValue) current).ToTruncated() : 0;
            var nextEnv = _nextEnv.WithExprValue(runningIndexExpr, runningIndexOffset.ToExpr());

            var dataCollection = collectionSeg.Traverse(environment.Data);
            if (dataCollection is JsonArray array)
            {
                return Enumerable.Range(0, array.Count).Aggregate(
                    nextEnv.WithResult<IEnumerable<ResolvedRule<T>>>([]),
                    (acc, index) =>
                    {
                        var envWithIndex = acc.Env.WithExprValue(rules.Index, index.ToExpr());
                        return rules.Rules.Aggregate(acc with { Env = envWithIndex },
                            (acc2, r) => 
                                acc2.Env.EvaluateRule(r).WithExprValue(runningIndexExpr, (NumberValue) (runningIndexOffset + index + 1)).AppendTo(acc2));
                    });
            }

            throw new ArgumentException($"Not an array: {dataCollection?.GetType()}");
        }
    }

    private static (EvalEnvironment, JsonPathSegments) EvalPath(EvalEnvironment env, PathExpr pathExpr,
        JsonPathSegments parentSegments)
    {
        if (pathExpr.Parent != null)
        {
            var withPare = EvalPath(env, pathExpr.Parent, parentSegments);
            env = withPare.Item1;
            parentSegments = withPare.Item2;
        }

        var (nextEnv, seg) = env.Evaluate(pathExpr.Segment);
        return seg switch
        {
            StringValue { Value: var s } => (nextEnv, parentSegments.Field(s)),
            NumberValue { LongValue: { } l } => (nextEnv, parentSegments.Index((int)l)),
            _ => throw new ArgumentException($"{seg}")
        };
    }

    public static ExprValue ToValue(JsonNode? node)
    {
        return node switch
        {
            null => ExprValue.Null,
            JsonArray ja => new ArrayValue(ja.Select(ToValue)),
            JsonObject jo => new ObjectValue(jo),
            JsonValue v => v.GetValue<object>() switch
            {
                bool b => new BoolValue(b),
                int i => new NumberValue(i, null),
                long l => new NumberValue(l, null),
                double d => new NumberValue(null, d),
                string s => new StringValue(s),
                JsonElement e => e.ValueKind switch
                {
                    JsonValueKind.False => new BoolValue(false),
                    JsonValueKind.True => new BoolValue(true),
                    JsonValueKind.String => new StringValue(e.GetString()!),
                    JsonValueKind.Number => new NumberValue(e.TryGetInt64(out var l) ? l : null,
                        e.TryGetDouble(out var d) ? d : null),
                    _ => throw new ArgumentOutOfRangeException($"{e.ValueKind}-{e}")
                },
                var objValue => throw new ArgumentOutOfRangeException($"{objValue}-{objValue.GetType()}")
            },
        };
    }

    public static EvaluatedExpr GetValueFromPath(EvalEnvironment env, PathExpr path)
    {
        var (newEnv, segments) = EvalPath(env, path, JsonPathSegments.Empty);
        var outNode = segments.Traverse(newEnv.Data);
        return new EvaluatedExpr(newEnv, ToValue(outNode));
    }
}

public record Failure(ExprValue Message, InbuiltFunction Function, ExprValue Actual, ExprValue Expected);

public record EvalEnvironment(
    JsonObject Data,
    JsonObject Config,
    Failure? Failure,
    IEnumerable<(string, ExprValue)> RuleProperties,
    ImmutableDictionary<Expr, ExprValue> Evaluated)
{
    public EvalEnvironment WithExprValue(Expr expr, ExprValue value)
    {
        return this with { Evaluated = Evaluated.SetItem(expr, value) };
    }

    public static EvalEnvironment FromData(JsonObject data, JsonObject config)
    {
        return new EvalEnvironment(data, config, null, [],
            ImmutableDictionary<Expr, ExprValue>.Empty);
    }
}

public record EvaluatedResult<T>(EvalEnvironment Env, T Result)
{
    public EvaluatedResult<T> WithExprValue(Expr expr, ExprValue value)
    {
        return this with { Env = Env.WithExprValue(expr, value) };
    }
}

public record EvaluatedExpr(EvalEnvironment Env, ExprValue Value)
{
    public EvaluatedExpr IfElse(Expr trueExpr, Expr falseExpr)
    {
        return Value.AsBool() ? Env.Evaluate(trueExpr) : Env.Evaluate(falseExpr);
    }
}