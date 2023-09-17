using System.Text.Json.Serialization;
using Astrolabe.Annotation;

namespace Astrolabe.Schemas;

[JsonBaseType("SchemaType", typeof(SimpleSchemaField))]
[JsonSubType(typeof(EntityRefField), "EntityRef")]
[JsonSubType(typeof(CompoundField), "Compound")]
public abstract record SchemaField(string Type, string Field)
{
    public string? DisplayName { get; set; }
    
    public bool? System { get; set; }
    
    public IEnumerable<string>? Tags { get; set; }
    
    public IEnumerable<string>? OnlyForTypes { get; set; }
    
    public bool? Required { get; set; }
    
    public bool? Collection { get; set; }
    
    public object? DefaultValue { get; set; }
    
    public bool? IsTypeField { get; set; }
    
    public bool? Searchable { get; set; }
    
    public IEnumerable<FieldOption>? Options { get; set; }
    
    public SchemaRestrictions? Restrictions { get; set; }
    
    [JsonExtensionData]
    public IDictionary<string, object?>? Extensions { get; set; }

    public FieldType GetFieldType()
    {
        return Enum.Parse<FieldType>(Type);
    }
}

public record SimpleSchemaField(string Type, string Field) : SchemaField(Type, Field);

public record EntityRefField(string Field, string EntityRefType, string? ParentField) : SchemaField(FieldType.EntityRef.ToString(), Field);

public record CompoundField(string Field, IEnumerable<SchemaField> Children, bool? TreeChildren) : SchemaField(FieldType.Compound.ToString(), Field);

[JsonString]
public enum FieldType
{
    String,
    Bool,
    Int,
    Date,
    DateTime,
    Double,
    EntityRef,
    Compound,
    AutoId,
    Image
}

public record SchemaRestrictions(IEnumerable<FieldOption>? Options = null);

public record FieldOption(string Name, object Value);

public static class SchemaTags
{
    public const string SchemaField = "_SchemaField";
    public const string NestedSchemaField = "_NestedSchemaField";
    public const string NoControl = "_NoControl";
    public const string ValuesOf = "_ValuesOf:";
    public const string TableList = "_TableList";
    public const string ThemeList = "_ThemeList";
    public const string DefaultValue = "_DefaultValue";
    public const string HtmlEditor = "_HtmlEditor";
}
