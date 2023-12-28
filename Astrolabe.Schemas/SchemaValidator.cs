using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Astrolabe.Annotation;

namespace Astrolabe.Schemas;

[JsonString]
public enum ValidatorType
{
    Jsonata,
    Date
}

[JsonBaseType("type", typeof(SimpleExpression))]
[JsonSubType("Jsonata", typeof(JsonataValidator))]
[JsonSubType("Date", typeof(DateValidator))]
public abstract record SchemaValidator([property: SchemaOptions(typeof(ValidatorType))] string Type)
{
    [JsonExtensionData]
    public IDictionary<string, object?>? Extensions { get; set; }
}

public record SimpleValidator(string Type) : SchemaValidator(Type);

public record JsonataValidator(string Expression) : SchemaValidator(ValidatorType.Jsonata.ToString());

[JsonString]

public enum DateComparison
{
    [Display(Name = "Not Before")]
    NotBefore,
    [Display(Name = "Not After")]
    NotAfter
}

public record DateValidator(DateComparison Comparison, DateOnly? FixedDate, int? DaysFromCurrent)
    : SchemaValidator(ValidatorType.Date.ToString());
