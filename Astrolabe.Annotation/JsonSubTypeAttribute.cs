namespace Astrolabe.Annotation;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class JsonSubTypeAttribute : Attribute
{
    public Type SubType { get; }
    
    public string Discriminator { get; }

    public JsonSubTypeAttribute(string discriminator, Type subType)
    {
        SubType = subType;
        Discriminator = discriminator;
    }
}