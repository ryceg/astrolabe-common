namespace Astrolabe.Evaluator;

using EvaluatedExprValue = EnvironmentValue<ExprValue>;

public static class Interpreter
{
    public static EnvironmentValue<Expr> ResolveExpr(this EnvironmentValue<Expr> environment)
    {
        return environment.Env.ResolveExpr(environment.Value);
    }

    public static EnvironmentValue<Expr> ResolveExpr(this EvalEnvironment environment, Expr expr)
    {
        var already = environment.GetReplacement(expr);
        if (already != null)
            return environment.WithValue(already);
        return expr switch
        {
            ExprValue or VarExpr or LambdaExpr => environment.WithExpr(expr),
            ArrayExpr ae
                => environment
                    .EvaluateEach(ae.ValueExpr, ResolveExpr)
                    .Map(x => (Expr)new ArrayExpr(x.ToList())),
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
            ResolveEval resolveExpr
                => environment.ResolveExpr(resolveExpr.Expr).Evaluate().AsExpr(),
            CallableExpr callExpr => DoCall(callExpr),
        };

        EnvironmentValue<Expr> DoCall(CallableExpr callExpr)
        {
            var resolvedCall = environment
                .EvaluateAll(callExpr.Args, (e, ex) => e.ResolveExpr(ex).Single())
                .Map(callExpr.WithArgs);
            return resolvedCall.Env.ResolveCall(resolvedCall.Value);
        }
    }

    public static EvaluatedExprValue Evaluate(this EnvironmentValue<Expr> envExpr)
    {
        return envExpr.Env.Evaluate(envExpr.Value);
    }

    public static EvaluatedExprValue ResolveAndEvaluate(this EvalEnvironment env, Expr expr)
    {
        return env.ResolveExpr(expr).Evaluate();
    }

    public static EvaluatedExprValue Evaluate(this EvalEnvironment environment, Expr expr)
    {
        var already = environment.GetReplacement(expr);
        if (already != null)
            return environment.WithValue(already.AsValue());
        return expr switch
        {
            ExprValue { Value: DataPath dp } => environment.EvaluateData(dp),
            CallExpr callExpr => environment.EvaluateCall(callExpr),
            CallEnvExpr callEnvExpr => environment.EvaluateCall(callEnvExpr),
            ArrayExpr arrayExpr => EvalArray(arrayExpr),
            ExprValue v => environment.WithValue(v),
            _ => throw new ArgumentOutOfRangeException(expr.ToString())
        };

        EvaluatedExprValue EvalArray(ArrayExpr arrayExpr)
        {
            var elements = arrayExpr.ValueExpr.Aggregate(
                environment.WithEmpty<ExprValue>(),
                (acc, e) => acc.Env.Evaluate(e).AppendTo(acc)
            );
            return elements.Map(x =>
            {
                var rawElements = ArrayValue.From(x.Select(v => v.Value));
                return ExprValue.From(rawElements);
            });
        }
    }
}
