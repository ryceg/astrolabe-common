using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using Astrolabe.JSON;

namespace Astrolabe.Validation;

public static class Interpreter
{
    public static PathExpr ResolvePath(PathExpr pathExpr, ImmutableDictionary<Expr, ExprValue> resolve)
    {
        return new PathExpr(ResolveExpr(pathExpr.Segment, resolve),
            pathExpr.Parent != null ? ResolvePath(pathExpr.Parent, resolve) : null);
    }

    public static Expr ResolveExpr(Expr expr, ImmutableDictionary<Expr, ExprValue> resolve)
    {
        if (resolve.TryGetValue(expr, out var already))
            return already;
        return expr switch
        {
            ExprValue v => v,
            GetData getData => new GetData(Path: ResolvePath(getData.Path, resolve)),
            CallExpr callExpr => callExpr with { Args = callExpr.Args.Select(x => ResolveExpr(x, resolve)).ToList() }
        };
    }

    public static (EvalEnvironment, ExprValue) Evaluate(Expr expr, EvalEnvironment environment)
    {
        if (environment.Evaluated.TryGetValue(expr, out var already))
            return (environment, already);
        return expr switch
        {
            CallExpr callExpr => EvalCallExpr(callExpr),
            GetData getData => DoGetData(getData),
            ExprValue v => (environment, v),
            _ => throw new ArgumentOutOfRangeException(expr.ToString())
        };

        (EvalEnvironment, ExprValue) DoGetData(GetData fromPath)
        {
            var (newEnv, segments) = EvalPath(environment, fromPath.Path, JsonPathSegments.Empty);
            var outNode = segments.Traverse(newEnv.Data);
            var objValue = outNode?.GetValue<object>();
            return (newEnv, objValue switch
            {
                null => ExprValue.Null,
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
                _ => throw new ArgumentOutOfRangeException($"{objValue}-{objValue.GetType()}")
            });
        }

        (EvalEnvironment, ExprValue) EvalCallExpr(CallExpr callExpr)
        {
            var (nextEnv, evalArgs) = callExpr.Args.Aggregate((environment, Enumerable.Empty<ExprValue>()),
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
                if (v1 is NullValue || v2 is NullValue)
                    return (environment, ExprValue.Null);
                var result = callExpr.Function switch
                {
                    InbuiltFunction.Eq => (v1 == v2).ToExpr(),
                    InbuiltFunction.Ne => (v1 != v2).ToExpr(),
                    InbuiltFunction.And => (v1.AsBool() && v2.AsBool()).ToExpr(),
                    InbuiltFunction.Or => (v1.AsBool() || v2.AsBool()).ToExpr(),
                    InbuiltFunction.Add or InbuiltFunction.Divide or InbuiltFunction.Minus or InbuiltFunction.Multiply
                        => DoMathOp(callExpr.Function, v1, v2),
                    var f => DoCompare(f, v1, v2).ToExpr()
                };
                return (nextEnv, result);
            }

            if (argsList.Count == 1)
            {
                var v1 = argsList[0];
                var result = callExpr.Function switch
                {
                    InbuiltFunction.Not => (!v1.AsBool()).ToExpr()
                };
                return (nextEnv, result);
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
            diff = ((NumberValue)o1).AsDouble().CompareTo(((NumberValue)o2).AsDouble());
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
                InbuiltFunction.Divide => l1 / l2,
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

    public static (EvalEnvironment, IEnumerable<ResolvedRule<T>>) EvaluateRule<T>(Rule<T> rule,
        EvalEnvironment environment)
    {
        return rule switch
        {
            RulesForEach<T> rulesForEach => DoRulesForEach(rulesForEach),
            _ => DoRules(rule)
        };

        (EvalEnvironment, IEnumerable<ResolvedRule<T>>) DoRules(Rule<T> single)
        {
            var (nextEnv, segments) = EvalPath(environment, ResolvePath(single.Path, environment.Evaluated),
                JsonPathSegments.Empty);
            return (nextEnv, single.Musts
                .Select(x => new ResolvedRule<T>(segments, ResolveExpr(x, environment.Evaluated))));
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
                        var envWithIndex = acc.nextEnv.WithExprValue(rules.Index, index.ToExpr());
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
            StringValue { Value: var s } => (nextEnv, parentSegments.Field(s)),
            NumberValue { LongValue: { } l } => (nextEnv, parentSegments.Index((int)l)),
            _ => throw new ArgumentException($"{seg}")
        };
    }
}

public record EvalEnvironment(JsonObject Data, JsonObject Config, ImmutableDictionary<Expr, ExprValue> Evaluated)
{
    public EvalEnvironment WithExprValue(Expr expr, ExprValue value)
    {
        return this with { Evaluated = Evaluated.SetItem(expr, value) };
    }

    public static EvalEnvironment FromData(JsonObject data, JsonObject config)
    {
        return new EvalEnvironment(data, config, ImmutableDictionary<Expr, ExprValue>.Empty);
    }
}