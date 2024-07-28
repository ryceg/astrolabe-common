using Astrolabe.Evaluator.Functions;

namespace Astrolabe.Evaluator;

using EvaluatedExprValue = EnvironmentValue<ValueExpr>;

public static class Interpreter
{
    public static EnvironmentValue<EvalExpr> ResolveExpr(this EnvironmentValue<EvalExpr> environment)
    {
        return environment.Env.ResolveExpr(environment.Value);
    }

    public static EnvironmentValue<EvalExpr> ResolveExpr(this EvalEnvironment environment, EvalExpr expr)
    {
        var already = environment.GetReplacement(expr);
        if (already != null)
            return environment.WithValue(already);
        return expr switch
        {
            ValueExpr { Value: DataPath dp }
                => environment.WithExpr(new ValueExpr(environment.BasePath.Concat(dp))),
            LambdaExpr lambdaExpr => DoLambda(lambdaExpr),
            ValueExpr or VarExpr or LambdaExpr => environment.WithExpr(expr),
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
                                .Then(e => e.WithReplacement(nextVar.Item1, e.Value))
                                .Env
                    )
                    .ResolveExpr(v.In),
            CallExpr callExpr => (environment.GetReplacement(new VarExpr(callExpr.Function)).AsValue().Value as FunctionHandler).Resolve(callExpr, environment),
        };

        EnvironmentValue<EvalExpr> DoLambda(LambdaExpr lambdaExpr)
        {
            return environment.BasePath switch
            {
                IndexPath ip
                    => environment
                        .WithReplacement(lambdaExpr.Variable, ValueExpr.From(ip.Index))
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
        var already = environment.GetReplacement(expr);
        if (already != null)
            return environment.WithValue(already.AsValue());
        return expr switch
        {
            ValueExpr { Value: DataPath dp } => environment.EvaluateData(dp),
            ArrayExpr arrayExpr => EvalArray(arrayExpr),
            ValueExpr v => environment.WithValue(v),
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
