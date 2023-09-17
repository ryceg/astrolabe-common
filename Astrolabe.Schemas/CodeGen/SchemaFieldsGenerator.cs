using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
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
        return new TsImport($"{_clientPath}", type.Name);
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

            var controlsInterface = new TsInterface(FormTypeName(type),
                new TsObjectType(objectTypeData.Members.Select(ControlsMember)));

            var deps = objectTypeData.Members.SelectMany(x => CreateDeclarations(x.Data()));

            var tsConstName = SchemaConstName(type);
            var tsAllName = FormTypeName(type);

            // allFields.Select(x => TsToSource.NamedField(x.fieldName, x.withScalarOptions))
            //     .ToList();


            // var withOptions = SetOptions(memberField, new Dictionary<string, object>
            // {
            //     { "displayName", firstProp.Info.Name },
            //     { "required", firstCType.Nullability == Nullability.NotNullable ? true : null },
            //     { "onlyForTypes", onlyForTypes.Any() ? onlyForTypes : null }, { "tags", tags.Any() ? tags : null }
            // });
            //
            // var withScalarOptions = withOptions.Function switch
            // {
            //     TsRawExpr { Source: "makeScalarField" } => SetOptions(withOptions,
            //         new Dictionary<string, object>
            //         {
            //             { "isTypeField", fieldName == discriminator ? true : null },
            //             { "defaultValue", defaultValue?.Value },
            //             { "required", defaultValue != null ? true : null }
            //         }),
            //     _ => withOptions
            // };

            return deps.Concat(new TsDeclaration[]
            {
                controlsInterface,
                new TsAssignment(tsConstName,
                    new TsCallExpression(BuildSchema(tsAllName),
                        new List<TsExpr> { new TsObjectExpr(objectTypeData.Members.Select(FieldForMember)) })),
                CreateDefaultFormConst(objectTypeData.Type),
                CreateConvertFunction(objectTypeData.Type)

            });
        }

        TsFieldType ControlsMember(TypeMember<SimpleTypeData> member)
        {
            return new TsFieldType(member.FieldName, false, FormType(member.Data()));
        }
    }

    private TsType FormType(SimpleTypeData data)
    {
        return data switch
        {
            EnumerableTypeData enumerableTypeData => new TsArrayType(FormType(enumerableTypeData.Element())),
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

        return new TsTypeRef(FormTypeName(type));
    }
    
    private TsCallExpression SetOptions(TsCallExpression call, IDictionary<string, object> options)
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
            _ when type == typeof(object) => ScalarWithOptions(FieldType.String, null),
            _ => ScalarWithOptions(FieldType.String, null)
        };
    }

    private TsObjectField FieldForMember(TypeMember<SimpleTypeData> member)
    {
        return TsObjectField.NamedField(member.FieldName, FieldForType(member.Data()));
    }

    private TsCallExpression FieldForType(SimpleTypeData simpleType)
    {
        return simpleType switch
        {
            EnumerableTypeData enumerableTypeData => SetOption(FieldForType(enumerableTypeData.Element()), "collection",
                true),
            ObjectTypeData objectTypeData => TsCallExpression.Make(
                MakeCompoundField, TsObjectExpr.Make(
                    TsObjectField.NamedField("children", new TsRawExpr(SchemaConstName(objectTypeData.Type))))),
            _ => FieldForTypeOnly(simpleType.Type)
        };
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
        if (options != null)
        {
            return SetOption(makeCall, "restrictions",
                new TsObjectExpr(new[] { TsObjectField.NamedField("options", new TsConstExpr(options)) }));
        }

        return makeCall;
    }

    private TsCallExpression SetOption(TsCallExpression call, string field, object value)
    {
        return SetOptions(call, new Dictionary<string, object> { { field, value } });
    }

    private static bool IsStringEnum(Type type)
    {
        var converterAttr = ((JsonConverterAttribute?)Attribute.GetCustomAttribute(type,
            typeof(JsonConverterAttribute)));
        return (converterAttr?.ConverterType == typeof(JsonStringEnumConverter));
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
