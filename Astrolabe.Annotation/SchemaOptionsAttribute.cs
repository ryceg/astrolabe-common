namespace Astrolabe.Annotation;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class SchemaOptionsAttribute : Attribute
{
    public Type? EnumType { get; }
    public string? FieldType { get; }
    public string? RequiredText { get; }
    public string? SingularName { get; }

    public SchemaOptionsAttribute(Type? enumType = null, string? fieldType = null, string? requiredText = null,
        string? singularName = null)
    {
        EnumType = enumType;
        FieldType = fieldType;
        RequiredText = requiredText;
        SingularName = singularName;
    }
}