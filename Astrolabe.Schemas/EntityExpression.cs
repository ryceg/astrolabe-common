using System.Text.Json.Serialization;
using Astrolabe.Annotation;

namespace Astrolabe.Schemas;

[JsonString]
public enum ExpressionType
{
    Jsonata,
    FieldValue,
    UserMatch
}

[JsonBaseType("type", typeof(SimpleExpression))]
[JsonSubType("FieldValue", typeof(FieldValueExpression))]
[JsonSubType("Jsonata", typeof(JsonataExpression))]
[JsonSubType("UserMatch", typeof(UserMatchExpression))]
public abstract record EntityExpression(string Type)
{
    [JsonExtensionData]
    public IDictionary<string, object?>? Extensions { get; set; }
}

public record SimpleExpression(string Type) : EntityExpression(Type);

public record JsonataExpression(string Expression) : EntityExpression(ExpressionType.Jsonata.ToString());

public record FieldValueExpression([property: SchemaTag(SchemaTags.SchemaField)] string Field,  [property: SchemaTag("_ValuesOf:field")] object Value) : EntityExpression(ExpressionType.FieldValue.ToString());

public record UserMatchExpression(string UserMatch) : EntityExpression(ExpressionType.UserMatch.ToString());
