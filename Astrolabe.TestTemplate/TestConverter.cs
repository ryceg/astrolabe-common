using System.Text.Json;
using System.Text.Json.Serialization;
using Astrolabe.Common.Annotation;

namespace Astrolabe.TestTemplate;

public class TestConverter : JsonConverterFactory
{
    private readonly JsonStringEnumConverter _inner = new();

    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsDefined(typeof(JsonStringAttribute), false);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return _inner.CreateConverter(typeToConvert, options);
    }
}