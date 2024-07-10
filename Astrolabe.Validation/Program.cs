using System.Text.Json.Nodes;
using Astrolabe.Validation;

// var rules = new TestDsl().Rules;
// var config = new JsonObject();
// config["startability"] = new JsonArray(10, 21);

// Console.WriteLine(Interpreter.Evaluate((!((NumberExpr<int>)10 == 10)).Expr, new EvalEnvironment(data)).Item2);
//
// Console.WriteLine(Interpreter.Evaluate((new NumberExpr<int>(10.ToExpr()) * 5 + 2 -
//                                         new NumberExpr<int>(new GetData("Width"))).Expr, new EvalEnvironment(data)).Item2);
// Console.WriteLine(rules[0]);
// Console.WriteLine(Interpreter.Evaluate(rules[0].Must.Expr, new EvalEnvironment(data)));
// var parsed = JsonNode.Parse(new FileStream("pbs-83t.json", FileMode.Open));
// var baseEnv = EvalEnvironment.FromData((JsonObject)parsed!, config)
//     .WithResult(Enumerable.Empty<ResolvedRule<VehicleDefinitionEdit>>());
//
// var loadedRules = ValidationLoader.LoadRules();
// var (env, allRules) = baseEnv.Env.EvaluateRule(loadedRules);
//
// // Console.WriteLine(string.Join("\n", allRules));
//
// var evaluatedRules = allRules.Select(x => (x.Path, x.Must, env.Evaluate(x.Must)));
// var failed = evaluatedRules
//     .Where(x => x.Item3.Value.IsFalse())
//     .Select(x => $"{x.Path} {x.Item2} {x.Item3.Env.Failure}");
//
// Console.WriteLine(string.Join("\n", failed));

Console.WriteLine("TODO");
