namespace Astrolabe.Annotation;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class JsonSubTypeAttribute : Attribute
{
    public Type SubType { get; }
    
    public string Discriminator { get; }

    public JsonSubTypeAttribute(Type subType, string discriminator)
    {
        SubType = subType;
        Discriminator = discriminator;
    }
}