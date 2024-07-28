using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Astrolabe.Evaluator;
using Astrolabe.Evaluator.Functions;

namespace Astrolabe.Validation;

public record ValidatorEnvironment(
    Func<DataPath, object?> GetData,
    DataPath BasePath,
    IEnumerable<Failure> Failures,
    ValueExpr Message,
    IEnumerable<ResolvedRule> Rules,
    ImmutableHashSet<DataPath> FailedData,
    ImmutableDictionary<string, object?> Properties,
    ImmutableDictionary<EvalExpr, EvalExpr> Replacements
) : EvalEnvironment
{
    public const string RuleFunction = "ValidatorRule";

    public static ValidatorEnvironment FromEnv(EvalEnvironment eval)
    {
        return (ValidatorEnvironment)eval;
    }

    public static ValidatorEnvironment FromData(Func<DataPath, object?> data)
    {
        return new ValidatorEnvironment(
            data,
            DataPath.Empty,
            [],
            ValueExpr.Null,
            [],
            ImmutableHashSet<DataPath>.Empty,
            ImmutableDictionary<string, object?>.Empty,
            ImmutableDictionary<EvalExpr, EvalExpr>.Empty
        );
    }

    public ValidatorEnvironment WithFailedPath(DataPath rulePath)
    {
        return this with { FailedData = FailedData.Add(rulePath) };
    }

    public EnvironmentValue<ValueExpr> EvaluateData(DataPath dataPath)
    {
        return FailedData.Contains(dataPath)
            ? this.WithNull()
            : this.WithValue(new ValueExpr(GetData(dataPath)));
    }

    public EvalExpr? GetReplacement(EvalExpr expr)
    {
        return CollectionExtensions.GetValueOrDefault(Replacements, expr);
    }

    public EvalEnvironment WithReplacement(EvalExpr expr, EvalExpr? value)
    {
        return this with
        {
            Replacements =
                value == null ? Replacements.Remove(expr) : Replacements.SetItem(expr, value)
        };
    }

    public EvalEnvironment MapReplacement(EvalExpr expr, Func<EvalExpr?, EvalExpr> mapValue)
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

    public EvalEnvironment WithBasePath(DataPath basePath)
    {
        return this with { BasePath = basePath };
    }

    public EnvironmentValue<EvalExpr> ResolveValCall(CallExpr callEnvExpr)
    {
        var args = callEnvExpr.Args;
        return callEnvExpr.Function switch
        {
            RuleFunction => ResolveRule(),
            _
                => this.EvaluateEach(args, (env, e) => env.ResolveExpr(e))
                    .Map(x => (EvalExpr)(callEnvExpr with { Args = x.ToList() }))
        };

        EnvironmentValue<EvalExpr> ResolveRule()
        {
            var path = this.ResolveExpr(args[0]);
            var resolvedMust = path.Env.ResolveExpr(args[1]);
            var evalProps = resolvedMust.Env.Evaluate(args[2]);
            var valEnv = FromEnv(evalProps.Env);
            return (
                valEnv with
                {
                    Rules = valEnv.Rules.Append(
                        new ResolvedRule(
                            path.Value.AsValue().AsPath(),
                            resolvedMust.Value,
                            valEnv.Properties.ToDictionary()
                        )
                    ),
                    Properties = ImmutableDictionary<string, object?>.Empty,
                }
            ).WithExpr(ValueExpr.Null);
        }
    }

    public EnvironmentValue<ValueExpr> EvaluateValCall(CallExpr callEnvExpr)
    {
        var args = callEnvExpr.Args;
        return callEnvExpr.Function switch
        {
            "WithProperty" => DoProp(),
            "WithMessage" => DoMessage()
        };

        EnvironmentValue<ValueExpr> DoMessage()
        {
            var (msgEnv, msg) = this.Evaluate(callEnvExpr.Args[0]);
            var valEnv = FromEnv(msgEnv);
            return (valEnv with { Message = msg }).Evaluate(callEnvExpr.Args[1]);
        }

        EnvironmentValue<ValueExpr> DoProp()
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

public record Failure(CallExpr Call, IList<ValueExpr> EvaluatedArgs);

public record RuleFailure(IEnumerable<Failure> Failures, string? Message, ResolvedRule Rule);
