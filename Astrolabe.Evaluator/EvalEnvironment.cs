using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Astrolabe.Evaluator;

using EvaluatedExpr = EnvironmentValue<ExprValue>;

public interface EvalEnvironment
{
    EvaluatedExpr EvaluateData(DataPath dataPath);
    DataPath BasePath { get; }
    Expr? GetReplacement(Expr expr);
    EvalEnvironment WithReplacement(Expr expr, Expr? value);
    EvalEnvironment MapReplacement(Expr expr, Func<Expr?, Expr> mapValue);

    EnvironmentValue<ExprValue> EvaluateCall(CallableExpr callEnvExpr);

    EnvironmentValue<Expr> ResolveCall(CallableExpr callEnvExpr);
    EvalEnvironment WithBasePath(DataPath indexPath);
}

public record EnvironmentValue<T>(EvalEnvironment Env, T Value)
{
    public ExprValue AsValue()
    {
        return (ExprValue)(object)Value!;
    }

    public EnvironmentValue<T2> Map<T2>(Func<T, EvalEnvironment, T2> select)
    {
        return Env.WithValue(select(Value, Env));
    }

    public EnvironmentValue<T2> Then<T2>(Func<EnvironmentValue<T>, EnvironmentValue<T2>> select)
    {
        return select(this);
    }

    public EnvironmentValue<T2> Map<T2>(Func<T, T2> select)
    {
        return Env.WithValue(select(Value));
    }

    public EnvironmentValue<IEnumerable<T>> Single()
    {
        return Env.WithValue<IEnumerable<T>>([Value]);
    }

    public EnvironmentValue<T> WithBasePath(DataPath basePath)
    {
        return Env.WithBasePath(basePath).WithValue(Value);
    }
}

public static class EvalEnvironmentExtensions
{
    public static EvalEnvironment EvaluateForEach<T>(
        this EvalEnvironment env,
        IEnumerable<T> evalList,
        Func<EvalEnvironment, T, EvalEnvironment> evalFunc
    )
    {
        return evalList.Aggregate(env, evalFunc);
    }

    public static EvalEnvironment WithReplacement(this EnvironmentValue<Expr> ev, Expr variable)
    {
        return ev.Env.WithReplacement(variable, ev.Value);
    }

    public static EnvironmentValue<Expr> AsExpr<T>(this EnvironmentValue<T> ev)
        where T : Expr
    {
        return ev.Map(x => (Expr)x);
    }

    public static EnvironmentValue<IEnumerable<T>> SingleOrEmpty<T>(
        this EnvironmentValue<T?> evalResult
    )
    {
        return evalResult.Env.WithValue<IEnumerable<T>>(
            evalResult.Value != null ? [evalResult.Value] : []
        );
    }

    public static EnvironmentValue<T> WithReplacement<T>(
        this EnvironmentValue<T> evalExpr,
        Expr expr,
        Expr? value
    )
    {
        return evalExpr with { Env = evalExpr.Env.WithReplacement(expr, value) };
    }

    public static EnvironmentValue<IEnumerable<object?>> Singleton(this EvaluatedExpr evalExpr)
    {
        return evalExpr.Map<IEnumerable<object?>>(x => [x.Value]);
    }

    public static EvaluatedExpr IfElse(this EvaluatedExpr evalExpr, Expr trueExpr, Expr falseExpr)
    {
        return evalExpr.Value.IsNull()
            ? evalExpr
            : evalExpr.Env.Evaluate(evalExpr.Value.AsBool() ? trueExpr : falseExpr);
    }

    public static EnvironmentValue<IEnumerable<ExprValue>> AppendTo(
        this EvaluatedExpr acc,
        EnvironmentValue<IEnumerable<ExprValue>> other
    )
    {
        return acc.Env.WithValue(other.Value.Append(acc.Value));
    }

    public static EnvironmentValue<List<ExprValue>> EvaluateAllExpr(
        this EvalEnvironment env,
        IEnumerable<Expr> evalList
    )
    {
        return env.EvaluateAll(evalList, (env2, e) => env2.Evaluate(e).Single())
            .Map(x => x.ToList());
    }

    public static EnvironmentValue<IEnumerable<TResult>> EvaluateEach<T, TResult>(
        this EvalEnvironment env,
        IEnumerable<T> evalList,
        Func<EvalEnvironment, T, EnvironmentValue<TResult>> evalFunc
    )
    {
        return evalList.Aggregate(
            env.WithEmpty<TResult>(),
            (allResults, r) =>
            {
                var result = evalFunc(allResults.Env, r);
                return result.Map(x => allResults.Value.Append(x));
            }
        );
    }

    public static EnvironmentValue<IEnumerable<TResult>> EvaluateAll<T, TResult>(
        this EvalEnvironment env,
        IEnumerable<T> evalList,
        Func<EvalEnvironment, T, EnvironmentValue<IEnumerable<TResult>>> evalFunc
    )
    {
        return evalList.Aggregate(
            env.WithEmpty<TResult>(),
            (allResults, r) =>
            {
                var result = evalFunc(allResults.Env, r);
                return result.AppendTo(allResults);
            }
        );
    }

    public static EnvironmentValue<T> WithValue<T>(this EvalEnvironment env, T result)
    {
        return new EnvironmentValue<T>(env, result);
    }

    public static EnvironmentValue<IEnumerable<T>> WithEmpty<T>(this EvalEnvironment env)
    {
        return new EnvironmentValue<IEnumerable<T>>(env, []);
    }

    public static EvaluatedExpr WithExprValue(this EvalEnvironment env, ExprValue value)
    {
        return new EvaluatedExpr(env, value);
    }

    public static EnvironmentValue<Expr> WithExpr(this EvalEnvironment env, Expr value)
    {
        return new EnvironmentValue<Expr>(env, value);
    }

    public static EvaluatedExpr WithNull(this EvalEnvironment env)
    {
        return new EvaluatedExpr(env, ExprValue.Null);
    }

    public static EnvironmentValue<IEnumerable<T>> AppendTo<T>(
        this EnvironmentValue<IEnumerable<T>> envResult,
        EnvironmentValue<IEnumerable<T>> other
    )
    {
        return envResult with { Value = other.Value.Concat(envResult.Value) };
    }
}
