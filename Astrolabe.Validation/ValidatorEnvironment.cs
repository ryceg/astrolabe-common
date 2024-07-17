using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Astrolabe.Evaluator;

namespace Astrolabe.Validation;

public record ValidatorEnvironment(
    Func<DataPath, object?> GetData,
    IEnumerable<Failure> Failures,
    ExprValue Message,
    ImmutableHashSet<DataPath> FailedData,
    ImmutableDictionary<string, object?> Properties,
    ImmutableDictionary<Expr, Expr> Replacements
) : EvalEnvironment
{
    public static ValidatorEnvironment FromEnv(EvalEnvironment eval)
    {
        return (ValidatorEnvironment)eval;
    }

    public static ValidatorEnvironment FromData(Func<DataPath, object?> data)
    {
        return new ValidatorEnvironment(
            data,
            [],
            ExprValue.Null,
            ImmutableHashSet<DataPath>.Empty,
            ImmutableDictionary<string, object?>.Empty,
            ImmutableDictionary<Expr, Expr>.Empty
        );
    }

    public ValidatorEnvironment WithFailedPath(DataPath rulePath)
    {
        return this with { FailedData = FailedData.Add(rulePath) };
    }

    public EnvironmentValue<ExprValue> EvaluateData(DataPath dataPath)
    {
        return FailedData.Contains(dataPath)
            ? this.WithNull()
            : this.WithValue(new ExprValue(GetData(dataPath)));
    }

    public bool TryGetReplacement(
        Expr expr,
        [MaybeNullWhen(false)] out EnvironmentValue<Expr> value
    )
    {
        if (Replacements.TryGetValue(expr, out var ok))
        {
            value = this.WithValue(ok);
            return true;
        }
        value = null;
        return false;
    }

    public EvalEnvironment WithReplacement(Expr expr, Expr value)
    {
        return this with { Replacements = Replacements.SetItem(expr, value) };
    }

    public EvalEnvironment MapReplacement(Expr expr, Func<Expr?, Expr> mapValue)
    {
        return this with
        {
            Replacements = Replacements.SetItem(
                expr,
                Replacements.TryGetValue(expr, out var existing)
                    ? mapValue(existing)
                    : mapValue(null)
            )
        };
    }

    public EnvironmentValue<ExprValue> EvaluateCall(CallableExpr callExpr)
    {
        if (callExpr is CallEnvExpr callEnvExpr)
            return EvaluateValCall(callEnvExpr);
        if (callExpr is not CallExpr ce)
            return this.WithNull();
        var evalArgs = this.EvaluateAll(ce.Args, (ee, e) => ee.Evaluate(e).Single())
            .Map(x => x.ToList());
        var result = DefaultFunctions.FunctionHandlers[ce.Function].Evaluate(evalArgs.Value);
        if (
            result.IsFalse()
            && ce.Function
                is InbuiltFunction.Eq
                    or InbuiltFunction.Ne
                    or InbuiltFunction.Gt
                    or InbuiltFunction.Lt
                    or InbuiltFunction.GtEq
                    or InbuiltFunction.LtEq
        )
        {
            return (
                FromEnv(evalArgs.Env) with
                {
                    Failures = Failures.Append(new Failure(ce, evalArgs.Value))
                }
            ).WithExprValue(ExprValue.False);
        }
        return evalArgs.Env.WithValue(result);
    }

    public EnvironmentValue<Expr> ResolveCall(CallableExpr callEnvExpr)
    {
        if (callEnvExpr is not CallExpr ce)
            return this.WithExpr(callEnvExpr);
        var result = DefaultFunctions.FunctionHandlers[ce.Function].Resolve(callEnvExpr.Args);
        return this.WithExpr(result ?? ce);
    }

    public EnvironmentValue<ExprValue> EvaluateValCall(CallEnvExpr callEnvExpr)
    {
        var args = callEnvExpr.Args;
        return callEnvExpr.Function switch
        {
            "WithProperty" => DoProp(),
            "WithMessage" => DoMessage()
        };

        EnvironmentValue<ExprValue> DoMessage()
        {
            var (msgEnv, msg) = this.Evaluate(callEnvExpr.Args[0]);
            var valEnv = FromEnv(msgEnv);
            return (valEnv with { Message = msg }).Evaluate(callEnvExpr.Args[1]);
        }

        EnvironmentValue<ExprValue> DoProp()
        {
            var (evalEnvironment, argList) = this.EvaluateAllExpr(
                [callEnvExpr.Args[0], callEnvExpr.Args[1]]
            );
            var valEnv = FromEnv(evalEnvironment);
            return (
                valEnv with
                {
                    Properties = valEnv.Properties.SetItem(argList[0].AsString(), argList[1].Value)
                }
            ).Evaluate(callEnvExpr.Args[2]);
        }
    }
}

public record Failure(CallExpr Call, IList<ExprValue> EvaluatedArgs);

public record RuleFailure(IEnumerable<Failure> Failures, string? Message, ResolvedRule Rule);
