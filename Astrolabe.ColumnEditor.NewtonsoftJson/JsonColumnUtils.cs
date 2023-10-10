using Newtonsoft.Json.Linq;

namespace Astrolabe.ColumnEditor.NewtonsoftJson;

public static class JsonColumnUtils
{
    public static JObject ParseJson(string? strValue)
    {
        return string.IsNullOrEmpty(strValue) ? new JObject() : JObject.Parse(strValue);
    }
}