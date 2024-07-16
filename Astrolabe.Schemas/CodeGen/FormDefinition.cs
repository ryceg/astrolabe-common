using Astrolabe.CodeGen.Typescript;

namespace Astrolabe.Schemas.CodeGen;

public interface FormDefinition<out TConfig>
{
    string Value { get; }

    string Name { get; }

    Type GetSchema();

    TConfig Config { get; }
}

public record FormDefinition<TSchema, TConfig>(string Value, string Name, TConfig Config)
    : FormDefinition<TConfig>
{
    public Type GetSchema()
    {
        return typeof(TSchema);
    }
}

public class FormBuilder<TConfig>
{
    public FormDefinition<TSchema, TConfig> Form<TSchema>(string value, string name, TConfig config)
    {
        return new FormDefinition<TSchema, TConfig>(value, name, config);
    }
}

public static class FormDefinition
{
    public static TsFile GenerateFormFile<T>(
        IEnumerable<FormDefinition<T>> definitions,
        string schemaModule,
        string formModuleDir
    )
    {
        var formVars = definitions.Select(MakeAssignment);
        return TsFile.FromDeclarations(formVars.Cast<TsDeclaration>().ToList());

        TsAssignment MakeAssignment(FormDefinition<T> x)
        {
            var jsonFile = new TsImport(
                formModuleDir + x.Value + ".json",
                x.Value + "Json",
                true
            ).Ref;

            return new TsAssignment(
                x.Value,
                new TsObjectExpr(
                    [
                        TsObjectField.NamedField("value", new TsConstExpr(x.Value)),
                        TsObjectField.NamedField("name", new TsConstExpr(x.Name)),
                        TsObjectField.NamedField(
                            "schema",
                            new TsImport(
                                schemaModule,
                                SchemaFieldsGenerator.SchemaConstName(x.GetSchema())
                            ).Ref
                        ),
                        TsObjectField.NamedField("defaultConfig", new TsConstExpr(x.Config)),
                        TsObjectField.NamedField(
                            "controls",
                            new TsPropertyExpr(jsonFile, new TsRawExpr("controls"))
                        ),
                        TsObjectField.NamedField(
                            "config",
                            new TsPropertyExpr(jsonFile, new TsRawExpr("config"))
                        ),
                    ]
                )
            );
        }
    }
}
