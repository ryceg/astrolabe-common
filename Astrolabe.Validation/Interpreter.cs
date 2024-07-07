using System.Numerics;
using System.Text.Json.Nodes;
using Astrolabe.JSON;

namespace Astrolabe.Validation;

public static class Interpreter
{
    public static (EvalEnvironment, Value) Evaluate(Expr expr, EvalEnvironment environment)
    {
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
                JsonValue v => v.GetValue<object>() switch
                {
                    int i => new LongValue(i),
                    long l => new LongValue(l),
                    double d => new DoubleValue(d),
                    string s => new StringValue(s)
                }
            });
        }

        (EvalEnvironment, JsonPathSegments) EvalPath(EvalEnvironment env, PathExpr pathExpr, JsonPathSegments parentSegments)
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
        if ((o1, o2) is (LongValue {Value: var i1}, LongValue {Value: var i2}))
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

}

public record EvalEnvironment(JsonObject Data);