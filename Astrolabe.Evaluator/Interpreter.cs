using Astrolabe.Evaluator.Functions;

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
            VarExpr varExpr
                => environment.GetVariable(varExpr.Name) is { } e
                    ? environment.WithValue(e)
                    : throw new ArgumentException("Unknown variable: " + varExpr.Name),
            ValueExpr => environment.WithExpr(expr),
            ArrayExpr ae
                => environment
                    .EvaluateEach(ae.ValueExpr, ResolveExpr)
                    .Map(x => (EvalExpr)new ArrayExpr(x.ToList())),
            LetExpr v
                => environment
                    .EvaluateForEach(
                        v.Vars,
                        (acc, nextVar) =>
                            acc.ResolveExpr(nextVar.Item2)
                                .Then(e => e.WithReplacement(nextVar.Item1.Name, e.Value))
                                .Env
                    )
                    .ResolveExpr(v.In),
            CallExpr { Function: var func } ce
                => environment.GetVariable(func) is ValueExpr { Value: FunctionHandler handler }
                    ? handler.Resolve(environment, ce)
                    : throw new ArgumentException("No function: " + func),
            _ => throw new ArgumentException("Could not resolve: " + expr)
        };

        EnvironmentValue<EvalExpr> ResolvePath(DataPath dp)
        {
            var resolvedPath = environment.BasePath.Concat(dp);
            return environment.GetData(resolvedPath) switch
            {
                ArrayValue av
                    => environment.WithExpr(
                        new ArrayExpr(
                            Enumerable
                                .Range(0, av.Count)
                                .Select(x => new PathExpr(new IndexPath(x, resolvedPath)))
                        )
                    ),
                var v => environment.WithExpr(new PathExpr(resolvedPath))
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
            var elements = arrayExpr.ValueExpr.Aggregate(
                environment.WithEmpty<ValueExpr>(),
                (acc, e) => acc.Env.Evaluate(e).AppendTo(acc)
            );
            return elements.Map(x =>
            {
                var rawElements = ArrayValue.From(x.Select(v => v.Value));
                return ValueExpr.From(rawElements);
            });
        }
    }
}
