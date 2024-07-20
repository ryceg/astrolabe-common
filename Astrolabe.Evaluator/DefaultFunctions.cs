namespace Astrolabe.Evaluator;

using BoolNumberOp = NumberOp<bool, bool>;

public interface FunctionHandler
{
    ExprValue Evaluate(IList<ExprValue> args);

    Expr? Resolve(IList<Expr> args);
}

public abstract class BinOp : FunctionHandler
{
    public ExprValue Evaluate(IList<ExprValue> args)
    {
        var a1 = args[0];
        var a2 = args[1];
        return a1.IsEitherNull(a2) ? ExprValue.Null : EvalBin(a1, a2);
    }

    public abstract ExprValue EvalBin(ExprValue a1, ExprValue a2);

    public virtual Expr? Resolve(IList<Expr> args)
    {
        var a1 = args[0];
        var a2 = args[1];
        if (a1.IsEitherNull(a2))
            return ExprValue.Null;
        if (a1 is ExprValue ev1 && !ev1.IsData() && a2 is ExprValue ev2 && !ev2.IsData())
            return EvalBin(ev1, ev2);
        return null;
    }
}

public class EqualityFunc(bool not) : BinOp
{
    public override ExprValue EvalBin(ExprValue a1, ExprValue a2)
    {
        return ExprValue.From(not ^ a1.AsEqualityCheck().Equals(a2.AsEqualityCheck()));
    }
}

public class NumberOp<TOutD, TOutL>(
    Func<double, double, TOutD> doubleOp,
    Func<long, long, TOutL> longOp
) : BinOp
{
    public override ExprValue EvalBin(ExprValue o1, ExprValue o2)
    {
        if ((o1.MaybeInteger(), o2.MaybeInteger()) is ({ } l1, { } l2))
        {
            return new ExprValue(longOp(l1, l2));
        }

        return new ExprValue(
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
    public override ExprValue EvalBin(ExprValue a1, ExprValue a2)
    {
        return ExprValue.From(a1.AsBool() && a2.AsBool());
    }

    public override Expr? Resolve(IList<Expr> args)
    {
        return (args[0], args[1]) switch
        {
            (ExprValue { Value: true }, _) => args[1],
            (_, ExprValue { Value: true }) => args[0],
            (ExprValue { Value: false }, _) => ExprValue.False,
            (_, ExprValue { Value: false }) => ExprValue.False,
            _ => base.Resolve(args)
        };
    }
}

public class OrOp : BinOp
{
    public override ExprValue EvalBin(ExprValue a1, ExprValue a2)
    {
        return ExprValue.From(a1.AsBool() || a2.AsBool());
    }
}

public class NotOp : FunctionHandler
{
    public ExprValue Evaluate(IList<ExprValue> args)
    {
        var arg = args[0];
        return arg.IsNull() ? ExprValue.Null : ExprValue.From(!arg.AsBool());
    }

    public Expr? Resolve(IList<Expr> args)
    {
        return args[0] is ExprValue { Value: bool b } ? ExprValue.From(!b) : null;
    }
}

public class IfElseOp : FunctionHandler
{
    public ExprValue Evaluate(IList<ExprValue> args)
    {
        if (args[0].IsNull())
            return ExprValue.Null;
        return args[0].AsBool() ? args[1] : args[2];
    }

    public Expr? Resolve(IList<Expr> args)
    {
        if (args[0].IsNull())
            return ExprValue.Null;
        if (args[0] is ExprValue { Value: bool b })
        {
            return b ? args[1] : args[2];
        }
        return null;
    }
}

public abstract class ResolveIfValue : FunctionHandler
{
    public abstract ExprValue Evaluate(IList<ExprValue> args);

    public Expr? Resolve(IList<Expr> args)
    {
        if (args[0].MaybeValue() is { } v)
        {
            return Evaluate([v]);
        }
        return null;
    }
}

public abstract class ArrayOp : ResolveIfValue
{
    public override ExprValue Evaluate(IList<ExprValue> args)
    {
        var asList = args[0].AsList();
        return EvalArray(asList);
    }

    private ExprValue EvalArray(IList<object?> asList)
    {
        if (asList.Any(x => x is ArrayValue))
        {
            return ExprValue.From(
                ArrayValue.From(asList.Select(x => EvalArray(ExprValue.ToList(x)).Value))
            );
        }

        return EvalArrayOp(asList);
    }

    protected abstract ExprValue EvalArrayOp(IList<object?> arrayValues);
}

public class AggregateNumberOp(NumberOp<double, long> aggregate) : ArrayOp
{
    protected override ExprValue EvalArrayOp(IList<object?> values)
    {
        return values.Aggregate(
            ExprValue.From(0d),
            (a, b) => aggregate.EvalBin(a, new ExprValue(b))
        );
    }
}

public class CountOp : ArrayOp
{
    protected override ExprValue EvalArrayOp(IList<object?> asList)
    {
        return ExprValue.From(asList.Count);
    }
}

public class StringOp : ResolveIfValue
{
    public override ExprValue Evaluate(IList<ExprValue> args)
    {
        return ExprValue.From(ToString(args[0].Value));
    }

    public static string ToString(object? value)
    {
        return value switch
        {
            null => "",
            ArrayValue av => string.Join("", ExprValue.ToList(av).Select(ToString)),
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
            { InbuiltFunction.String, new StringOp() }
        };
}
