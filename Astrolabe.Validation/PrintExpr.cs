using Astrolabe.Evaluator;

namespace Astrolabe.Validation;

public static class PrintExpr
{
    public static string Print(this ResolvedRule rule)
    {
        return $"ResolvedRule " + rule.Path + " " + rule.Must.Print();
    }

    public static string Print(this Rule rule)
    {
        return rule switch
        {
            MultiRule multiRule
                => $"[\n{string.Join("\n", multiRule.Rules.Select(x => x.Print()))}\n]",
            SingleRule pathRule => $"Rule {pathRule.Path.Print()}: {pathRule.Must.Print()}",
            ForEachRule rulesForEach
                => $"ForEachRule {rulesForEach.Path.Print()} {rulesForEach.Index.Print()} {rulesForEach.Rule.Print()}",
            _ => throw new ArgumentOutOfRangeException(nameof(rule))
        };
    }
}
