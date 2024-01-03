using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Astrolabe.Schemas.CodeGen;

public class GenCSharp
{
    public static ClassDeclarationSyntax GenMembers(string className, IEnumerable<SchemaField> fields)
    {
        return ClassDeclaration(className)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .WithMembers(new SyntaxList<MemberDeclarationSyntax>(
                fields.Select(x => PropertyDeclaration(
                        ParseTypeName("string"), x.Field)
                    .AddAccessorListAccessors(
                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                        AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))))));
    }
}