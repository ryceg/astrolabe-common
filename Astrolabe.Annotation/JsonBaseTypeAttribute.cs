namespace Astrolabe.Annotation;

[AttributeUsage(AttributeTargets.Class)]
public class JsonBaseTypeAttribute : Attribute
{
    public Type DefaultType { get; }
    public string TypeField { get; }

    public JsonBaseTypeAttribute(string typeField, Type defaultType)
    {
        DefaultType = defaultType;
        TypeField = typeField;
    }

}