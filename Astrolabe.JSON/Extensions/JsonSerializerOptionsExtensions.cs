using System.Text.Json;
using System.Text.Json.Serialization;

namespace Astrolabe.JSON.Extensions;

public static class JsonSerializerOptionsExtensions
{
    public static JsonSerializerOptions AddStandardOptions(this JsonSerializerOptions options)
    {
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.Converters.Add(new JsonBaseTypeConverter());
        options.Converters.Add(new StringAttributeConverter());
        return options;
    }
}