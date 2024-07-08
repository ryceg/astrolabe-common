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
    (EvalEnvironment.FromData((JsonObject)parsed!, config), Enumerable.Empty<ResolvedRule<VehicleDefinitionEdit>>()),
    (acc, r) =>
    {
        var res = Interpreter.EvaluateRule(r, acc.Item1);
        return (res.Item1, acc.Item2.Concat(res.Item2));
    });

var failed = allRules.Where(x => Interpreter.Evaluate(x.Must, env).Item2 is BoolValue { Value: false });

Console.WriteLine(string.Join("\n", failed));

public class TestDsl : AbstractValidator<VehicleDefinitionEdit>
{
    public TestDsl()
    {
        AddRules([
            RuleFor(x => x.Width).Must(x => x + 10 < 110),
            RuleFor(x => x.Startability).Constrained(),
            // RuleFor(x => x.NotNullable).Must(x => x > 4),
            RuleFor(x => x.SteerAxleCompliant).Must(x => !x),
            RulesFor(x => x.Components, (p) =>
            [
                p.RuleFor(x => x.Tare).Must(x => x > 3)
            ])
        ]);
    }
}

public class TestConstraints : AbstractValidator<VehicleDefinitionEdit>
{
    public TestConstraints()
    {
        AddRules([
            RuleFor(x => x.Width).Constraint(0, 10),
            RuleFor(x => x.Startability).Constrained(),
            // RuleFor(x => x.NotNullable).Must(x => x > 4),
            RuleFor(x => x.SteerAxleCompliant).Must(x => !x),
            RulesFor(x => x.Components, (p) =>
            [
                p.RuleFor(x => x.Tare).Must(x => x > 3)
            ])
        ]);
    }
}