using System.Numerics;
using System.Text.Json.Nodes;
using Astrolabe.JSON;

namespace Astrolabe.Validation;

public static class Interpreter
{
    public static (EvalEnvironment, object) Evaluate(Expr expr, EvalEnvironment environment)
    {
        return expr switch
        {
            CompareExpr compareExpr => DoCompareExpr(compareExpr),
            ConstantExpr {Value:var v} => (environment, v),
            LogicOpExpr logicOpExpr => DoLogicOpExpr(logicOpExpr),
            FromPath fromPath => DoFromPath(fromPath),
            MathBinOpExpr mathBinOpExpr => DoMath(mathBinOpExpr),
            _ => throw new ArgumentOutOfRangeException(expr.ToString())
        };

        
        (EvalEnvironment, object) DoMath(MathBinOpExpr me)
        {
            var (env2, v1) = Evaluate(me.E1, environment);
            var (env3, v2) = Evaluate(me.E2, env2);
            return (env3, DoMathOp(me.MathBinOp, v1, v2));
        }

        (EvalEnvironment, object) DoFromPath(FromPath fromPath)
        {
            var (newEnv, segments) = EvalPath(environment, fromPath.Path, JsonPathSegments.Empty);
            var outNode = segments.Traverse(newEnv.Data);
            return (newEnv, outNode switch
            {
                JsonValue v => v.GetValue<object>()
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
                string s => (nextEnv, parentSegments.Field(s)),
                int i => (nextEnv, parentSegments.Index(i))
            };
        }
        
        (EvalEnvironment, bool) DoCompareExpr(CompareExpr ce)
        {
            var (env2, v1) = Evaluate(ce.E1, environment);
            var (env3, v2) = Evaluate(ce.E2, env2);
            return (env3, DoCompare(ce.CompareType, v1, v2));
        }
        
        (EvalEnvironment, bool) DoLogicOpExpr(LogicOpExpr le)
        {
            var v1 = Evaluate(le.E1, environment);
            if (le.LogicType == LogicType.Not)
                return (v1.Item1, !(bool)v1.Item2);
            var v2 = Evaluate(le.E2!, v1.Item1);
            var res = (le.LogicType, v1.Item2, v2.Item2) switch
            {
                (LogicType.Or, bool b1, bool b2) => b1 || b2, 
                (LogicType.And, bool b1, bool b2) => b1 && b2
            };
            return (v2.Item1, res);
        }

    }

    public static bool DoCompare(CompareType compareType, object o1, object o2)
    {
        var diff = (o1, o2) switch
        {
            (int v, IConvertible v2) => v.CompareTo(v2.ToInt32(null)), 
            (double v, IConvertible v2) => v.CompareTo(v2.ToDouble(null)),
            _ => throw new ArgumentException($"Compare {o1.GetType()}-{o2.GetType()}")
        };
        return compareType switch
        {
            CompareType.Eq => diff == 0,
            CompareType.Ne => diff != 0,
            CompareType.Gt => diff > 0,
            CompareType.GtEq => diff >= 0,
            CompareType.Lt => diff < 0,
            CompareType.LtEq => diff <= 0,
        };
    }
    
    public static object DoMathOp(MathBinOp op, object o1, object o2)
    {
        (double, double)? d = (o1, o2) switch
        {
            (double v1, IConvertible v2) => (v1, v2.ToDouble(null)), 
            (IConvertible v1, double v2) => (v1.ToDouble(null), v2),
            _ => null
        };
        if (d is var (d1, d2))
        {
            return op switch
            {
                MathBinOp.Add => d1 + d2,
                MathBinOp.Minus => d1 - d2,
                MathBinOp.Multiply => d1 * d2,
                MathBinOp.Divide => d1 / d2,
            };
        }
        if ((o1, o2) is (int i1, int i2))
        {
            return op switch
            {
                MathBinOp.Add => i1 + i2,
                MathBinOp.Minus => i1 - i2,
                MathBinOp.Multiply => i1 * i2,
                MathBinOp.Divide => i1 / i2,
            };
        }
        throw new ArgumentException($"MathOp {op} {o1.GetType()}-{o2.GetType()}");
    }

}

public record EvalEnvironment(JsonObject Data);