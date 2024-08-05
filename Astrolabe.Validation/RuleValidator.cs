using System.Collections;
using System.Collections.Immutable;
using System.Text.Json.Nodes;
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

public static class RuleValidator
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
        return EvalEnvironment
            .DataFrom(data)
            .WithVariables(
                ImmutableDictionary<string, EvalExpr>
                    .Empty.AddRange(DefaultFunctions.FunctionHandlers.Select(ToVariable))
                    .Add("$ValidatorState", new ValueExpr(ValidatorState.Empty))
                    .Add(
                        RuleFunction,
                        new ValueExpr(FunctionHandler.ResolveOnly(ResolveValidation))
                    )
                    .Add(
                        "WithMessage",
                        new ValueExpr(FunctionHandler.DefaultResolve(EvalWithMessage))
                    )
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
                    var (env, args) = e.EvalSelect(call.Args, (e2, x) => e2.Evaluate(x));
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
        var (evalEnvironment, args) = environment.EvalSelect(callExpr.Args, Interpreter.Evaluate);
        var argList = args.ToList();
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
            .WithNull();
    }

    public static List<RuleFailure> ValidateJson(
        JsonNode data,
        Rule rule,
        LetExpr? variables,
        Func<DataPath, IEnumerable<ResolvedRule>, IEnumerable<ResolvedRule>> adjustRules
    )
    {
        var baseEnv = FromData(JsonDataLookup.FromObject(data));
        return ValidateRules(baseEnv, rule, variables, adjustRules).Value.ToList();
    }

    public static EnvironmentValue<IEnumerable<RuleFailure>> ValidateRules(
        EvalEnvironment baseEnv,
        Rule rule,
        LetExpr? variables,
        Func<DataPath, IEnumerable<ResolvedRule>, IEnumerable<ResolvedRule>> adjustRules
    )
    {
        var ruleEnv = variables != null ? baseEnv.ResolveAndEvaluate(variables).Env : baseEnv;
        var ruleList = ruleEnv.EvaluateRule(rule);
        var allRules = ruleList.Value.ToList();
        var byPath = allRules.ToLookup(x => x.Path);
        var dataOrder = allRules.GetDataOrder();
        var validationResult = ruleEnv.EvalConcat(
            dataOrder,
            (de, p) =>
                de.EvalConcat(
                    adjustRules(p, byPath[p]),
                    (e, r) => e.EvaluateFailures(r).SingleOrEmpty()
                )
        );
        return validationResult;
    }

    public static EnvironmentValue<RuleFailure?> EvaluateFailures(
        this EvalEnvironment environment,
        ResolvedRule rule
    )
    {
        var (outEnv, result) = environment.Evaluate(rule.Must);
        RuleFailure? failure = null;
        var valEnv = outEnv.GetValidatorState();
        if (result.IsFalse())
        {
            failure = new RuleFailure(valEnv.Failures, valEnv.Message.AsString(), rule);
        }

        var failedData = result.IsFalse() ? valEnv.FailedData.Add(rule.Path) : valEnv.FailedData;
        var resetEnv = valEnv with
        {
            Properties = ImmutableDictionary<string, object?>.Empty,
            Message = ValueExpr.Null,
            Failures = [],
            FailedData = failedData
        };
        return (
            outEnv.UpdateValidatorState(_ => resetEnv) with
            {
                ValidData = dp => !failedData.Contains(dp)
            }
        ).WithValue(failure);
    }

    public static EnvironmentValue<IEnumerable<ResolvedRule>> EvaluateRule(
        this EvalEnvironment environment,
        Rule rule
    )
    {
        return environment.ResolveExpr(ToExpr(rule)).Map((v, e) => e.GetValidatorState().Rules);
    }

    private static EvalExpr ToExpr(Rule rule)
    {
        return rule switch
        {
            ForEachRule rulesForEach => DoRulesForEach(rulesForEach),
            SingleRule pathRule => DoPathRule(pathRule),
            MultiRule multi => DoMultiRule(multi)
        };

        EvalExpr DoMultiRule(MultiRule multiRule)
        {
            return new ArrayExpr(multiRule.Rules.Select(ToExpr));
        }

        EvalExpr DoPathRule(SingleRule pathRule)
        {
            return new CallExpr(
                RuleValidator.RuleFunction,
                [pathRule.Path, pathRule.Must, pathRule.Props]
            );
        }

        EvalExpr DoRulesForEach(ForEachRule rules)
        {
            var ruleExpr = ToExpr(rules.Rule);
            if (rules.Variables != null)
                ruleExpr = rules.Variables with { In = ruleExpr };
            return CallExpr.Map(rules.Path, new LambdaExpr(rules.Index.Name, ruleExpr));
        }
    }
}

public record Failure(CallExpr Call, IList<ValueExpr> EvaluatedArgs);

public record RuleFailure(IEnumerable<Failure> Failures, string? Message, ResolvedRule Rule);
