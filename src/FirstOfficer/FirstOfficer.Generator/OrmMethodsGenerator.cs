using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Linq;
using FirstOfficer.Generator.Diagnostics;
using FirstOfficer.Generator.Helpers;
using FirstOfficer.Generator.Services;
using FirstOfficer.Generator.StateManagement;
using FirstOfficer.Generator.Syntax;
using FirstOfficer.Generator.Syntax.Templates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CSharp;

namespace FirstOfficer.Generator
{
    [Generator]
    public class OrmMethodsGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {

#if DEBUG
            //   DebugGenerator.AttachDebugger();
#endif

            //diagnostics
            var diagnostic = context.CompilationProvider.SelectMany(
                static (compilation, _) => CompilationDiagnostics.BuildCompilationDiagnostics(compilation, DiagnosticCategories.Orm)
            );
            context.RegisterSourceOutput(diagnostic, static (context, diagnostic) => context.ReportDiagnostic(diagnostic));


            var entitiesProvider =
                context.SyntaxProvider.CreateSyntaxProvider(
                     IsClass,
                    (syntaxContext, _) => (ClassDeclarationSyntax)syntaxContext.Node )
                    .Where(a=> a is not null);
            
            var compilation = context.CompilationProvider.Combine(entitiesProvider.Collect());
            
            context.RegisterSourceOutput(compilation,
                static (spc, source) =>
                CreateSource(spc, source.Left, source.Right));
        }

        private bool IsClass(SyntaxNode node, CancellationToken token)
        {
            return node.IsKind(SyntaxKind.ClassDeclaration);
        }
        private static void CreateSource(SourceProductionContext context, Microsoft.CodeAnalysis.Compilation comp, ImmutableArray<ClassDeclarationSyntax> entitiesDeclarations)
        {
            foreach (var entityDeclarationSyntax in entitiesDeclarations)
            {
                var entitiesDeclaration = comp.GetSemanticModel(entityDeclarationSyntax.SyntaxTree).GetDeclaredSymbol(entityDeclarationSyntax);
                if (entitiesDeclaration == null || !OrmSymbolService.IsEntity(entitiesDeclaration))
                {
                    continue;
                }
                var className = entitiesDeclaration.Name;
                var codeCompileUnit = GetCodeCompileUnit(context, comp, className);
                var codeTypeDeclaration = codeCompileUnit.Namespaces[0].Types[0];
                //add save templates
                codeTypeDeclaration.Members.Add(SaveGenerator.GenerateSaveMethods(entitiesDeclaration));

                //add query methods
                codeTypeDeclaration.Members.Add(
                    new CodeSnippetTypeMember(DatabaseQueryable.GetTemplate(entitiesDeclaration)));

                codeTypeDeclaration.Members.Add(
                    new CodeSnippetTypeMember(EntityEnums.GetTemplate(entitiesDeclaration)));
                
                
                codeTypeDeclaration.Members.Add(
                    new CodeSnippetTypeMember(DatabaseQuery.GetTemplate(entitiesDeclaration)));

                codeTypeDeclaration.Members.Add(
                    new CodeSnippetTypeMember(EntityMapper.GetTemplate(entitiesDeclaration)));

                codeTypeDeclaration.Members.Add(
                    new CodeSnippetTypeMember(DatabaseDelete.GetTemplate(entitiesDeclaration)));

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
                    .Replace($"class {className}Entity", $"static class {className}Entity"); //hack to make it static

                context.AddSource($"{className}Entity.g.cs", SyntaxHelper.FormatCode(output));

            }

            StateManager.SaveState(comp, context);
            Syntax.Templates.Helpers.GetTemplate(context);

        }

        private static CodeCompileUnit GetCodeCompileUnit(SourceProductionContext context, Microsoft.CodeAnalysis.Compilation comp,
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

            var codeTypeDeclaration = new CodeTypeDeclaration($"{className}Entity")
            {
                IsClass = true,
                IsPartial = false,
                TypeAttributes = TypeAttributes.Public
            };
            codeNamespace.Types.Add(codeTypeDeclaration);
            return codeCompileUnit;
        }

  
    }
}