using System.Collections;
using Astrolabe.Annotation;

namespace Astrolabe.Evaluator;

[JsonString]
public enum InbuiltFunction
{
    Eq,
    Lt,
    LtEq,
    Gt,
    GtEq,
    Ne,
    And,
    Or,
    Not,
    Add,
    Minus,
    Multiply,
    Divide,
    IfElse,
    Sum,
    Count
}

public interface Expr;

public record DotExpr(Expr Base, Expr Segment) : Expr;

public record MapExpr(Expr Array, Expr ElemPath, Expr MapTo) : Expr;

public record ResolveEval(Expr Expr) : Expr;

public record ExprValue(object? Value) : Expr
{
    public static ExprValue Null => new((object?)null);
    public static ExprValue False => new(false);
    public static ExprValue True => new(true);

    public static ExprValue EmptyPath => new(DataPath.Empty);

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

    public static ExprValue From(bool? v)
    {
        return new ExprValue(v);
    }

    public static ExprValue From(string? v)
    {
        return new ExprValue(v);
    }

    public static ExprValue From(ArrayValue? v)
    {
        return new ExprValue(v);
    }

    public static ExprValue From(int? v)
    {
        return new ExprValue(v);
    }

    public static ExprValue From(double? v)
    {
        return new ExprValue(v);
    }

    public static ExprValue From(long? v)
    {
        return new ExprValue(v);
    }

    public static ExprValue From(DataPath? v)
    {
        return new ExprValue(v);
    }
}

public record ArrayExpr(IEnumerable<Expr> ValueExpr) : Expr
{
    public override string ToString()
    {
        return $"[{string.Join(", ", ValueExpr)}]";
    }
}

public interface CallableExpr : Expr
{
    IList<Expr> Args { get; }

    CallableExpr WithArgs(IEnumerable<Expr> args);
}

public record CallExpr(InbuiltFunction Function, IList<Expr> Args) : CallableExpr
{
    public override string ToString()
    {
        return $"{Function}({string.Join(", ", Args)})";
    }

    public CallableExpr WithArgs(IEnumerable<Expr> args)
    {
        return this with { Args = args.ToList() };
    }
}

public record CallEnvExpr(string Function, IList<Expr> Args) : CallableExpr
{
    public override string ToString()
    {
        return $"{Function}({string.Join(", ", Args)})";
    }

    public CallableExpr WithArgs(IEnumerable<Expr> args)
    {
        return this with { Args = args.ToList() };
    }
}

public record VarExpr(string Name, int IndexId) : Expr
{
    private static int _indexCount;

    public override string ToString()
    {
        return $"${Name}{IndexId}";
    }

    public static VarExpr MakeNew(string name)
    {
        return new VarExpr(name, ++_indexCount);
    }

    public VarExpr Prepend(string extra)
    {
        return new VarExpr(extra + Name, IndexId);
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

public static class ValueExtensions
{
    public static bool IsData(this Expr expr)
    {
        return expr is ExprValue { Value: DataPath dp };
    }

    public static bool IsValue(this Expr expr)
    {
        return expr is ExprValue;
    }

    public static bool IsNull(this Expr expr)
    {
        return expr is ExprValue { Value: null };
    }

    public static bool IsDataPath(this Expr expr, DataPath dataPath)
    {
        return expr is ExprValue { Value: DataPath dp } && dp.Equals(dataPath);
    }

    public static ExprValue AsValue(this Expr expr)
    {
        return (ExprValue)expr;
    }

    public static VarExpr AsVar(this Expr expr)
    {
        return (VarExpr)expr;
    }

    public static ArrayValue AsArray(this ExprValue v)
    {
        return (v.Value as ArrayValue)!;
    }

    public static bool AsBool(this ExprValue v)
    {
        return (bool)v.Value!;
    }

    public static int AsInt(this ExprValue v)
    {
        return (int)v.Value!;
    }

    public static object AsEqualityCheck(this ExprValue v)
    {
        return v.Value switch
        {
            int i => (double)i,
            long l => (double)l,
            { } o => o,
            _ => throw new ArgumentException("Cannot be compared: " + v)
        };
    }

    public static long? MaybeLong(this ExprValue v)
    {
        return v.Value switch
        {
            int or long => (long)v.Value,
            _ => null
        };
    }

    public static bool IsEitherNull(this Expr v, Expr other)
    {
        return v.IsNull() || other.IsNull();
    }

    public static double AsDouble(this ExprValue v)
    {
        return ExprValue.AsDouble(v.Value);
    }

    public static DataPath AsPath(this ExprValue v)
    {
        return (DataPath)v.Value!;
    }

    public static bool IsNull(this ExprValue v)
    {
        return v.Value == null;
    }

    public static bool IsTrue(this ExprValue v)
    {
        return v.Value is true;
    }

    public static bool IsFalse(this ExprValue v)
    {
        return v.Value is false;
    }

    public static string AsString(this ExprValue v)
    {
        return (string)v.Value!;
    }

    public static Expr AndExpr(this Expr expr, Expr other)
    {
        return expr is ExprValue(bool b)
            ? b
                ? other
                : expr
            : new CallExpr(InbuiltFunction.And, [expr, other]);
    }

    public static Expr DotExpr(this Expr expr, Expr other)
    {
        return (expr, other) switch
        {
            (ExprValue { Value: DataPath ps }, ExprValue v) => ExprValue.From(ApplyDot(ps, v)),
            _ => new DotExpr(expr, other)
        };
    }

    public static DataPath ApplyDot(DataPath basePath, ExprValue segment)
    {
        return segment switch
        {
            { Value: string s } => new FieldPath(s, basePath),
            _ => new IndexPath(segment.AsInt(), basePath)
        };
    }
}
