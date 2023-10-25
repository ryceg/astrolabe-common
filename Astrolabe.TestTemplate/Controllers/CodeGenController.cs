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
        var gen = new SchemaFieldsGenerator("../client");
        var visitor = new SimpleTypeVisitor();
        var schemaFieldType = visitor.VisitType(typeof(SchemaField).ToContextualType());
        var controls = visitor.VisitType(typeof(ControlDefinition).ToContextualType());
        var declarations = gen.CreateDeclarations(schemaFieldType).Concat(gen.CreateDeclarations(controls));
        var file = TsFile.FromDeclarations(declarations.ToList());
        return file.ToSource();
    }
}