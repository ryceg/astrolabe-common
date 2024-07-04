using System.Text.Json.Nodes;
using Astrolabe.Validation;


var rules = new TestDsl().Rules;
var data = new JsonObject();
data["Width"] = 100;

Console.WriteLine(Interpreter.Evaluate((new NumberExpr<int>(10.ToExpr()) * 5 + 2 -
                                        new NumberExpr<int>(new FromPath("Width"))).Expr, new EvalEnvironment(data)).Item2);
// Console.WriteLine(rules[0]);
// Console.WriteLine(Interpreter.Evaluate(rules[0].Must.Expr, new EvalEnvironment(data)));

public class TestDsl : AbstractValidator<VehicleDefinitionEdit>
{
    public TestDsl()
    {
        AddRules([
            RuleFor(x => x.Width).Must(x => x + 10 >= 110),
            RuleFor(x => x.NotNullable).Must(x => x > 4),
            RuleFor(x => x.SteerAxleCompliant).Must(x => !x)
        ]);
    }
}
