using Astrolabe.Common;

namespace Astrolabe.EF.Search;

public record FieldValue(string Field, object? Value)
{
    public static FieldValue FromString(string str)
    {
        return str.Split(':') switch
        {
            [var f, var v] => new FieldValue(f, EscapeUtils.UnescapeString(v, '\\', FieldValueExtensions.SemiColonEsc)),
            _ => throw new InvalidDataException($"Invalid field value: {str}")
        };
    }

    public override string ToString()
    {
        return $"{Field}:{EscapeUtils.EscapeString(Value?.ToString() ?? "", '\\', FieldValueExtensions.SemiColonEsc)}";
    }
}

public static class FieldValueExtensions
{
    internal static readonly IDictionary<char, char> SemiColonEsc =
        (IDictionary<char, char>)new Dictionary<char, char>()
        {
            {
                ':',
                'c'
            }
        };

    public static ListEditResults<T> EditFields<T>(ICollection<T>? existing, IEnumerable<FieldValue> edited,
        Func<T, FieldValue> getExisting, Func<FieldValue, T> create)
    {
        return ListEditor.EditList(existing, edited, (fv, e) => getExisting(e) == fv, (_, _) => false, create);
    }
}