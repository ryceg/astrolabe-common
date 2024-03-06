using System.Text.Json;
using Azure.Core.Serialization;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace Astrolabe.EF.Search;

public delegate SimpleSearchDocument SimpleSearchIndexer<in T>(T document);

public static class SimpleSearchIndexer
{
    public static SimpleSearchIndexer<T> Create<T>(JsonSerializerOptions? options = null, Func<SearchField, bool>? textFields = null, Func<SearchField, bool>? filterFields = null)
    {
        var serializer = options != null ? new JsonObjectSerializer(options) : new JsonObjectSerializer();
        var fields = new FieldBuilder { Serializer = serializer }.Build(typeof(T));
        var textFieldList = fields.Where(textFields ?? (x => x.IsSearchable ?? false)).ToArray();
        var filterFieldList = fields.Where(filterFields ?? (x => x.IsFilterable ?? false)).ToArray();
        return doc =>
        {
            var jsonDoc = JsonSerializer.SerializeToElement(doc, options);
            var searchStrings = textFieldList.SelectMany(x =>
            {
                return jsonDoc.TryGetProperty(x.Name, out var value)
                    ? new [] { (ElementValue(value, x.Type)?.ToString() ?? "").Trim() }
                    : Enumerable.Empty<string>();
            }).Where(x => !string.IsNullOrEmpty(x));
            return new SimpleSearchDocument(string.Join(" ", searchStrings), filterFieldList
                .SelectMany(x =>
                {
                    return jsonDoc.TryGetProperty(x.Name, out var value)
                        ? new FieldValue[] { new(x.Name, ElementValue(value, x.Type)) }
                        : Enumerable.Empty<FieldValue>();
                }));
        };
    }

    public static object? ElementValue(JsonElement element, SearchFieldDataType type)
    {
        return type.ToString() switch
        {
            "Edm.String" => element.GetString(),
            "Edm.Int32" => element.GetInt32(),
            "Edm.Int64" => element.GetInt64(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}