using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using Astrolabe.JSON;

namespace Astrolabe.Validation;

public static class Interpreter
{
    public static PathExpr ResolvePath(PathExpr pathExpr, ImmutableDictionary<Expr, Value> resolve)
    {
        return new PathExpr(ResolveExpr(pathExpr.Segment, resolve),
            pathExpr.Parent != null ? ResolvePath(pathExpr.Parent, resolve) : null);
    }

    public static Expr ResolveExpr(Expr expr, ImmutableDictionary<Expr, Value> resolve)
    {
        if (resolve.TryGetValue(expr, out var already))
            return already;
        return expr switch
        {
            Value v => v,
            GetData getData => new GetData(Path: ResolvePath(getData.Path, resolve)),
            CallExpr callExpr => callExpr with { Args = callExpr.Args.Select(x => ResolveExpr(x, resolve)).ToList() }
        };
    }

    public static (EvalEnvironment, Value) Evaluate(Expr expr, EvalEnvironment environment)
    {
        if (environment.Evaluated.TryGetValue(expr, out var already))
            return (environment, already);
        return expr switch
        {
            CallExpr callExpr => EvalCallExpr(callExpr),
            GetData getData => DoGetData(getData),
            Value v => (environment, v),
            _ => throw new ArgumentOutOfRangeException(expr.ToString())
        };

        (EvalEnvironment, Value) DoGetData(GetData fromPath)
        {
            var (newEnv, segments) = EvalPath(environment, fromPath.Path, JsonPathSegments.Empty);
            var outNode = segments.Traverse(newEnv.Data);
            return (newEnv, outNode switch
            {
                JsonValue v => v.GetValueKind() switch
                {
                    JsonValueKind.Number => v.TryGetValue(out double d) ? new DoubleValue(d) :
                        v.TryGetValue(out int i) ? new LongValue(i) : new LongValue(v.GetValue<long>()),
                    JsonValueKind.String => new StringValue(v.GetValue<string>()),
                    JsonValueKind.False or JsonValueKind.True => new BoolValue(v.GetValue<bool>()),
                    var other => throw new ArgumentException($"{segments} {other.GetType()}")
                },
                null => DebugNull()
            });

            NullValue DebugNull()
            {
                Console.WriteLine("Null at: " + segments);
                return new NullValue();
            }
        }


        (EvalEnvironment, Value) EvalCallExpr(CallExpr callExpr)
        {
            var (nextEnv, evalArgs) = callExpr.Args.Aggregate((environment, Enumerable.Empty<Value>()),
                (e, v) =>
                {
                    var (newEnv, result) = Evaluate(v, e.environment);
                    return (newEnv, e.Item2.Append(result));
                });
            var argsList = evalArgs.ToList();
            if (argsList.Count == 2)
            {
                var v1 = argsList[0];
                var v2 = argsList[1];
                if ((v1, v2) is (NullValue, _) or (_, NullValue))
                    return (environment, NullValue.Instance);
                var result = callExpr.Function switch
                {
                    InbuiltFunction.Eq => new BoolValue(v1 == v2),
                    InbuiltFunction.Ne => new BoolValue(v1 != v2),
                    InbuiltFunction.And => (BoolValue)v1 && (BoolValue)v2,
                    InbuiltFunction.Or => (BoolValue)v1 || (BoolValue)v2,
                    InbuiltFunction.Add or InbuiltFunction.Divide or InbuiltFunction.Minus or InbuiltFunction.Multiply
                        => DoMathOp(callExpr.Function, v1, v2),
                    var f => new BoolValue(DoCompare(f, v1, v2))
                };
                return (nextEnv, result);
            }

            if (argsList.Count == 1)
            {
                var v1 = argsList[0];
                var result = callExpr.Function switch
                {
                    InbuiltFunction.Not => new BoolValue(!((BoolValue)v1).Value)
                };
                return (nextEnv, result);
            }

            throw new ArgumentException("Wrong number of arguments");
        }
    }

    public static bool DoCompare(InbuiltFunction compareType, Value o1, Value o2)
    {
        var diff = (o1, o2) switch
        {
            (LongValue v, DoubleValue v2) => ((double)v.Value).CompareTo(v2.Value),
            (LongValue v, LongValue v2) => v.Value.CompareTo(v2.Value),
            (DoubleValue v, DoubleValue v2) => v.Value.CompareTo(v2.Value),
            (DoubleValue v, LongValue v2) => v.Value.CompareTo(v2.Value),
            _ => throw new ArgumentException($"Compare {o1.GetType()}-{o2.GetType()}")
        };
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

    public static Value DoMathOp(InbuiltFunction op, Value o1, Value o2)
    {
        (double, double)? d = (o1, o2) switch
        {
            (DoubleValue v1, LongValue v2) => (v1.Value, v2.Value),
            (DoubleValue v1, DoubleValue v2) => (v1.Value, v2.Value),
            (LongValue v1, DoubleValue v2) => (v1.Value, v2.Value),
            _ => null
        };
        if (d is var (d1, d2))
        {
            return new DoubleValue(op switch
            {
                InbuiltFunction.Add => d1 + d2,
                InbuiltFunction.Minus => d1 - d2,
                InbuiltFunction.Multiply => d1 * d2,
                InbuiltFunction.Divide => d1 / d2,
            });
        }

        if ((o1, o2) is (LongValue { Value: var i1 }, LongValue { Value: var i2 }))
        {
            return new LongValue(op switch
            {
                InbuiltFunction.Add => i1 + i2,
                InbuiltFunction.Minus => i1 - i2,
                InbuiltFunction.Multiply => i1 * i2,
                InbuiltFunction.Divide => i1 / i2,
            });
        }

        throw new ArgumentException($"MathOp {op} {o1.GetType()}-{o2.GetType()}");
    }

    public static (EvalEnvironment, IEnumerable<ResolvedRule<T>>) EvaluateRule<T>(Rule<T> rule,
        EvalEnvironment environment)
    {
        return rule switch
        {
            RulesForEach<T> rulesForEach => DoRulesForEach(rulesForEach),
            SingleRule<T> singleRule => DoSingleRule(singleRule),
            _ => throw new ArgumentOutOfRangeException(nameof(rule))
        };

        (EvalEnvironment, IEnumerable<ResolvedRule<T>>) DoSingleRule(SingleRule<T> single)
        {
            var (nextEnv, segments) = EvalPath(environment, ResolvePath(single.Path, environment.Evaluated),
                JsonPathSegments.Empty);
            return (nextEnv, [new ResolvedRule<T>(segments, ResolveExpr(single.Must, environment.Evaluated))]);
        }

        (EvalEnvironment, IEnumerable<ResolvedRule<T>>) DoRulesForEach(RulesForEach<T> rules)
        {
            var (nextEnv, collectionSeg) = EvalPath(environment, rules.Path, JsonPathSegments.Empty);
            var dataCollection = collectionSeg.Traverse(environment.Data);
            return dataCollection switch
            {
                JsonArray array => Enumerable.Range(0, array.Count).Aggregate(
                    (nextEnv, Enumerable.Empty<ResolvedRule<T>>()),
                    (acc, index) =>
                    {
                        var envWithIndex = acc.nextEnv.WithExprValue(rules.Index, new LongValue(index));
                        return rules.Rules.Aggregate((envWithIndex, acc.Item2), (acc2, r) =>
                        {
                            var (env, evalRules) = EvaluateRule(r, acc2.envWithIndex);
                            return (env, acc2.Item2.Concat(evalRules));
                        });
                    })
            };
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

        var (nextEnv, seg) = Evaluate(pathExpr.Segment, env);
        return seg switch
        {
            StringValue s => (nextEnv, parentSegments.Field(s.Value)),
            LongValue l => (nextEnv, parentSegments.Index((int)l.Value))
        };
    }
}

public record EvalEnvironment(JsonObject Data, JsonObject Config, ImmutableDictionary<Expr, Value> Evaluated)
{
    public EvalEnvironment WithExprValue(Expr expr, Value value)
    {
        return this with { Evaluated = Evaluated.SetItem(expr, value) };
    }

    public static EvalEnvironment FromData(JsonObject data, JsonObject config)
    {
        return new EvalEnvironment(data, config, ImmutableDictionary<Expr, Value>.Empty);
    }
}