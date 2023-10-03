namespace Astrolabe.Annotation;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class SchemaOptionsAttribute : Attribute
{
    public Type EnumType { get; }

    public SchemaOptionsAttribute(Type enumType)
    {
        EnumType = enumType;
    }
}