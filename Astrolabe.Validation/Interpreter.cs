using System.Collections.Immutable;
using Astrolabe.Evaluator;

namespace Astrolabe.Validation;

public static class Interpreter
{
    public static EnvironmentValue<RuleFailure?> EvaluateFailures(
        this EvalEnvironment environment,
        ResolvedRule rule
    )
    {
        var (outEnv, result) = environment.Evaluate(rule.Must);
        RuleFailure? failure = null;
        var valEnv = ValidatorEnvironment.FromEnv(outEnv);
        if (result.IsFalse())
        {
            failure = new RuleFailure(valEnv.Failures, valEnv.Message.AsString(), rule);
        }

        var resetEnv = valEnv with
        {
            Properties = ImmutableDictionary<string, object?>.Empty,
            Message = ExprValue.Null,
            Failures = [],
            FailedData = result.IsFalse() ? valEnv.FailedData.Add(rule.Path) : valEnv.FailedData
        };
        return resetEnv.WithValue(failure);
    }

    public static EnvironmentValue<IEnumerable<ResolvedRule>> EvaluateRule(
        this EvalEnvironment environment,
        Rule rule
    )
    {
        return rule switch
        {
            ForEachRule rulesForEach => DoRulesForEach(rulesForEach),
            SingleRule pathRule => DoPathRule(pathRule),
            MultiRule multi => DoMultiRule(multi)
        };

        EnvironmentValue<IEnumerable<ResolvedRule>> DoMultiRule(MultiRule multiRule)
        {
            return environment.EvaluateAll(multiRule.Rules, EvaluateRule);
        }

        EnvironmentValue<IEnumerable<ResolvedRule>> DoPathRule(SingleRule pathRule)
        {
            var (pathEnv, segments) = environment.ResolveExpr(pathRule.Path);
            var (mustEnv, must) = pathEnv.ResolveExpr(pathRule.Must);
            var (propsEnv, props) = mustEnv.ResolveExpr(pathRule.Props);
            var propsResult = propsEnv.Evaluate(props);
            return propsEnv
                .WithValue(
                    new ResolvedRule(
                        ((ExprValue)segments).AsPath(),
                        must,
                        ValidatorEnvironment.FromEnv(propsResult.Env).Properties
                    )
                )
                .Single();
        }

        EnvironmentValue<IEnumerable<ResolvedRule>> DoRulesForEach(ForEachRule rules)
        {
            var (pathEnv, collectionSeg) = environment.ResolveExpr(rules.Path);
            var indexExpr = rules.Index;
            var runningIndexExpr = indexExpr.AsVar().Prepend("Total");
            var runningIndexOffset =
                pathEnv.GetReplacement(runningIndexExpr)?.AsValue().AsInt() ?? 0;
            var nextEnv = pathEnv.WithReplacement(
                runningIndexExpr,
                ExprValue.From(runningIndexOffset)
            );

            var dataCollection = nextEnv
                .EvaluateData(collectionSeg.AsValue().AsPath())
                .AsValue()
                .Value;
            if (dataCollection is ArrayValue array)
            {
                return nextEnv.EvaluateAll(
                    Enumerable.Range(0, array.Count),
                    (env, index) =>
                    {
                        var envWithIndex = env.WithReplacement(indexExpr, ExprValue.From(index));
                        return envWithIndex
                            .EvaluateRule(rules.Rule)
                            .WithReplacement(
                                runningIndexExpr,
                                ExprValue.From(runningIndexOffset + index + 1)
                            );
                    }
                );
            }

            throw new ArgumentException($"Not an array: {dataCollection?.GetType()}");
        }
    }
}
