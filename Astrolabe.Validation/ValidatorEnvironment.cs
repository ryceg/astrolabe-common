using System.Collections.Immutable;
using Astrolabe.Evaluator;
using Astrolabe.Evaluator.Functions;

namespace Astrolabe.Validation;

public record ValidatorState(
    IEnumerable<Failure> Failures,
    ValueExpr Message,
    IEnumerable<ResolvedRule> Rules,
    ImmutableHashSet<DataPath> FailedData,
    ImmutableDictionary<string, object?> Properties
)
{
    public static readonly ValidatorState Empty =
        new(
            [],
            ValueExpr.Null,
            [],
            ImmutableHashSet<DataPath>.Empty,
            ImmutableDictionary<string, object?>.Empty
        );
}

public static class ValidatorEnvironment
{
    public const string RuleFunction = "ValidatorRule";

    public static ValidatorState GetValidatorState(this EvalEnvironment env)
    {
        return (ValidatorState)env.GetVariable("$ValidatorState")!.AsValue().Value!;
    }

    public static EvalEnvironment UpdateValidatorState(
        this EvalEnvironment env,
        Func<ValidatorState, ValidatorState> update
    )
    {
        return env.WithVariable("$ValidatorState", new ValueExpr(update(GetValidatorState(env))));
    }

    public static EvalEnvironment FromData(Func<DataPath, object?> data)
    {
        return new EvalEnvironment(
            data,
            null,
            DataPath.Empty,
            ImmutableDictionary<string, EvalExpr>
                .Empty.AddRange(DefaultFunctions.FunctionHandlers.Select(ToVariable))
                .Add("$ValidatorState", new ValueExpr(ValidatorState.Empty))
                .Add(RuleFunction, new ValueExpr(FunctionHandler.ResolveOnly(ResolveValidation)))
                .Add("WithMessage", new ValueExpr(FunctionHandler.DefaultResolve(EvalWithMessage)))
                .Add(
                    "WithProperty",
                    new ValueExpr(FunctionHandler.DefaultResolve(EvalWithProperty))
                )
        );
    }

    private static KeyValuePair<string, EvalExpr> ToVariable(
        KeyValuePair<string, FunctionHandler> func
    )
    {
        var funcValue = func.Key switch
        {
            "=" or "!=" or ">" or "<" or ">=" or "<=" or "notEmpty" => WrapFunc(func.Value),
            _ => func.Value
        };
        return new KeyValuePair<string, EvalExpr>(func.Key, new ValueExpr(funcValue));

        FunctionHandler WrapFunc(FunctionHandler handler)
        {
            return handler with
            {
                Evaluate = (e, call) =>
                {
                    var (env, args) = e.EvaluateEach(call.Args, (e2, x) => e2.Evaluate(x));
                    var argValuesValue = args.ToList();
                    var result = handler.Evaluate(env, call.WithArgs(argValuesValue));
                    var resultValue = result.Value;
                    if (resultValue.IsFalse())
                    {
                        return result
                            .Env.UpdateValidatorState(v =>
                                v with
                                {
                                    Failures = v.Failures.Append(new Failure(call, argValuesValue))
                                }
                            )
                            .WithValue(resultValue);
                    }
                    return result;
                }
            };
        }
    }

    private static EnvironmentValue<ValueExpr> EvalWithProperty(
        EvalEnvironment environment,
        CallExpr callExpr
    )
    {
        var (evalEnvironment, argList) = environment.EvaluateAllExpr(
            [callExpr.Args[0], callExpr.Args[1]]
        );
        return evalEnvironment
            .UpdateValidatorState(valEnv =>
                valEnv with
                {
                    Properties = valEnv.Properties.SetItem(argList[0].AsString(), argList[1].Value)
                }
            )
            .Evaluate(callExpr.Args[2]);
    }

    public static EnvironmentValue<ValueExpr> EvalWithMessage(
        EvalEnvironment environment,
        CallExpr callExpr
    )
    {
        var (msgEnv, msg) = environment.Evaluate(callExpr.Args[0]);
        return msgEnv
            .UpdateValidatorState(v => v with { Message = msg })
            .Evaluate(callExpr.Args[1]);
    }

    public static EnvironmentValue<EvalExpr> ResolveValidation(
        EvalEnvironment environment,
        CallExpr callExpr
    )
    {
        var args = callExpr.Args;
        var path = environment.ResolveExpr(args[0]);
        var resolvedMust = path.Env.ResolveExpr(args[1]);
        var evalProps = resolvedMust.Env.Evaluate(args[2]);
        return evalProps
            .Env.UpdateValidatorState(valState =>
                valState with
                {
                    Rules = valState.Rules.Append(
                        new ResolvedRule(
                            path.Value.AsPath(),
                            resolvedMust.Value,
                            valState.Properties.ToDictionary()
                        )
                    ),
                    Properties = ImmutableDictionary<string, object?>.Empty,
                }
            )
            .WithExpr(ValueExpr.Null);
    }
}

public record Failure(CallExpr Call, IList<ValueExpr> EvaluatedArgs);

public record RuleFailure(IEnumerable<Failure> Failures, string? Message, ResolvedRule Rule);
