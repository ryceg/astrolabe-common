using System;

namespace Astrolabe.Common.Annotation;

[AttributeUsage(AttributeTargets.Class)]
public class JsonBaseTypeAttribute : Attribute
{
    public string TypeField;

    public JsonBaseTypeAttribute(string typeField)
    {
        TypeField = typeField;
    }

}