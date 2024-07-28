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
            "=" or "!=" or ">" or "<" or ">=" or "<=" => WrapFunc(func.Value),
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

    // public ValidatorEnvironment WithFailedPath(DataPath rulePath)
    // {
    //     return this with { FailedData = FailedData.Add(rulePath) };
    // }
    //
    // public EvalExpr? GetVariable(string expr)
    // {
    //     return CollectionExtensions.GetValueOrDefault(Replacements, expr);
    // }
    //
    // public EvalEnvironment WithVariable(string name, EvalExpr? value)
    // {
    //     return this with
    //     {
    //         Replacements =
    //             value == null ? Replacements.Remove(name) : Replacements.SetItem(name, value)
    //     };
    // }

    // public EnvironmentValue<ValueExpr> EvaluateCall(CallExpr callExpr)
    // {
    //     if (callExpr is CallExpr callEnvExpr)
    //         return EvaluateValCall(callEnvExpr);
    //     if (callExpr is not CallExpr ce)
    //         return this.WithNull();
    //     var envResult = DefaultFunctions
    //         .FunctionHandlers[ce.Function]
    //         .Evaluate(callExpr.Args, this);
    //     var result = envResult.Value.Item1;
    //     if (
    //         result.IsFalse()
    //         && ce.Function
    //             is InbuiltFunction.Eq
    //                 or InbuiltFunction.Ne
    //                 or InbuiltFunction.Gt
    //                 or InbuiltFunction.Lt
    //                 or InbuiltFunction.GtEq
    //                 or InbuiltFunction.LtEq
    //     )
    //     {
    //         return (
    //             FromEnv(envResult.Env) with
    //             {
    //                 Failures = Failures.Append(new Failure(ce, envResult.Value.Item2))
    //             }
    //         ).WithExprValue(ValueExpr.False);
    //     }
    //     return envResult.Env.WithValue(result);
    // }
    //
    // public EnvironmentValue<EvalExpr> ResolveCall(CallExpr callExpr)
    // {
    //     if (callExpr is CallExpr callEnvExpr)
    //         return ResolveValCall(callEnvExpr);
    //     if (callExpr is not CallExpr ce)
    //         return this.WithExpr(callExpr);
    //     return DefaultFunctions.FunctionHandlers[ce.Function].Resolve(callExpr, this);
    // }

    // public EvalEnvironment WithBasePath(DataPath basePath)
    // {
    //     return this with { BasePath = basePath };
    // }
}

public record Failure(CallExpr Call, IList<ValueExpr> EvaluatedArgs);

public record RuleFailure(IEnumerable<Failure> Failures, string? Message, ResolvedRule Rule);
