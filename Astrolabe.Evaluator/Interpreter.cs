namespace Astrolabe.Evaluator;

using EvaluatedExprValue = EnvironmentValue<ExprValue>;

public static class Interpreter
{
    public static EnvironmentValue<Expr> ResolveExpr(this EvalEnvironment environment, Expr expr)
    {
        if (environment.TryGetReplacement(expr, out var already))
            return already;
        return expr switch
        {
            ExprValue v => environment.WithExpr(v),
            VarExpr v => environment.WithExpr(v),
            DotExpr dotExpr => DoDot(dotExpr),
            ResolveEval resolveExpr
                => environment.ResolveExpr(resolveExpr.Expr).Evaluate().AsExpr(),
            CallableExpr callExpr => DoCall(callExpr),
            MapExpr mapExpr => DoMap(mapExpr)
        };

        EnvironmentValue<Expr> DoMap(MapExpr mapExpr)
        {
            var (arrayEnv, arrayPath) = environment.ResolveExpr(mapExpr.Array);
            var arrayExpr = arrayEnv.Evaluate(arrayPath).Value;
            var arrayVal = arrayExpr.AsValue();
            if (arrayVal.IsNull())
                return arrayEnv.WithExpr(ExprValue.Null);
            var arrayValue = arrayVal.AsArray();
            var elemVar = mapExpr.ElemPath;
            var arrayBasePath = arrayPath.AsValue().AsPath();
            var results = arrayEnv.EvaluateAll(
                Enumerable.Range(0, arrayValue.Count),
                (e, v) =>
                    e.WithReplacement(elemVar, new ExprValue(new IndexPath(v, arrayBasePath)))
                        .ResolveExpr(mapExpr.MapTo)
                        .Single()
            );

            return arrayEnv.WithExpr(new ArrayExpr(results.Value));
        }

        EnvironmentValue<Expr> DoCall(CallableExpr callExpr)
        {
            var resolvedCall = environment
                .EvaluateAll(callExpr.Args, (e, ex) => e.ResolveExpr(ex).Single())
                .Map(callExpr.WithArgs);
            return resolvedCall.Env.ResolveCall(resolvedCall.Value);
        }

        EnvironmentValue<Expr> DoDot(DotExpr dotExpr)
        {
            var (pathEnv, pathValue) = environment.ResolveExpr(dotExpr.Base);
            var (segEnv, segValue) = pathEnv.ResolveExpr(dotExpr.Segment);
            var basePath = pathValue.AsValue().Value switch
            {
                string baseString => new FieldPath(baseString, DataPath.Empty),
                DataPath dp => dp
            };
            var resolved = ExprValue.From(ValueExtensions.ApplyDot(basePath, segValue.AsValue()));
            return segEnv.WithExpr(resolved);
        }
    }

    public static EvaluatedExprValue Evaluate(this EnvironmentValue<Expr> envExpr)
    {
        return envExpr.Env.Evaluate(envExpr.Value);
    }

    public static EvaluatedExprValue Evaluate(this EvalEnvironment environment, Expr expr)
    {
        if (environment.TryGetReplacement(expr, out var already))
            return already.Evaluate();
        return expr switch
        {
            ExprValue { Value: DataPath dp } => environment.EvaluateData(dp),
            CallExpr callExpr => environment.EvaluateCall(callExpr),
            CallEnvExpr callEnvExpr => environment.EvaluateCall(callEnvExpr),
            ArrayExpr arrayExpr
                => arrayExpr
                    .ValueExpr.Aggregate(
                        environment.WithEmpty<ExprValue>(),
                        (acc, e) => acc.Env.Evaluate(e).AppendTo(acc)
                    )
                    .Map(x => ExprValue.From(ArrayValue.From(x.Select(v => v.AsValue().Value)))),
            ExprValue v => environment.WithValue(v),
            _ => throw new ArgumentOutOfRangeException(expr.ToString())
        };
    }
}
