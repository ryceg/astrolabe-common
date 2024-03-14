using System.Collections.Immutable;
using System.Text.Json.Nodes;

namespace Astrolabe.JSON;

public record JsonPathSegments(ImmutableStack<object> Segments)
{
    public static readonly JsonPathSegments Empty = new(ImmutableStack.Create<object>());
    public JsonPathSegments Field(string field) => new(Segments.Push(field));
    public JsonPathSegments Index(int index) => new(Segments.Push(index));

    public JsonNode? Traverse(JsonNode? node)
    {
        var allSegments = Segments.ToArray();
        foreach (var segment in allSegments)
        {
            if (node == null)
                return null;
            node = segment switch
            {
                int i => node[i],
                string s => node[s],
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        return node;
    }
}


public static class JsonPathSegmentsExtensions
{
    public static string ToPathString(this JsonPathSegments segments)
    {
        return string.Join("", segments.Segments.Reverse().Select((x, i) => x switch
        {
            string s when i > 0 => "." + s,
            int v => $"[{v}]",
            _ => x.ToString()
        }));
    }
}