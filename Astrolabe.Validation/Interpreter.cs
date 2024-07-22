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
        return environment
            .ResolveExpr(ToExpr(rule))
            .Map((v, e) => ValidatorEnvironment.FromEnv(e).Rules);
    }

    private static Expr ToExpr(Rule rule)
    {
        return rule switch
        {
            ForEachRule rulesForEach => DoRulesForEach(rulesForEach),
            SingleRule pathRule => DoPathRule(pathRule),
            MultiRule multi => DoMultiRule(multi)
        };

        Expr DoMultiRule(MultiRule multiRule)
        {
            return new ArrayExpr(multiRule.Rules.Select(ToExpr));
        }

        Expr DoPathRule(SingleRule pathRule)
        {
            return new CallEnvExpr(
                ValidatorEnvironment.RuleFunction,
                [pathRule.Path, pathRule.Must, pathRule.Props]
            );
        }

        Expr DoRulesForEach(ForEachRule rules)
        {
            var ruleExpr = ToExpr(rules.Rule);
            if (rules.Variables != null)
                ruleExpr = rules.Variables with { In = ruleExpr };
            return new CallExpr(
                InbuiltFunction.Map,
                [rules.Path, new LambdaExpr(rules.Index, ruleExpr)]
            );
        }
    }
}
