namespace Astrolabe.Schemas;

public static class SchemaFieldExtensions
{
    public static bool IsScalarField(this SchemaField field)
    {
        return !(field.Collection is true || field is CompoundField);
    }
}