using Astrolabe.Common;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.EF.Search;

public record FieldValue(string Field, string Value)
{
    public static FieldValue FromString(string str)
    {
        return str.Split(':') switch
        {
            [var f, var v] => new FieldValue(f, EscapeUtils.UnescapeString(v, '\\', FieldValues.SemiColonEsc)),
            _ => throw new InvalidDataException($"Invalid field value: {str}")
        };
    }

    public override string ToString()
    {
        return FieldValues.FieldString(Field, Value);
    }
}

public static class FieldValues
{
    internal static readonly IDictionary<char, char> SemiColonEsc =
        (IDictionary<char, char>)new Dictionary<char, char>()
        {
            {
                ':',
                'c'
            }
        };

    public static string FieldString(string field, string value)
    {
        return $"{field}:{EscapeUtils.EscapeString(value, '\\', FieldValues.SemiColonEsc)}";
    }

    public static ListEditResults<T> EditFields<T>(ICollection<T>? existing, IEnumerable<FieldValue> edited,
        Func<T, FieldValue> getExisting, Func<FieldValue, T> create)
    {
        return ListEditor.EditList(existing, edited, (fv, e) => getExisting(e) == fv, (_, _) => false, create);
    }
    
    public static void EditFields<T>(DbSet<T> table, ICollection<T>? existing, IEnumerable<FieldValue> edited,
        Func<T, FieldValue> getExisting, Func<FieldValue, T> create) where T : class
    {
        var result = EditFields(existing, edited, getExisting, create);
        table.AddRange(result.Added);
        table.RemoveRange(result.Removed);
    }

}