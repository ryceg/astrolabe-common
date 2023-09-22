namespace Astrolabe.Annotation;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class SchemaTagAttribute : Attribute
{
    public string Tag { get; }

    public SchemaTagAttribute(string tag)
    {
        Tag = tag;
    }
}
