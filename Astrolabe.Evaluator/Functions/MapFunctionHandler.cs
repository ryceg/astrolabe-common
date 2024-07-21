namespace Astrolabe.Evaluator.Functions;

public class MapFunctionHandler : FunctionHandler
{
    public EnvironmentValue<(ExprValue, List<ExprValue>)> Evaluate(
        IList<Expr> args,
        EvalEnvironment environment
    )
    {
        throw new NotImplementedException();
    }

    public EnvironmentValue<Expr?> Resolve(IList<Expr> args, EvalEnvironment environment)
    {
        throw new NotImplementedException();
    }
}
