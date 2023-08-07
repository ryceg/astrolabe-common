namespace Astrolabe.Common.Annotation;

[System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = true)]
public class SchemaTagAttribute : System.Attribute
{
    public string Tag;

    public SchemaTagAttribute(string tag)
    {
        Tag = tag;
    }
}
