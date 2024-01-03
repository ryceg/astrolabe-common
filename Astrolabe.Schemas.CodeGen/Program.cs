using Astrolabe.Schemas;
using Astrolabe.Schemas.CodeGen;
using Microsoft.CodeAnalysis;

GenCSharp.GenMembers("Test",
        new[]
        {
            new SimpleSchemaField(FieldType.String.ToString(), "cool"),
            new SimpleSchemaField(FieldType.Int.ToString(), "wow")
        }).NormalizeWhitespace()
    .WriteTo(Console.Out);