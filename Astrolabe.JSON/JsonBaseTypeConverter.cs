using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Astrolabe.Annotation;

namespace Astrolabe.JSON;

public class JsonBaseTypeConverter : JsonConverter<object>
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsDefined(typeof(JsonBaseTypeAttribute), false);
    }
    
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        var baseTypeAttribute = typeToConvert.GetCustomAttribute<JsonBaseTypeAttribute>()!;
        var discriminator = baseTypeAttribute.TypeField;
        using var jsonDocument = JsonDocument.ParseValue(ref reader);
        if (!jsonDocument.RootElement.TryGetProperty(discriminator, out var typeProperty))
        {
            throw new JsonException();
        }

        var typeString = typeProperty.GetString();
        var attributes = typeToConvert.GetCustomAttributes(typeof(JsonSubTypeAttribute), false);
        var realType = attributes.Cast<JsonSubTypeAttribute>().FirstOrDefault(x => typeString!.Equals(x.Discriminator))?.SubType ??
                       baseTypeAttribute.DefaultType;

        return jsonDocument.Deserialize(realType, options)!;
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }

}