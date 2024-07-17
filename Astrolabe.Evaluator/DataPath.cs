namespace Astrolabe.Evaluator;

public interface DataPath
{
    public static readonly DataPath Empty = new EmptyPath();
}

public record EmptyPath : DataPath;

public record FieldPath(string Field, DataPath Parent) : DataPath
{
    public override string ToString()
    {
        return "Path {" + this.ToPathString() + "}";
    }
}

public record IndexPath(int Index, DataPath Parent) : DataPath
{
    public override string ToString()
    {
        return this.ToPathString();
    }
}

public static class DataPathExtensions
{
    public static DataPath Concat(this DataPath dp1, DataPath dp2)
    {
        return dp2 switch
        {
            EmptyPath => dp1,
            FieldPath { Field: var f, Parent: var cp } => new FieldPath(f, dp1.Concat(cp)),
            IndexPath { Index: var i, Parent: var cp } => new IndexPath(i, dp1.Concat(cp)),
        };
    }

    public static string ToPathString(this DataPath segments)
    {
        return segments switch
        {
            EmptyPath => "",
            FieldPath { Field: var f, Parent: EmptyPath } => f,
            FieldPath { Field: var f, Parent: var p } => $"{p.ToPathString()}.{f}",
            IndexPath { Index: var i, Parent: var p } => $"{p.ToPathString()}[{i}]",
            _ => throw new ArgumentOutOfRangeException(nameof(segments), segments, null)
        };
    }
}
