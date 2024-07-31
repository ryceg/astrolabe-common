using System.Collections.Immutable;

namespace Astrolabe.Evaluator.Functions;

public static class DefaultFunctions
{
    public static string ExprValueToString(object? value)
    {
        return value switch
        {
            null => "",
            ArrayValue av => string.Join("", ValueExpr.ToList(av).Select(ExprValueToString)),
            ObjectValue => "{}",
            _ => value.ToString() ?? ""
        };
    }

    public static FunctionHandler UnaryNullOp(Func<object, object?> evaluate)
    {
        return FunctionHandler.DefaultEval(
            (args) =>
                args switch
                {
                    [{ } v1] => evaluate(v1),
                    [_] => null,
                    _ => throw new ArgumentException("Wrong number of args:" + args)
                }
        );
    }

    public static FunctionHandler BinOp(Func<object?, object?, object?> evaluate)
    {
        return FunctionHandler.DefaultEval(
            (args) =>
                args switch
                {
                    [var v1, var v2] => evaluate(v1, v2),
                    _ => throw new ArgumentException("Wrong number of args:" + args)
                }
        );
    }

    public static FunctionHandler BinNullOp(Func<object, object, object?> evaluate)
    {
        return FunctionHandler.DefaultEval(
            (args) =>
                args switch
                {
                    [{ } v1, { } v2] => evaluate(v1, v2),
                    [_, _] => null,
                    _ => throw new ArgumentException("Wrong number of args:" + args)
                }
        );
    }

    public static FunctionHandler BoolOp(Func<bool, bool, bool> func)
    {
        return BinNullOp(
            (a, b) =>
                (a, b) switch
                {
                    (bool b1, bool b2) => func(b1, b2),
                    _ => throw new ArgumentException("Bad args for bool op")
                }
        );
    }

    public static FunctionHandler NumberOp<TOutD, TOutL>(
        Func<double, double, TOutD> doubleOp,
        Func<long, long, TOutL> longOp
    )
    {
        return BinNullOp(
            (o1, o2) =>
            {
                if (ValueExpr.MaybeInteger(o1) is { } l1 && ValueExpr.MaybeInteger(o2) is { } l2)
                {
                    return longOp(l1, l2);
                }
                return doubleOp(ValueExpr.AsDouble(o1), ValueExpr.AsDouble(o2));
            }
        );
    }

    public static FunctionHandler EqualityFunc(bool not)
    {
        return BinNullOp((v1, v2) => not ^ Equals(AsEqualityCheck(v1), AsEqualityCheck(v2)));
    }

    public static object AsEqualityCheck(this object? v)
    {
        return v switch
        {
            int i => (double)i,
            long l => (double)l,
            not null => v,
            _ => throw new ArgumentException("Cannot be compared: " + v)
        };
    }

    private static readonly FunctionHandler AddNumberOp = NumberOp<double, long>(
        (d1, d2) => d1 + d2,
        (l1, l2) => l1 + l2
    );

    private static readonly FunctionHandler IfElseOp = FunctionHandler.DefaultEval(
        (args) =>
            args switch
            {
                [bool b, var thenVal, var elseVal] => b ? thenVal : elseVal,
                [null, _, _] => null,
                _ => throw new ArgumentException("Bad conditional: " + args),
            }
    );

    private static readonly FunctionHandler StringOp = FunctionHandler.DefaultEval(x =>
        ExprValueToString(ArrayValue.From(x))
    );

    public static FunctionHandler ArrayOp(Func<IList<object?>, object?> arrayFunc)
    {
        return FunctionHandler.DefaultEval(args =>
            args switch
            {
                [ArrayValue av] => CheckValues(ValueExpr.ToList(av)),
                _ => CheckValues(args)
            }
        );

        object? CheckValues(IList<object?> values)
        {
            if (values.Any(x => x == null))
                return null;
            return values.Any(x => x is ArrayValue)
                ? ArrayValue.From(values.Select(x => CheckValues(ValueExpr.ToList(x))))
                : arrayFunc(values);
        }
    }

    public static readonly Dictionary<string, FunctionHandler> FunctionHandlers =
        new()
        {
            { "+", AddNumberOp },
            { "-", NumberOp<double, long>((d1, d2) => d1 - d2, (l1, l2) => l1 - l2) },
            { "*", NumberOp<double, long>((d1, d2) => d1 * d2, (l1, l2) => l1 * l2) },
            { "/", NumberOp<double, double>((d1, d2) => d1 / d2, (l1, l2) => (double)l1 / l2) },
            { "=", EqualityFunc(false) },
            { "!=", EqualityFunc(true) },
            { "<", NumberOp((d1, d2) => d1 < d2, (l1, l2) => l1 < l2) },
            { "<=", NumberOp((d1, d2) => d1 <= d2, (l1, l2) => l1 <= l2) },
            { ">", NumberOp((d1, d2) => d1 > d2, (l1, l2) => l1 > l2) },
            { ">=", NumberOp((d1, d2) => d1 >= d2, (l1, l2) => l1 >= l2) },
            { "and", BoolOp((a, b) => a && b) },
            { "or", BoolOp((a, b) => a || b) },
            { "!", UnaryNullOp((a) => a is bool b ? !b : null) },
            { "?", IfElseOp },
            {
                "sum",
                ArrayOp(vals => vals.Select(ValueExpr.AsDouble).Aggregate(0d, (a, b) => a + b))
            },
            {
                "min",
                ArrayOp(vals =>
                    vals.Select(ValueExpr.AsDouble).Aggregate(double.MaxValue, Math.Min)
                )
            },
            {
                "max",
                ArrayOp(vals =>
                    vals.Select(ValueExpr.AsDouble).Aggregate(double.MinValue, Math.Max)
                )
            },
            { "count", ArrayOp(vals => vals.Count) },
            { "array", FunctionHandler.DefaultEval(args => ArrayValue.From(args).Flatten()) },
            {
                "notEmpty",
                FunctionHandler.DefaultEval(x =>
                    x[0] switch
                    {
                        string s => !string.IsNullOrWhiteSpace(s),
                        null => false,
                        _ => true
                    }
                )
            },
            { "string", StringOp },
            {
                "resolve",
                FunctionHandler.ResolveOnly(
                    (e, call) => e.ResolveAndEvaluate(call.Args[0]).Map(x => (EvalExpr)x)
                )
            },
            {
                "which",
                FunctionHandler.ResolveOnly(
                    (e, call) =>
                    {
                        return e.ResolveExpr(
                            call.Args.Aggregate(
                                new WhichState(ValueExpr.Null, null, null),
                                (s, x) => s.Next(x)
                            ).Current
                        );
                    }
                )
            },
            { "[", FilterFunctionHandler.Instance },
            { ".", MapFunctionHandler.Instance },
        };

    public static EvalEnvironment CreateEnvironment(Func<DataPath, object?> getData)
    {
        return new EvalEnvironment(
            getData,
            null,
            DataPath.Empty,
            ImmutableDictionary.CreateRange(
                FunctionHandlers.Select(x => new KeyValuePair<string, EvalExpr>(
                    x.Key,
                    new ValueExpr(x.Value)
                ))
            )
        );
    }

    record WhichState(EvalExpr Current, EvalExpr? Compare, EvalExpr? ToExpr)
    {
        public WhichState Next(EvalExpr expr)
        {
            if (Compare is null)
                return this with { Compare = expr };
            if (ToExpr is null)
                return this with { ToExpr = expr };
            return this with
            {
                Current = new CallExpr("?", [new CallExpr("=", [Compare, ToExpr]), expr, Current]),
                ToExpr = null
            };
        }
    }
}
