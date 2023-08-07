using System.Text.Json;
using System.Text.Json.Serialization;
using Astrolabe.Common.Annotation;

namespace Astrolabe.TestTemplate;

public class SubTypeConverter : JsonConverter<object>
{
    public SubTypeConverter()
    {
    }
    
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsDefined(typeof(JsonBaseTypeAttribute), false);
    }

    private JsonSerializerOptions? _recursive;
    
    private JsonSerializerOptions? _original;

    private JsonSerializerOptions GetRecursive(JsonSerializerOptions options)
    {
        if (_original != null && options != _original)
            throw new NotSupportedException("Must be an instance per options");
        if (_recursive == null)
        {
            _original = options;
            var newOptions = new JsonSerializerOptions(options);
            newOptions.Converters.Remove(this);
            _recursive = newOptions;
        }
        return _recursive;
    }
    
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        var discriminatorAttr = typeToConvert.GetCustomAttributes(typeof(JsonBaseTypeAttribute), false);
        var discriminator = ((JsonBaseTypeAttribute)discriminatorAttr[0]).PropertyName;
        using var jsonDocument = JsonDocument.ParseValue(ref reader);
        if (!jsonDocument.RootElement.TryGetProperty(discriminator, out var typeProperty))
        {
            throw new JsonException();
        }

        var typeString = typeProperty.GetString();
        var attributes = typeToConvert.GetCustomAttributes(typeof(SwaggerSubTypeAttribute), false);
        var attribute = attributes.Cast<SwaggerSubTypeAttribute>().FirstOrDefault(x => x.DiscriminatorValue == typeString);
        return jsonDocument.Deserialize(attribute != null ? attribute.SubType : typeToConvert, GetRecursive(options))!;
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, GetRecursive(options));
    }

}