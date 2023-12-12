using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Text;
using System.Security.Cryptography;
using FirstOfficer.Generator.Extensions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace FirstOfficer.Generator.StateManagement
{
    internal static class StateManager
    {
        private static readonly SHA256 Sha256 = SHA256.Create();

        internal static string ComputeHashForSyntax(ClassDeclarationSyntax classDeclarationSyntax)
        {
            string content = classDeclarationSyntax.ToFullString();
            byte[] hashBytes = Sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
            return BitConverter.ToString(hashBytes).Replace("-", "");
        }

        internal static Dictionary<string, string> LoadState(Microsoft.CodeAnalysis.Compilation comp)
        {

            var rtn = new Dictionary<string, string>();

            foreach (var compSyntaxTree in comp.SyntaxTrees)
            {
                var root = compSyntaxTree.GetRoot();
                var sModel = comp.GetSemanticModel(compSyntaxTree);

                var classDeclarationSyntax = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();

                if (classDeclarationSyntax is null)
                {
                    continue;
                }

                if (ModelExtensions.GetDeclaredSymbol(sModel, classDeclarationSyntax) is INamedTypeSymbol { IsAbstract: true } classSymbol && 
                    classSymbol.AllInterfaces.ToArray().Any(a => a.Name.EndsWith("IStateStorage")))
                {
                    foreach (var attr in classSymbol.GetAttributes().Where(a=> a.AttributeClass?.Name == "StorageItemAttribute"))
                    {
                        var name = attr.ConstructorArguments[0].Value?.ToString();
                        var checksum = attr.ConstructorArguments[1].Value?.ToString();
                        if (rtn.ContainsKey(name!))
                        {
                            rtn.Remove(name!);
                        }
                        rtn.Add(name!, checksum!);
                    }
                }

            }
            return rtn;
        }

        internal static Dictionary<string, string> CurrentState(Microsoft.CodeAnalysis.Compilation comp)
        {
   
            var rtn = new Dictionary<string, string>();
            foreach (var compSyntaxTree in comp.SyntaxTrees)
            {
                var root = compSyntaxTree.GetRoot();
                var sModel = comp.GetSemanticModel(compSyntaxTree);

                var classDeclarationSyntax = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();

                if (classDeclarationSyntax is null)
                {
                    continue;
                }

                if (ModelExtensions.GetDeclaredSymbol(sModel, classDeclarationSyntax) is INamedTypeSymbol { IsAbstract: false } classSymbol &&
                    classSymbol.AllInterfaces.ToArray().Any(a => a.Name == "IEntity"))
                {
                    rtn.Add($"IEntity-{classSymbol.FullName()}", ComputeHashForSyntax(classDeclarationSyntax));
                }

            }

            return rtn;
        }

        internal static void SaveState(Microsoft.CodeAnalysis.Compilation comp, SourceProductionContext context)
        {
            var checksums = CurrentState(comp);

            var attributeList = new List<AttributeSyntax>();

            foreach (var checksum in checksums)
            {
                var attributeArgument1 = SyntaxFactory.AttributeArgument(
                    SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(checksum.Key)));
                var attributeArgument2 = SyntaxFactory.AttributeArgument(
                    SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(checksum.Value)));
                var attributeArguments = SyntaxFactory.SeparatedList(new[] { attributeArgument1, attributeArgument2 });

                attributeList.Add(SyntaxFactory.Attribute(SyntaxFactory.ParseName(typeof(StorageItemAttribute).FullName!), SyntaxFactory.AttributeArgumentList(attributeArguments)));

            }

            var attributeListItems = SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(attributeList.ToArray()));

            var classBlock = SyntaxFactory.ClassDeclaration("StateStorage")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.InternalKeyword),
                    SyntaxFactory.Token(SyntaxKind.AbstractKeyword))
                .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(typeof(IStateStorage).FullName!)))
                .AddAttributeLists(attributeListItems).NormalizeWhitespace();

            context.AddSource("StateStorage.g",
                SourceText.From(classBlock.ToFullString(), Encoding.UTF8));
        }
    }
}
