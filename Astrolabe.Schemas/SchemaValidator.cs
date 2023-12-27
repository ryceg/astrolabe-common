using System.Text.Json.Serialization;
using Astrolabe.Annotation;

namespace Astrolabe.Schemas;

[JsonString]
public enum ValidatorType
{
    Jsonata
}

[JsonBaseType("type", typeof(SimpleExpression))]
[JsonSubType("Jsonata", typeof(JsonataValidator))]
public abstract record SchemaValidator([property: SchemaOptions(typeof(ValidatorType))] string Type)
{
    [JsonExtensionData]
    public IDictionary<string, object?>? Extensions { get; set; }
}

public record SimpleValidator(string Type) : SchemaValidator(Type);

public record JsonataValidator(string Expression) : SchemaValidator(ValidatorType.Jsonata.ToString());
