using System.Collections;
using System.ComponentModel.DataAnnotations;
using Astrolabe.Annotation;

namespace Astrolabe.Evaluator;

[JsonString]
public enum InbuiltFunction
{
    [Display(Name = "=")]
    Eq,

    [Display(Name = "!=")]
    Ne,

    [Display(Name = "<")]
    Lt,

    [Display(Name = "<=")]
    LtEq,

    [Display(Name = ">")]
    Gt,

    [Display(Name = ">=")]
    GtEq,

    [Display(Name = "and")]
    And,

    [Display(Name = "or")]
    Or,

    [Display(Name = "!")]
    Not,

    [Display(Name = "+")]
    Add,

    [Display(Name = "-")]
    Minus,

    [Display(Name = "*")]
    Multiply,

    [Display(Name = "/")]
    Divide,

    [Display(Name = "?")]
    IfElse,
    Sum,
    Count,
    String,
    Map,
    Filter
}

public static class InbuiltFunctions
{
    public static string VariableName(this InbuiltFunction func)
    {
        throw new NotImplementedException();
    }
}

public interface EvalExpr;

public record LetExpr(IEnumerable<(VarExpr, EvalExpr)> Vars, EvalExpr In) : EvalExpr
{
    public static LetExpr AddVar(LetExpr? letExpr, VarExpr varExpr, EvalExpr expr)
    {
        var varTuple = (varExpr, expr);
        if (letExpr == null)
            return new LetExpr([varTuple], ValueExpr.Null);
        return letExpr with { Vars = letExpr.Vars.Append(varTuple) };
    }
}

public record PathExpr(DataPath Path) : EvalExpr;

public record LambdaExpr(VarExpr Variable, EvalExpr Value) : EvalExpr;
public record ValueExpr(object? Value) : EvalExpr
{
    public static ValueExpr Null => new((object?)null);
    public static ValueExpr False => new(false);
    public static ValueExpr True => new(true);

    public static ValueExpr EmptyPath => new(DataPath.Empty);

    public static double AsDouble(object? v)
    {
        return v switch
        {
            int i => i,
            long l => l,
            double d => d,
            _ => throw new ArgumentException("Value is not a number: " + (v ?? "null"))
        };
    }

    public DataPath? MaybeDataPath()
    {
        return MaybeDataPath(Value);
    }

    public static DataPath? MaybeDataPath(object? v)
    {
        return v switch
        {
            DataPath dp => dp,
            int i => new IndexPath(i, DataPath.Empty),
            long l => new IndexPath((int)l, DataPath.Empty),
            double d => new IndexPath((int)d, DataPath.Empty),
            string s => new FieldPath(s, DataPath.Empty),
            _ => null
        };
    }

    public double? MaybeDouble()
    {
        return MaybeDouble(Value);
    }

    public static double? MaybeDouble(object? v)
    {
        return v switch
        {
            int i => i,
            long l => l,
            double d => d,
            _ => null
        };
    }

    public static ValueExpr From(bool? v)
    {
        return new ValueExpr(v);
    }

    public static ValueExpr From(string? v)
    {
        return new ValueExpr(v);
    }

    public static ValueExpr From(ArrayValue? v)
    {
        return new ValueExpr(v);
    }

    public static ValueExpr From(int? v)
    {
        return new ValueExpr(v);
    }

    public static ValueExpr From(double? v)
    {
        return new ValueExpr(v);
    }

    public static ValueExpr From(long? v)
    {
        return new ValueExpr(v);
    }

    public object? ToNative()
    {
        return ToNative(Value);
    }

    public static object? ToNative(object? v)
    {
        return v switch
        {
            ArrayValue av => av.Values.Cast<object?>().Select(ToNative),
            ObjectValue ov => ov.Object,
            _ => v
        };
    }

    public object? Flatten()
    {
        return Flatten(Value);
    }

    public static object? Flatten(object? v)
    {
        return v switch
        {
            ArrayValue av
                => av
                    .Values.Cast<object?>()
                    .SelectMany(v =>
                        Flatten(v) switch
                        {
                            IEnumerable<object?> res => res,
                            var o => [o]
                        }
                    ),
            ObjectValue ov => ov.Object,
            _ => v
        };
    }

    public static IList<object?> ToList(object? value)
    {
        return value switch
        {
            ArrayValue av => av.Values.Cast<object?>().ToList(),
            _ => [value]
        };
    }

    public IList<object?> AsList()
    {
        return ToList(Value);
    }
}

public record ArrayExpr(IEnumerable<EvalExpr> ValueExpr) : EvalExpr
{
    public override string ToString()
    {
        return $"ArrayExpr [{string.Join(", ", ValueExpr)}]";
    }
}

public record CallExpr(string Function, IList<EvalExpr> Args) : EvalExpr
{
    public override string ToString()
    {
        return $"{Function}({string.Join(", ", Args)})";
    }

    public CallExpr WithArgs(IEnumerable<EvalExpr> args)
    {
        return this with { Args = args.ToList() };
    }

    public static EvalExpr Inbuilt(InbuiltFunction inbuilt, IEnumerable<EvalExpr> args)
    {
        return new CallExpr(inbuilt.VariableName(), args.ToList());
    }
}

public record VarExpr(string Name) : EvalExpr
{
    private static int _indexCount;

    public override string ToString()
    {
        return $"${Name}";
    }

    public static VarExpr MakeNew(string name)
    {
        return new VarExpr(name + (++_indexCount));
    }

    public VarExpr Prepend(string extra)
    {
        return new VarExpr(extra + Name);
    }

    public EvalExpr Append(string append)
    {
        return new VarExpr(Name + append);
    }
}

public record ArrayValue(int Count, IEnumerable Values)
{
    public static ArrayValue From<T>(IEnumerable<T> enumerable)
    {
        var l = enumerable.ToList();
        return new ArrayValue(l.Count, l);
    }
}

public record ObjectValue(object Object);

public static class ValueExtensions
{
    public static bool IsData(this EvalExpr expr)
    {
        return expr is ValueExpr { Value: DataPath dp };
    }

    public static bool IsValue(this EvalExpr expr)
    {
        return expr is ValueExpr;
    }

    public static ValueExpr? MaybeValue(this EvalExpr expr)
    {
        return expr as ValueExpr;
    }

    public static bool IsNull(this EvalExpr expr)
    {
        return expr is ValueExpr { Value: null };
    }

    public static bool IsDataPath(this EvalExpr expr, DataPath dataPath)
    {
        return expr is ValueExpr { Value: DataPath dp } && dp.Equals(dataPath);
    }

    public static ValueExpr AsValue(this EvalExpr expr)
    {
        return (ValueExpr)expr;
    }

    public static VarExpr AsVar(this EvalExpr expr)
    {
        return (VarExpr)expr;
    }

    public static ArrayValue AsArray(this ValueExpr v)
    {
        return (v.Value as ArrayValue)!;
    }

    public static bool AsBool(this ValueExpr v)
    {
        return (bool)v.Value!;
    }

    public static int AsInt(this ValueExpr v)
    {
        return v.Value switch
        {
            double d => (int)d,
            long l => (int)l,
            int i => i
        };
    }

    public static object AsEqualityCheck(this ValueExpr v)
    {
        return v.Value switch
        {
            int i => (double)i,
            long l => (double)l,
            { } o => o,
            _ => throw new ArgumentException("Cannot be compared: " + v)
        };
    }

    public static long? MaybeInteger(this ValueExpr v)
    {
        return v.Value switch
        {
            int i => i,
            long l => l,
            _ => null
        };
    }

    public static bool IsEitherNull(this EvalExpr v, EvalExpr other)
    {
        return v.IsNull() || other.IsNull();
    }

    public static double AsDouble(this ValueExpr v)
    {
        return ValueExpr.AsDouble(v.Value);
    }

    public static DataPath AsPath(this ValueExpr v)
    {
        return v.Value switch
        {
            string s => new FieldPath(s, DataPath.Empty),
            DataPath dp => dp
        };
    }

    public static bool IsNull(this ValueExpr v)
    {
        return v.Value == null;
    }

    public static bool IsTrue(this ValueExpr v)
    {
        return v.Value is true;
    }

    public static bool IsFalse(this ValueExpr v)
    {
        return v.Value is false;
    }

    public static string AsString(this ValueExpr v)
    {
        return (string)v.Value!;
    }

    public static EvalExpr AndExpr(this EvalExpr expr, EvalExpr other)
    {
        return expr is ValueExpr(bool b)
            ? b
                ? other
                : expr
            : CallExpr.Inbuilt(InbuiltFunction.And, [expr, other]);
    }

    public static EvalExpr DotExpr(this EvalExpr expr, EvalExpr other)
    {
        return (expr, other) switch
        {
            (ValueExpr { Value: DataPath ps }, ValueExpr v) => new PathExpr(ApplyDot(ps, v)),
            _ => CallExpr.Inbuilt(InbuiltFunction.Map, [expr, other])
        };
    }

    public static DataPath ApplyDot(DataPath basePath, ValueExpr segment)
    {
        return segment switch
        {
            { Value: string s } => new FieldPath(s, basePath),
            _ => new IndexPath(segment.AsInt(), basePath)
        };
    }
}
