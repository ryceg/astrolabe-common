using System.Reflection;
using System.Text.Json;
using Astrolabe.Annotation;
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
        var fields = (new FieldBuilder { Serializer = serializer }.Build(typeof(T))).Concat(GetEnumFields(typeof(T), serializer)).ToList();
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

    public static string? ElementValue(JsonElement element, SearchFieldDataType type)
    {
        return type.ToString() switch
        {
            "Edm.String" => element.GetString(),
            "Edm.Int32" => element.GetInt32().ToString(),
            "Edm.Int64" => element.GetInt64().ToString(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static IEnumerable<SearchField> GetEnumFields(Type type, JsonObjectSerializer serializer)
    {
        var convert = (IMemberNameConverter)serializer;
        return type.GetProperties(BindingFlags.Public|BindingFlags.Instance).SelectMany(p =>
        {
            var name = convert.ConvertMemberName(p);
            var enumType = GetEnumType(p.PropertyType);
            return enumType != null ? [CreateSimpleField()] : Array.Empty<SearchField>();
            
            SearchField CreateSimpleField()
            {
                var isStringEnum = enumType.GetCustomAttribute<JsonStringAttribute>() != null;
                SearchField field = new SimpleField(name, isStringEnum ? SearchFieldDataType.String : SearchFieldDataType.Int32);
                foreach (var attribute in p.GetCustomAttributes())
                {
                    switch (attribute)
                    {
                        case SearchableFieldAttribute sf:
                            field.IsSearchable = true;
                            ProcessSimpleField(sf);
                            break;
                        case SimpleFieldAttribute simpleField:
                            ProcessSimpleField(simpleField);
                            break;
                    }
                }
                return field;

                void ProcessSimpleField(SimpleFieldAttribute sf)
                {
                    if (sf.IsFilterable)
                        field.IsFilterable = sf.IsFilterable;
                }
            }
        });

        Type? GetEnumType(Type t)
        {
            if (t.IsEnum)
                return t;
            if (t.IsConstructedGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                return GetEnumType(t.GenericTypeArguments[0]);
            return null;
        }
    }
    
}