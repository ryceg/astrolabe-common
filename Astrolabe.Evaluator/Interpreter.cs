namespace Astrolabe.Evaluator;

using EvaluatedExprValue = EnvironmentValue<ValueExpr>;

public static class Interpreter
{
    public static EnvironmentValue<EvalExpr> ResolveExpr(
        this EnvironmentValue<EvalExpr> environment
    )
    {
        return environment.Env.ResolveExpr(environment.Value);
    }

    public static EnvironmentValue<EvalExpr> ResolveExpr(
        this EvalEnvironment environment,
        EvalExpr expr
    )
    {
        return expr switch
        {
            PathExpr { Path: var dp } => ResolvePath(dp),
            LambdaExpr lambdaExpr => DoLambda(lambdaExpr),
            VarExpr varExpr => ResolveVar(varExpr),
            ValueExpr => environment.WithValue(expr),
            ArrayExpr ae
                => environment
                    .EvalSelect(ae.Values, ResolveExpr)
                    .Map(x => new ArrayExpr(x.ToList())),
            LetExpr v
                => environment
                    .EvalForEach(
                        v.Vars,
                        (acc, nextVar) =>
                            acc.ResolveExpr(nextVar.Item2)
                                .Run((e) => e.Env.WithVariable(nextVar.Item1.Name, e.Value))
                    )
                    .ResolveExpr(v.In),
            CallExpr { Function: var func } ce
                => environment.GetVariable(func) is ValueExpr { Value: FunctionHandler handler }
                    ? handler.Resolve(environment, ce)
                    : throw new ArgumentException("No function: " + func),
            _ => throw new ArgumentException("Could not resolve: " + expr)
        };

        EnvironmentValue<EvalExpr> ResolveVar(VarExpr varExpr)
        {
            var evalExpr = environment.GetVariable(varExpr.Name);
            if (evalExpr == null)
                throw new ArgumentException("Unknown variable: " + varExpr.Name);
            return environment.WithValue(evalExpr);
        }

        EnvironmentValue<EvalExpr> ResolvePath(DataPath dp)
        {
            var resolvedPath = environment.BasePath.Concat(dp);
            return environment.GetData(resolvedPath) switch
            {
                ArrayValue av
                    => environment.WithValue(
                        new ArrayExpr(
                            Enumerable
                                .Range(0, av.Count)
                                .Select(x => new PathExpr(new IndexPath(x, resolvedPath)))
                        )
                    ),
                var v => environment.WithValue(new PathExpr(resolvedPath))
            };
        }

        EnvironmentValue<EvalExpr> DoLambda(LambdaExpr lambdaExpr)
        {
            return environment.BasePath switch
            {
                IndexPath ip
                    => environment
                        .WithVariable(lambdaExpr.Variable, ValueExpr.From(ip.Index))
                        .ResolveExpr(lambdaExpr.Value)
            };
        }
    }

    public static EvaluatedExprValue Evaluate(this EnvironmentValue<EvalExpr> envExpr)
    {
        return envExpr.Env.Evaluate(envExpr.Value);
    }

    public static EvaluatedExprValue ResolveAndEvaluate(this EvalEnvironment env, EvalExpr expr)
    {
        return env.ResolveExpr(expr).Evaluate();
    }

    private static EvaluatedExprValue EvaluateOptional(
        EvalEnvironment env,
        EvalExpr expr,
        int index
    )
    {
        return expr switch
        {
            OptionalExpr optional when env.Evaluate(optional.Condition) is var cond
                => cond.Run(x =>
                    (x.Value.MaybeDouble() is { } i ? index == (int)i : x.Value.AsBool())
                        ? x.Env.Evaluate(optional.Value)
                        : x.Env.WithValue(ValueExpr.Undefined)
                ),
            _ => env.Evaluate(expr)
        };
    }

    public static EvaluatedExprValue Evaluate(this EvalEnvironment environment, EvalExpr expr)
    {
        return expr switch
        {
            ArrayExpr arrayExpr => EvalArray(arrayExpr),
            ValueExpr v => environment.WithValue(v),
            CallExpr { Function: var func, Args: var args } callExpr
                when environment.GetVariable(func) is ValueExpr { Value: FunctionHandler handler }
                => handler.Evaluate(environment, callExpr),
            PathExpr { Path: var dp }
                => environment.WithValue(new ValueExpr(environment.GetData(dp))),
            _ => throw new ArgumentOutOfRangeException(expr.ToString())
        };

        EvaluatedExprValue EvalArray(ArrayExpr arrayExpr)
        {
            var elements = arrayExpr.Values.Aggregate(
                (environment.WithEmpty<ValueExpr>(), 0),
                (acc, e) =>
                    (
                        EvaluateOptional(acc.Item1.Env, e, acc.Item2).AppendTo(acc.Item1),
                        acc.Item2 + 1
                    )
            );
            return elements.Item1.Map(x =>
            {
                var rawElements = ArrayValue.From(x.Select(v => v.Value));
                return ValueExpr.From(rawElements);
            });
        }
    }
}
