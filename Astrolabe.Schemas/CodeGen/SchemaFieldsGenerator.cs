using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Astrolabe.Annotation;
using Astrolabe.CodeGen.Typescript;

namespace Astrolabe.Schemas.CodeGen;

public class SchemaFieldsGenerator : CodeGenerator<SimpleTypeData, TsDeclaration>
{
    private readonly SchemaFieldsGeneratorOptions _options;
    private readonly IEnumerable<TsType> _customFieldTypeParams;

    public SchemaFieldsGenerator(string clientPath) : this(new SchemaFieldsGeneratorOptions(clientPath))
    {
    }

    public SchemaFieldsGenerator(SchemaFieldsGeneratorOptions options)
    {
        _options = options;
        if (_options.CustomFieldTypes != null)
        {
            _customFieldTypeParams = new[]
                { new TsTypeSet(_options.CustomFieldTypes.Select(x => new TsStringConstantType(x))) };
        }
        else
        {
            _customFieldTypeParams = Array.Empty<TsType>();
        }
    }

    private static readonly TsRawExpr MakeScalarField =
        new("makeScalarField", new TsImport("@react-typed-forms/schemas", "makeScalarField"));

    private static readonly TsExpr MakeEntryExpr = new TsRawExpr("mkEntry", new TsImport(".", "mkEntry"));
    private static readonly TsImport FieldTypeImport = new("@react-typed-forms/schemas", "FieldType");


    private static readonly TsImport ApplyDefaultValuesImport =
        new TsImport("@react-typed-forms/schemas", "applyDefaultValues");

    private static readonly TsImport DefaultValueForFields =
        new TsImport("@react-typed-forms/schemas", "defaultValueForFields");

    private static readonly TsRawExpr MakeCompoundField = new TsRawExpr("makeCompoundField",
        new TsImport("@react-typed-forms/schemas", "makeCompoundField"));

    private static readonly TsRawExpr BuildSchemaFunc =
        new("buildSchema", new TsImport("@react-typed-forms/schemas", "buildSchema"));

    private static readonly HashSet<string> FormLibTypes = new()
    {
        "FieldType",
        "ControlDefinitionType",
        "DisplayDataType",
        "ExpressionType",
        "DynamicPropertyType",
        "ControlAdornmentType",
        "DataRenderType",
        "SyncTextType",
        "GroupRenderType",
        "DisplayDataType",
        "ControlDefinitionType",
        "ControlDefinition",
        "SchemaFieldType",
        "SchemaField",
        "IconMapping",
        "RenderOptions",
        "GroupRenderOptions",
        "DisplayData",
        "FieldOption",
        "EntityExpression",
        "DynamicProperty",
        "ControlAdornment",
        "SchemaRestrictions",
        "AdornmentPlacement"
    };

    public static string FormTypeName(Type type)
    {
        return type.Name + "Form";
    }

    public static string SchemaConstName(Type type)
    {
        return type.Name + "Schema";
    }

    private TsExpr BuildSchema(string schemaType)
    {
        return new TsTypeParamExpr(BuildSchemaFunc, new[] { new TsTypeRef(schemaType) }.Concat(_customFieldTypeParams));
    }

    private static string DefaultConstName(Type type)
    {
        return $"default{FormTypeName(type)}";
    }

    private TsAssignment CreateDefaultFormConst(Type type)
    {
        return new TsAssignment(DefaultConstName(type), new TsRawExpr(
                $"defaultValueForFields({SchemaConstName(type)})",
                new TsImports(new[] { DefaultValueForFields })),
            new TsTypeRef(FormTypeName(type)));
    }

    private TsRawFunction CreateConvertFunction(Type type)
    {
        return new TsRawFunction($"function to{FormTypeName(type)}(v: {type.Name}): {FormTypeName(type)} " + "{\n" +
                                 $"return applyDefaultValues(v, {SchemaConstName(type)});" + "\n}\n",
            new TsImports(new[] { ClientImport(type), ApplyDefaultValuesImport }));
    }


    private TsImport ClientImport(Type type)
    {
        return FormLibTypes.Contains(type.Name)
            ? new TsImport("@react-typed-forms/schemas", type.Name)
            : _options.ImportType(type);
    }

    protected override string TypeKey(SimpleTypeData typeData)
    {
        return typeData switch
        {
            EnumerableTypeData enumerableTypeData => enumerableTypeData.Element().Type.Name+"[]",
            ObjectTypeData objectTypeData => objectTypeData.Type.Name,
            _ => ""
        };
    }

    protected override IEnumerable<TsDeclaration> ToData(SimpleTypeData typeData)
    {
        return typeData switch
        {
            EnumerableTypeData enumerableTypeData => CollectData(enumerableTypeData.Element()),
            ObjectTypeData objectTypeData => ObjectDeclarations(objectTypeData),
            _ => Array.Empty<TsDeclaration>()
        };

        IEnumerable<TsDeclaration> ObjectDeclarations(ObjectTypeData objectTypeData)
        {
            var type = objectTypeData.Type;
            var members = objectTypeData.Members.Where(x =>
                x.Properties.First().GetCustomAttribute<JsonExtensionDataAttribute>() == null).ToList();

            var controlsInterface = new TsInterface(FormTypeName(type),
                new TsObjectType(members.Select(ControlsMember)));

            var deps = objectTypeData.Members.SelectMany(x => CollectData(x.Data()));

            var tsConstName = SchemaConstName(type);
            var tsAllName = FormTypeName(type);

            var baseType = type.GetCustomAttribute<JsonBaseTypeAttribute>();
            var subTypes = type.GetCustomAttributes<JsonSubTypeAttribute>();

            deps = deps.Concat(new TsDeclaration[]
            {
                controlsInterface,
                new TsAssignment(tsConstName,
                    new TsCallExpression(BuildSchema(tsAllName),
                        new List<TsExpr>
                        {
                            new TsObjectExpr(members.Select(x =>
                                FieldForMember(x, objectTypeData, baseType, subTypes)))
                        })),
                CreateDefaultFormConst(objectTypeData.Type),
            });
            if (_options.ShouldCreateConvert(objectTypeData.Type))
            {
                deps = deps.Append(CreateConvertFunction(objectTypeData.Type));
            }

            return deps;
        }

        TsFieldType ControlsMember(TypeMember<SimpleTypeData> member)
        {
            return new TsFieldType(member.FieldName, false, FormType(member.Data()));
        }
    }

    private TsObjectField FieldForMember(TypeMember<SimpleTypeData> member, ObjectTypeData parent,
        JsonBaseTypeAttribute? baseType, IEnumerable<JsonSubTypeAttribute> subTypes)
    {
        var onlyForTypes =
            member.Properties.Select(x => subTypes.FirstOrDefault(s => s.SubType == x.DeclaringType)?.Discriminator)
                .Where(x => x != null).ToList();
        var memberData = member.Data();
        var firstProp = member.Properties.First();
        var tags = firstProp.GetCustomAttributes<SchemaTagAttribute>().Select(x => x.Tag).ToList();
        var enumType = firstProp.GetCustomAttribute<SchemaOptionsAttribute>()?.EnumType ?? GetEnumType(memberData);
        var options = enumType != null ? EnumOptions(enumType, IsStringEnum(enumType)) : null;
        var buildFieldCall = SetOptions(FieldForType(memberData, parent), new Dictionary<string, object?>
        {
            { "isTypeField", baseType != null && baseType.TypeField == member.FieldName ? true : null },
            { "onlyForTypes", onlyForTypes.Count > 0 ? onlyForTypes : null },
            { "required", memberData.Nullable ? null : true },
            { "defaultValue", firstProp.GetCustomAttribute<DefaultValueAttribute>()?.Value },
            { "displayName", firstProp.GetCustomAttribute<DisplayAttribute>()?.Name ?? firstProp.Name },
            { "tags", tags.Count > 0 ? tags : null },
            { "options", options != null ? new TsConstExpr(options) : null }
        });
        return TsObjectField.NamedField(member.FieldName, buildFieldCall);
    }

    private Type? GetEnumType(SimpleTypeData data)
    {     
        return data switch
        {
            EnumerableTypeData enumerableTypeData => GetEnumType(enumerableTypeData.Element()),
            { Type.IsEnum: true } => data.Type,
            _ => null
        };
    }

    private TsType FormType(SimpleTypeData data)
    {
        return data switch
        {
            EnumerableTypeData enumerableTypeData => new TsArrayType(FormType(enumerableTypeData.Element()),
                Nullable: data.Nullable),
            _ => TsTypeOnly(data.Type) with { Nullable = data.Nullable }
        };
    }


    private TsType TsTypeOnly(Type type)
    {
        if (type.IsEnum)
        {
            return ClientImport(type).TypeRef;
        }

        if (type == typeof(string) || type == typeof(Guid) || type == typeof(DateTime) || type == typeof(DateOnly) ||
            type == typeof(DateTimeOffset))
            return new TsTypeRef("string");
        if (type == typeof(int) || type == typeof(double) || type == typeof(long))
            return new TsTypeRef("number");
        if (type == typeof(bool))
            return new TsTypeRef("boolean");
        if (type == typeof(object) ||
            (type.IsGenericType && typeof(IDictionary<,>).IsAssignableFrom(type.GetGenericTypeDefinition())))
            return new TsTypeRef("any");
        return new TsTypeRef(FormTypeName(type));
    }

    private static TsCallExpression SetOptions(TsCallExpression call, IDictionary<string, object?> options)
    {
        var argObject = (TsObjectExpr)call.Args.ToList()[0];
        var args = new TsExpr[]
        {
            options.Where(x => x.Value != null).Aggregate(argObject,
                (obj, v) => obj.SetField(new TsObjectField(new TsRawExpr(v.Key),
                    v.Value switch { TsExpr e => e, var value => new TsConstExpr(value) })))
        };
        return call with { Args = args };
    }

    private static FieldType FieldTypeForTypeOnly(Type type)
    {
        return type switch
        {
            { IsEnum: true } when IsStringEnum(type) is var stringEnum => stringEnum ? FieldType.String : FieldType.Int,
            _ when type == typeof(DateTime) => FieldType.DateTime,
            _ when type == typeof(DateTimeOffset) => FieldType.DateTime,
            _ when type == typeof(DateOnly) => FieldType.Date,
            _ when type == typeof(string) || type == typeof(Guid) => FieldType.String,
            _ when type == typeof(int) || type == typeof(long) => FieldType.Int,
            _ when type == typeof(double) => FieldType.Double,
            _ when type == typeof(bool) => FieldType.Bool,
            _ when type == typeof(object) => FieldType.Any,
            _ => FieldType.Any
        };
    }

    private TsCallExpression FieldForType(SimpleTypeData simpleType, ObjectTypeData parentObject)
    {
        var fieldType = _options.CustomFieldType?.Invoke(simpleType.Type);
        if (fieldType != null)
        {
            return MakeScalar(new TsConstExpr(fieldType));
        }

        return simpleType switch
        {
            EnumerableTypeData enumerableTypeData => SetOption(FieldForType(enumerableTypeData.Element(), parentObject),
                "collection", true),
            ObjectTypeData objectTypeData => DoObject(objectTypeData),
            _ => MakeScalar(new TsRawExpr("FieldType." + FieldTypeForTypeOnly(simpleType.Type), FieldTypeImport))
        };

        TsCallExpression DoObject(ObjectTypeData objectTypeData)
        {
            var fields = objectTypeData == parentObject
                ? new[]
                {
                    TsObjectField.NamedField("treeChildren",
                        new TsConstExpr(true))
                }
                : new[]
                {
                    TsObjectField.NamedField("children", new TsRawExpr(SchemaConstName(objectTypeData.Type)))
                };
            return TsCallExpression.Make(MakeCompoundField, TsObjectExpr.Make(fields));
        }
    }

    private TsCallExpression MakeScalar(TsExpr fieldTypeExpr)
    {
        return TsCallExpression.Make(
            MakeScalarField, TsObjectExpr.Make(
                TsObjectField.NamedField("type", fieldTypeExpr)
            )
        );
    }

    private static TsCallExpression SetOption(TsCallExpression call, string field, object value)
    {
        return SetOptions(call, new Dictionary<string, object?> { { field, value } });
    }

    private static bool IsStringEnum(Type type)
    {
        return type.GetCustomAttribute<JsonStringAttribute>() != null;
    }

    private static IEnumerable<FieldOption> EnumOptions(Type type, bool stringEnum)
    {
        return type.GetFields(BindingFlags.Static | BindingFlags.Public)
            .Select(x =>
                (Attribute.GetCustomAttribute(x, typeof(DisplayAttribute)),
                        Attribute.GetCustomAttribute(x, typeof(XmlEnumAttribute))) switch
                    {
                        (DisplayAttribute a, _) => new FieldOption(a.Name!, GetValue(x)),
                        (_, XmlEnumAttribute a) => new FieldOption(a.Name!, GetValue(x)),
                        _ => new FieldOption(x.Name, GetValue(x))
                    });

        object GetValue(FieldInfo info)
        {
            return stringEnum ? info.Name : (int)info.GetValue(null)!;
        }
    }

    public IEnumerable<TsDeclaration> CreateDeclarations(SimpleTypeData visitType)
    {
        return CollectData(visitType);
    }
}

public class SchemaFieldsGeneratorOptions
{
    public Func<Type, TsImport> ImportType { get; }

    public SchemaFieldsGeneratorOptions(Func<Type, TsImport> importType)
    {
        ImportType = importType;
    }

    public SchemaFieldsGeneratorOptions(string clientModule)
    {
        ImportType = x => new TsImport(clientModule, x.Name);
    }

    public Func<Type, bool>? CreateConvert { get; set; }
    public Func<Type, string?>? CustomFieldType { get; set; }

    public IEnumerable<string>? CustomFieldTypes { get; set; }

    public bool ShouldCreateConvert(Type type)
    {
        return CreateConvert?.Invoke(type) ?? true;
    }
}