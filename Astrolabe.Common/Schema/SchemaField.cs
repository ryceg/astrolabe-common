using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Astrolabe.Common.Annotation;

namespace Astrolabe.Common.Schema;

[JsonString]
public enum SchemaFieldType
{
    Scalar,
    Compound
}

[JsonBaseType("SchemaType")]
public abstract record SchemaField([property: DefaultValue("Scalar")] SchemaFieldType SchemaType, string Field, string DisplayName, [property: DefaultValue("String")]  FieldType Type,
    IEnumerable<string> Tags,
    bool System, bool Collection, IEnumerable<string> OnlyForTypes, bool Required);

public record ScalarField(string Field, string DisplayName, FieldType Type, IEnumerable<string> Tags,
        string EntityRefType, bool System, bool Required, bool Collection, string ParentField, bool Searchable,
        object DefaultValue, bool IsTypeField, IEnumerable<string> OnlyForTypes, SchemaRestrictions? Restrictions)
    : SchemaField(SchemaFieldType.Scalar, Field, DisplayName, Type, Tags, System, Collection, OnlyForTypes, Required);

public record CompoundField(string Field, string DisplayName, FieldType Type, IEnumerable<string> Tags, bool Collection,
        IEnumerable<SchemaField> Children, bool TreeChildren, IEnumerable<string> OnlyForTypes, bool Required)
    : SchemaField(SchemaFieldType.Compound, Field, DisplayName, Type, Tags, false, Collection, OnlyForTypes, Required);

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

public record EntitySchema(string ScopedId, IEnumerable<SchemaField> Fields, string Parent, IEnumerable<string> Tags);

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
