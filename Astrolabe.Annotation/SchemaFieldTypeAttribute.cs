namespace Astrolabe.Annotation;

[AttributeUsage(AttributeTargets.Property|AttributeTargets.Class)]
public class SchemaFieldTypeAttribute : Attribute
{
    public string FieldType { get; }

    public SchemaFieldTypeAttribute(string fieldType)
    {
        FieldType = fieldType;
    }
}