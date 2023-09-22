using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Astrolabe.Annotation;
using Astrolabe.CodeGen.Typescript;

namespace Astrolabe.Schemas.CodeGen;

public class SchemaFieldsGenerator : CodeGenerator<SimpleTypeData>
{
    private readonly string _clientPath;


    public SchemaFieldsGenerator(string clientPath)
    {
        _clientPath = clientPath;
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
        "SchemaRestrictions"
    };

    private static string FormTypeName(Type type)
    {
        return type.Name + "Form";
    }

    private static string SchemaConstName(Type type)
    {
        return type.Name + "Schema";
    }

    private static TsRawExpr BuildSchema(string schemaType)
    {
        return new TsRawExpr("buildSchema<" + schemaType + ">",
            new TsImport("@react-typed-forms/schemas", "buildSchema"));
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
        return new TsImport(FormLibTypes.Contains(type.Name) ? "@react-typed-forms/schemas" : _clientPath, type.Name);
    }

    protected override IEnumerable<TsDeclaration> ToDeclarations(SimpleTypeData typeData)
    {
        return typeData switch
        {
            EnumerableTypeData enumerableTypeData => CreateDeclarations(enumerableTypeData.Element()),
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

            var deps = objectTypeData.Members.SelectMany(x => CreateDeclarations(x.Data()));

            var tsConstName = SchemaConstName(type);
            var tsAllName = FormTypeName(type);
            
            var baseType = type.GetCustomAttribute<JsonBaseTypeAttribute>();
            var subTypes = type.GetCustomAttributes<JsonSubTypeAttribute>();

            return deps.Concat(new TsDeclaration[]
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
                CreateConvertFunction(objectTypeData.Type)
            });
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
        var buildFieldCall = SetOptions(FieldForType(memberData, parent), new Dictionary<string, object?>
        {
            { "isTypeField", baseType != null && baseType.TypeField == member.FieldName ? true : null },
            { "onlyForTypes", onlyForTypes.Count > 0 ? onlyForTypes : null },
            { "required", memberData.Nullable ? null : true},
            { "defaultValue", firstProp.GetCustomAttribute<DefaultValueAttribute>()?.Value},
            { "displayName", firstProp.GetCustomAttribute<DisplayAttribute>()?.Name ?? firstProp.Name },
            { "tags", tags.Count > 0 ? tags : null}
        });
        return TsObjectField.NamedField(member.FieldName, buildFieldCall);
    }


    private TsType FormType(SimpleTypeData data)
    {
        return data switch
        {
            EnumerableTypeData enumerableTypeData => new TsArrayType(FormType(enumerableTypeData.Element()), Nullable: data.Nullable),
            _ => TsTypeOnly(data.Type) with { Nullable = data.Nullable }
        };
    }


    private TsType TsTypeOnly(Type type)
    {
        if (type.IsEnum)
        {
            return ClientImport(type).TypeRef;
        }

        if (type == typeof(string) || type == typeof(Guid) || type == typeof(DateTime))
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

    private TsCallExpression FieldForTypeOnly(Type type)
    {
        return type switch
        {
            { IsEnum: true } when IsStringEnum(type) is var stringEnum => ScalarWithOptions(
                stringEnum ? FieldType.String : FieldType.Int, EnumOptions(type, stringEnum)),
            _ when type == typeof(DateTime) => ScalarWithOptions(FieldType.DateTime, null),
            _ when type == typeof(DateTimeOffset) => ScalarWithOptions(FieldType.DateTime, null),
            _ when type == typeof(DateOnly) => ScalarWithOptions(FieldType.Date, null),
            _ when type == typeof(string) || type == typeof(Guid) => ScalarWithOptions(FieldType.String, null),
            _ when type == typeof(int) || type == typeof(long) => ScalarWithOptions(FieldType.Int, null),
            _ when type == typeof(double) => ScalarWithOptions(FieldType.Double, null),
            _ when type == typeof(bool) => ScalarWithOptions(FieldType.Bool, null),
            _ when type == typeof(object) => ScalarWithOptions(FieldType.Any, null),
            _ => ScalarWithOptions(FieldType.Any, null)
        };
    }
    
    private TsCallExpression FieldForType(SimpleTypeData simpleType, ObjectTypeData parentObject)
    {
        return simpleType switch
        {
            EnumerableTypeData enumerableTypeData => SetOption(FieldForType(enumerableTypeData.Element(), parentObject),
                "collection", true),
            ObjectTypeData objectTypeData => DoObject(objectTypeData),
            _ => FieldForTypeOnly(simpleType.Type)
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

    private TsCallExpression ScalarWithOptions(FieldType fieldType, IEnumerable<FieldOption>? options)
    {
        var makeCall = TsCallExpression.Make(
            MakeScalarField,
            TsObjectExpr.Make(
                TsObjectField.NamedField("type",
                    new TsRawExpr("FieldType." + fieldType, FieldTypeImport))
            )
        );
        return options != null ? SetOption(makeCall, "options", new TsConstExpr(options)) : makeCall;
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
}