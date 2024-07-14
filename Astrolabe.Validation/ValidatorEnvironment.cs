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

    public EnvironmentValue<Expr> EvaluateCall(CallEnvExpr callEnvExpr)
    {
        var args = callEnvExpr.Args;
        return callEnvExpr.Function switch
        {
            "WithProperty" => DoProp(),
            "WithMessage" => DoMessage()
        };

        EnvironmentValue<Expr> DoMessage()
        {
            var (msgEnv, msg) = this.Evaluate(callEnvExpr.Args[0]);
            var valEnv = FromEnv(msgEnv);
            return (valEnv with { Message = msg }).WithValue(callEnvExpr.Args[1]);
        }

        EnvironmentValue<Expr> DoProp()
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
            ).WithValue(callEnvExpr.Args[2]);
        }
    }

    public EnvironmentValue<ExprValue> BooleanResult(
        bool? result,
        CallExpr callExpr,
        IEnumerable<ExprValue> evaluatedArgs
    )
    {
        if (result == false)
        {
            return (
                this with
                {
                    Failures = Failures.Append(new Failure(callExpr, evaluatedArgs.ToList()))
                }
            ).WithExprValue(ExprValue.False);
        }
        return this.WithExprValue(ExprValue.From(result));
    }
}

public record Failure(CallExpr Call, IList<ExprValue> EvaluatedArgs);

public record RuleFailure(IEnumerable<Failure> Failures, string? Message, ResolvedRule Rule);
