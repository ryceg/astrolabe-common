namespace Astrolabe.Evaluator.Functions;

using BoolNumberOp = NumberOp<bool, bool>;

public interface FunctionHandler
{
    EnvironmentValue<(ValueExpr, List<ValueExpr>)> Evaluate(
        IList<EvalExpr> args,
        EvalEnvironment environment
    );

    EnvironmentValue<EvalExpr> Resolve(CallExpr callExpr, EvalEnvironment environment);
}

public abstract class ResolveFirst : FunctionHandler
{
    public EnvironmentValue<(ValueExpr, List<ValueExpr>)> Evaluate(
        IList<EvalExpr> args,
        EvalEnvironment env
    )
    {
        var (nextEnv, evalArgs) = env.EvaluateEach(args, (e, expr) => e.Evaluate(expr));
        var argsEval = evalArgs.ToList();
        return nextEnv.WithValue((DoEvaluate(argsEval), argsEval));
    }

    public abstract ValueExpr DoEvaluate(IList<ValueExpr> evalArgs);

    public virtual EnvironmentValue<EvalExpr> Resolve(CallExpr callableExpr, EvalEnvironment env)
    {
        var (nextEnv, evalArgs) = env.EvaluateEach(
                callableExpr.Args,
                (e, expr) => e.ResolveExpr(expr)
            )
            .Map(x => x.ToList());
        return nextEnv.WithValue(
            DoResolve(evalArgs) is { } r ? r : callableExpr.WithArgs(evalArgs)
        );
    }

    public abstract EvalExpr? DoResolve(IList<EvalExpr> args);
}

public abstract class BinOp : ResolveFirst
{
    public override ValueExpr DoEvaluate(IList<ValueExpr> evalArgs)
    {
        var a1 = evalArgs[0];
        var a2 = evalArgs[1];
        return a1.IsEitherNull(a2) ? ValueExpr.Null : EvalBin(a1, a2);
    }

    public abstract ValueExpr EvalBin(ValueExpr a1, ValueExpr a2);

    public override EvalExpr? DoResolve(IList<EvalExpr> args)
    {
        var a1 = args[0];
        var a2 = args[1];
        if (a1.IsEitherNull(a2))
            return ValueExpr.Null;
        if (a1 is ValueExpr ev1 && !ev1.IsData() && a2 is ValueExpr ev2 && !ev2.IsData())
            return EvalBin(ev1, ev2);
        return null;
    }
}

public class EqualityFunc(bool not) : BinOp
{
    public override ValueExpr EvalBin(ValueExpr a1, ValueExpr a2)
    {
        return ValueExpr.From(not ^ a1.AsEqualityCheck().Equals(a2.AsEqualityCheck()));
    }
}

public class NumberOp<TOutD, TOutL>(
    Func<double, double, TOutD> doubleOp,
    Func<long, long, TOutL> longOp
) : BinOp
{
    public override ValueExpr EvalBin(ValueExpr o1, ValueExpr o2)
    {
        if ((o1.MaybeInteger(), o2.MaybeInteger()) is ({ } l1, { } l2))
        {
            return new ValueExpr(longOp(l1, l2));
        }

        return new ValueExpr(
            (o1.MaybeDouble(), o2.MaybeDouble()) switch
            {
                ({ } d1, { } d2) => doubleOp(d1, d2),
                _ => null
            }
        );
    }
}

public class AndOp : BinOp
{
    public override ValueExpr EvalBin(ValueExpr a1, ValueExpr a2)
    {
        return ValueExpr.From(a1.AsBool() && a2.AsBool());
    }

    public override EvalExpr? DoResolve(IList<EvalExpr> args)
    {
        return (args[0], args[1]) switch
        {
            (ValueExpr { Value: true }, _) => args[1],
            (_, ValueExpr { Value: true }) => args[0],
            (ValueExpr { Value: false }, _) => ValueExpr.False,
            (_, ValueExpr { Value: false }) => ValueExpr.False,
            _ => base.DoResolve(args)
        };
    }
}

public class OrOp : BinOp
{
    public override ValueExpr EvalBin(ValueExpr a1, ValueExpr a2)
    {
        return ValueExpr.From(a1.AsBool() || a2.AsBool());
    }
}

public class NotOp : ResolveFirst
{
    public override ValueExpr DoEvaluate(IList<ValueExpr> args)
    {
        return args[0].IsNull() ? ValueExpr.Null : ValueExpr.From(!args[0].AsBool());
    }

    public override EvalExpr? DoResolve(IList<EvalExpr> args)
    {
        return args[0] is ValueExpr { Value: bool b } ? ValueExpr.From(!b) : null;
    }
}

public class IfElseOp : ResolveFirst
{
    public override ValueExpr DoEvaluate(IList<ValueExpr> args)
    {
        var ifE = args[0];
        return ifE.IsNull()
            ? ValueExpr.Null
            : ifE.AsBool()
                ? args[1]
                : args[2];
    }

    public override EvalExpr? DoResolve(IList<EvalExpr> args)
    {
        var ifE = args[0];
        if (ifE.IsNull())
            return ValueExpr.Null;
        return ifE is ValueExpr { Value: bool b }
            ? b
                ? args[1]
                : args[2]
            : null;
    }
}

public abstract class ResolveIfValue : ResolveFirst
{
    public override EvalExpr? DoResolve(IList<EvalExpr> args)
    {
        if (args[0].MaybeValue() is { Value: not DataPath } v)
        {
            return DoEvaluate([v]);
        }
        return null;
    }
}

public abstract class ArrayOp : ResolveIfValue
{
    public override ValueExpr DoEvaluate(IList<ValueExpr> args)
    {
        var asList = args[0].AsList();
        return EvalArray(asList);
    }

    private ValueExpr EvalArray(IList<object?> asList)
    {
        if (asList.Any(x => x is ArrayValue))
        {
            return ValueExpr.From(
                ArrayValue.From(asList.Select(x => EvalArray(ValueExpr.ToList(x)).Value))
            );
        }

        return EvalArrayOp(asList);
    }

    protected abstract ValueExpr EvalArrayOp(IList<object?> arrayValues);
}

public class AggregateNumberOp(NumberOp<double, long> aggregate) : ArrayOp
{
    protected override ValueExpr EvalArrayOp(IList<object?> values)
    {
        return values.Aggregate(
            ValueExpr.From(0d),
            (a, b) => aggregate.EvalBin(a, new ValueExpr(b))
        );
    }
}

public class CountOp : ArrayOp
{
    protected override ValueExpr EvalArrayOp(IList<object?> asList)
    {
        return ValueExpr.From(asList.Count);
    }
}

public class StringOp : ResolveIfValue
{
    public override ValueExpr DoEvaluate(IList<ValueExpr> args)
    {
        return ValueExpr.From(ToString(args[0].Value));
    }

    public static string ToString(object? value)
    {
        return value switch
        {
            null => "",
            ArrayValue av => string.Join("", ValueExpr.ToList(av).Select(ToString)),
            ObjectValue => "{}",
            _ => value.ToString() ?? ""
        };
    }
}

public static class DefaultFunctions
{
    private static readonly NumberOp<double, long> AddNumberOp = new NumberOp<double, long>(
        (d1, d2) => d1 + d2,
        (l1, l2) => l1 + l2
    );
    public static readonly Dictionary<InbuiltFunction, FunctionHandler> FunctionHandlers =
        new()
        {
            { InbuiltFunction.Add, AddNumberOp },
            {
                InbuiltFunction.Minus,
                new NumberOp<double, long>((d1, d2) => d1 - d2, (l1, l2) => l1 - l2)
            },
            {
                InbuiltFunction.Multiply,
                new NumberOp<double, long>((d1, d2) => d1 * d2, (l1, l2) => l1 * l2)
            },
            {
                InbuiltFunction.Divide,
                new NumberOp<double, double>((d1, d2) => d1 / d2, (l1, l2) => (double)l1 / l2)
            },
            { InbuiltFunction.Eq, new EqualityFunc(false) },
            { InbuiltFunction.Ne, new EqualityFunc(true) },
            { InbuiltFunction.Lt, new BoolNumberOp((d1, d2) => d1 < d2, (l1, l2) => l1 < l2) },
            { InbuiltFunction.LtEq, new BoolNumberOp((d1, d2) => d1 <= d2, (l1, l2) => l1 <= l2) },
            { InbuiltFunction.Gt, new BoolNumberOp((d1, d2) => d1 > d2, (l1, l2) => l1 > l2) },
            { InbuiltFunction.GtEq, new BoolNumberOp((d1, d2) => d1 >= d2, (l1, l2) => l1 >= l2) },
            { InbuiltFunction.And, new AndOp() },
            { InbuiltFunction.Or, new OrOp() },
            { InbuiltFunction.Not, new NotOp() },
            { InbuiltFunction.IfElse, new IfElseOp() },
            { InbuiltFunction.Sum, new AggregateNumberOp(AddNumberOp) },
            { InbuiltFunction.Count, new CountOp() },
            { InbuiltFunction.String, new StringOp() },
            { InbuiltFunction.Filter, new FilterFunctionHandler() },
            { InbuiltFunction.Map, new MapFunctionHandler() },
        };
}
