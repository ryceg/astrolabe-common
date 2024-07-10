using System.Collections;
using System.Collections.Immutable;
using System.Text.Json.Nodes;

namespace Astrolabe.Validation;

using EvaluatedExpr = EvaluatedResult<ExprValue>;

public record EvalEnvironment(
    JsonObject Data,
    Failure? Failure,
    IEnumerable<(string, ExprValue)> RuleProperties,
    ImmutableDictionary<Expr, ExprValue> Evaluated
)
{
    public EvalEnvironment WithExprValue(Expr expr, ExprValue value)
    {
        return this with { Evaluated = Evaluated.SetItem(expr, value) };
    }

    public static EvalEnvironment FromData(JsonObject data)
    {
        return new EvalEnvironment(data, null, [], ImmutableDictionary<Expr, ExprValue>.Empty);
    }
}

public record Failure(
    ExprValue Message,
    InbuiltFunction Function,
    ExprValue Actual,
    ExprValue Expected
);

public record EvaluatedResult<T>(EvalEnvironment Env, T Result)
{
    public EvaluatedResult<T2> Map<T2>(Func<T, EvalEnvironment, T2> select)
    {
        return Env.WithResult(select(Result, Env));
    }
    
    public EvaluatedResult<T2> Map<T2>(Func<T, T2> select)
    {
        return Env.WithResult(select(Result));
    }

    public EvaluatedResult<IEnumerable<T>> Single()
    {
        return Env.WithResult<IEnumerable<T>>([Result]);
    }

}

public static class EvalEnvironmentExtensions
{
    public static EvaluatedResult<T> WithExprValue<T>(
        this EvaluatedResult<T> evalExpr,
        Expr expr,
        ExprValue value
    )
    {
        return evalExpr with { Env = evalExpr.Env.WithExprValue(expr, value) };
    }

    public static EvaluatedExpr IfElse(this EvaluatedExpr evalExpr, Expr trueExpr, Expr falseExpr)
    {
        return evalExpr.Env.Evaluate(evalExpr.Result.AsBool() ? trueExpr : falseExpr);
    }

    public static EvaluatedResult<IEnumerable<ExprValue>> AppendTo(
        this EvaluatedExpr acc,
        EvaluatedResult<IEnumerable<ExprValue>> other
    )
    {
        return acc.Env.WithResult(other.Result.Append(acc.Result));
    }

    public static EvaluatedResult<IEnumerable<TResult>> EvaluateAll<T, TResult>(
        this EvalEnvironment env,
        IEnumerable<T> evalList,
        Func<EvalEnvironment, T, EvaluatedResult<IEnumerable<TResult>>> evalFunc
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

    public static EvaluatedResult<T> WithResult<T>(this EvalEnvironment env, T result)
    {
        return new EvaluatedResult<T>(env, result);
    }

    public static EvaluatedResult<IEnumerable<T>> WithEmpty<T>(this EvalEnvironment env)
    {
        return new EvaluatedResult<IEnumerable<T>>(env, []);
    }

    public static EvaluatedExpr WithValue(this EvalEnvironment env, ExprValue value)
    {
        return new EvaluatedExpr(env, value);
    }

    public static EvaluatedExpr ToValue<T>(
        this EvaluatedResult<T> envResult,
        Func<T, ExprValue> mapValue
    )
    {
        return envResult.Env.WithValue(mapValue(envResult.Result));
    }

    public static EvaluatedResult<IEnumerable<T>> AppendTo<T>(
        this EvaluatedResult<IEnumerable<T>> envResult,
        EvaluatedResult<IEnumerable<T>> other
    )
    {
        return envResult with { Result = other.Result.Concat(envResult.Result) };
    }
}
