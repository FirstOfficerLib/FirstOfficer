using System.CodeDom;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using FirstOfficer.Generator.Attributes;
using FirstOfficer.Generator.Extensions;
using FirstOfficer.Generator.Helpers;
using FirstOfficer.Generator.StateManagement;
using FirstOfficer.Generator.Syntax;
using FirstOfficer.Generator.Syntax.Templates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CSharp;

namespace FirstOfficer.Generator
{
    [Generator]
    public class DataMethodsGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {

#if DEBUG
            //    DebugGenerator.AttachDebugger();
#endif

            //context.RegisterPostInitializationOutput(PostInitializationCallBack);

            var entitiesProvider = context.CompilationProvider.Select(SelectEntities<INamedTypeSymbol>);
            
            context.RegisterSourceOutput(entitiesProvider,
                static (spc, source) =>
                CreateSource(spc, source.Item2, source.Item1.ToList()));
        }


        private (IEnumerable<TResult>, Compilation) SelectEntities<TResult>(Compilation comp, CancellationToken cancellationToken) where TResult : INamedTypeSymbol
        {
            var rtn = new List<TResult>();

            var loadedEntityHashes = StateManager.LoadState(comp);
            var currentEntityHashes = StateManager.CurrentState(comp);

            foreach (var compSyntaxTree in comp.SyntaxTrees)
            {
                var root = compSyntaxTree.GetRoot();
                var sModel = comp.GetSemanticModel(compSyntaxTree);

                var classDeclarationSyntax = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();

                if (classDeclarationSyntax is null || classDeclarationSyntax.SyntaxTree.FilePath.EndsWith(".g.cs"))
                {
                    continue;
                }

                var classSymbol = sModel.GetDeclaredSymbol(classDeclarationSyntax);
                
                var key = $"IEntity-{classSymbol.FullName()}";
                if (classSymbol is { IsAbstract: false } && classSymbol.AllInterfaces.ToArray().Any(a => a.Name == "IEntity")
                    // &&  (!loadedEntityHashes.ContainsKey(key) || loadedEntityHashes[key] != currentEntityHashes[key])
                   )
                {
                    rtn.Add((TResult)classSymbol);
                }
            }
            return (rtn, comp);
        }

        private static void CreateSource(SourceProductionContext context, Compilation comp, List<INamedTypeSymbol> entitiesDeclarations)
        {
            

            foreach (var entitiesDeclaration in entitiesDeclarations)
            {
                var className = entitiesDeclaration.Name;
                var codeCompileUnit = GetCodeCompileUnit(context, comp, className);
                var codeTypeDeclaration = codeCompileUnit.Namespaces[0].Types[0];
                //add save templates
                codeTypeDeclaration.Members.Add(SaveGenerator.GenerateSaveMethods(entitiesDeclaration));

                codeTypeDeclaration.Members.Add(
                    new CodeSnippetTypeMember(EntityMapper.GetTemplate(entitiesDeclaration)));
                
                codeTypeDeclaration.Members.Add(
                    new CodeSnippetTypeMember(DatabaseQueryable.GetTemplate(entitiesDeclaration)));

                codeTypeDeclaration.Members.Add(
                    new CodeSnippetTypeMember(EntityEnums.GetTemplate(entitiesDeclaration)));

                //add query methods
                codeTypeDeclaration.Members.Add(
                    new CodeSnippetTypeMember(DatabaseQuery.GetTemplate(entitiesDeclaration)));

                codeTypeDeclaration.Members.Add(
                    new CodeSnippetTypeMember(DatabaseDelete.GetTemplate(entitiesDeclaration)));
                codeTypeDeclaration.Members.Add(
                    new CodeSnippetTypeMember(EntityChecksum.GetTemplate(entitiesDeclaration)));


                //write the class
                var provider = new CSharpCodeProvider();
                using var writer = new StringWriter();
                provider.GenerateCodeFromCompileUnit(codeCompileUnit, writer, new CodeGeneratorOptions()
                {
                    BracingStyle = "C",
                    IndentString = "    ", // Using 4 spaces for indentation
                    BlankLinesBetweenMembers = true
                });
                string output = writer.ToString()
                    .Replace($"class Entity{className}", $"static class Entity{className}"); //hack to make it static

                context.AddSource($"Entity{className}.g.cs", SyntaxHelper.FormatCode(output));

            }

            StateManager.SaveState(comp, context);
            Syntax.Templates.Helpers.GetTemplate(context);

        }

        private static CodeCompileUnit GetCodeCompileUnit(SourceProductionContext context, Compilation comp,
            string className)
        {
            var codeCompileUnit = new CodeCompileUnit();

            var codeNamespace = new CodeNamespace("FirstOfficer");
            codeNamespace.Imports.Add(new CodeNamespaceImport("System"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Collections"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Data"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("FirstOfficer.Data"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("Npgsql"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Text"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Data.Common"));

            codeCompileUnit.Namespaces.Add(codeNamespace);

            var codeTypeDeclaration = new CodeTypeDeclaration($"Entity{className}")
            {
                IsClass = true,
                IsPartial = false,
                TypeAttributes = TypeAttributes.Public
            };
            codeNamespace.Types.Add(codeTypeDeclaration);
            return codeCompileUnit;
        }


        private static void PostInitializationCallBack(IncrementalGeneratorPostInitializationContext context)
        {
            //empty;
        }
  
    }
}