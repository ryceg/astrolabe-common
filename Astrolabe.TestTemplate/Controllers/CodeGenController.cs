using Astrolabe.CodeGen;
using Astrolabe.CodeGen.Typescript;
using Astrolabe.Schemas;
using Astrolabe.Schemas.CodeGen;
using Microsoft.AspNetCore.Mvc;
using Namotion.Reflection;

namespace Astrolabe.TestTemplate.Controllers;

[ApiController]
[Route("[controller]")]
public class CodeGenController : ControllerBase
{
    [HttpGet("Schemas")]
    public string GetSchemas()
    {
        var gen = new SchemaFieldsGenerator(new SchemaFieldsGeneratorOptions("../client") {ForEditorLib = true});
        var declarations = gen.CollectDataForTypes(typeof(SchemaField), typeof(ControlDefinition));
        var file = TsFile.FromDeclarations(declarations.ToList());
        return file.ToSource();
    }
}