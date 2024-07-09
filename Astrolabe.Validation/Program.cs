using System.Text.Json.Nodes;
using Astrolabe.Validation;


var rules = new TestDsl().Rules;
var config = new JsonObject();
config["startability"] = new JsonArray(10, 21);

// Console.WriteLine(Interpreter.Evaluate((!((NumberExpr<int>)10 == 10)).Expr, new EvalEnvironment(data)).Item2);
//
// Console.WriteLine(Interpreter.Evaluate((new NumberExpr<int>(10.ToExpr()) * 5 + 2 -
//                                         new NumberExpr<int>(new GetData("Width"))).Expr, new EvalEnvironment(data)).Item2);
// Console.WriteLine(rules[0]);
// Console.WriteLine(Interpreter.Evaluate(rules[0].Must.Expr, new EvalEnvironment(data)));
var parsed = JsonNode.Parse(new FileStream("pbs-83t.json", FileMode.Open));

var (env, allRules) = rules.Aggregate(
    EvalEnvironment.FromData((JsonObject)parsed!, config).WithResult(Enumerable.Empty<ResolvedRule<VehicleDefinitionEdit>>()),
    (acc, r) => acc.Env.EvaluateRule(r).AppendTo(acc));

var evaledRules = allRules.Select(x => (x.Path, env.Evaluate(x.Must)));
var failed = evaledRules.Where(x => !x.Item2.Value.AsBool())
    .Select(x => $"{x.Path} {x.Item2.Env.Failure}");

Console.WriteLine(string.Join("\n", failed));

public class TestDsl : AbstractValidator<VehicleDefinitionEdit>
{
    public TestDsl()
    {
        AddRules([
            RuleFor(x => x.Length).Min(26, true).Max(36.5),
            RuleFor(x => x.Width).Min(2.4).Max(2.55),
            RuleFor(x => x.Height).Min(2.0, true).Max(4.6),
            RulesFor(x => x.Components, (c) =>
            [
                c.RulesFor(x => x.AxleGroups, (g) =>
                {
                    return
                    [
                        g.RulesFor(x => x.Axles, (a) =>
                        {
                            return
                            [
                                a.RuleFor(x => x.TyreSize)
                                    .IfElse(c.Index == 0 & g.Index == 1,
                                        x => x.Min(225).Max(315),
                                        x => x.Min(225).Max(485))
                            ];
                        }),
                    ];
                }),
            ])
        ]);
    }

    // new ValidationRange(26, 36.5, true),
    // new ValidationRange(2.4, 2.55),
    // new ValidationRange(2.0, 4.6),
    // new ParameterConstraint[]
    // {
    //     // new(ConstraintType.GCW, new ValidationRange(2.3, 2.55)),
    //     // see HVS-816 comment - round1 because disabled
    //     new(
    //         ConstraintType.TyreSize,
    //         new ValidationRange(225, 315),
    //         new ConstraintLocation(0, 1, null)
    //     ),
    //     new(ConstraintType.TyreSize, new ValidationRange(225, 485)),
    // }
    // .Concat(PBSTareMassConstraints(cl))
    //     .Concat(
    //     PBSAxleSpacingConstraints(cl)
    //         .Concat(PBSGCWConstraints(cl).Concat(PbsPerformanceConstraints()))
    // )
}