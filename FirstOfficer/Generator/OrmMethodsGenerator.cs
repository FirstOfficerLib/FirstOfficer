using System.Collections.Immutable;
using System.Text;
using FirstOfficer.Generator.Diagnostics;
using FirstOfficer.Generator.Helpers;
using FirstOfficer.Generator.Services;
using FirstOfficer.Generator.StateManagement;
using FirstOfficer.Generator.Syntax;
using FirstOfficer.Generator.Syntax.Templates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FirstOfficer.Generator
{
    [Generator]
    public class OrmMethodsGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {

#if DEBUG
          //  DebugGenerator.AttachDebugger();
#endif

            //diagnostics
            var diagnostic = context.CompilationProvider.SelectMany(
                static (compilation, _) => CompilationDiagnostics.BuildCompilationDiagnostics(compilation, DiagnosticCategories.Orm)
            );
            context.RegisterSourceOutput(diagnostic, static (context, diagnostic) => context.ReportDiagnostic(diagnostic));


            var entitiesProvider =
                context.SyntaxProvider.CreateSyntaxProvider(
                     IsClass,
                    (syntaxContext, _) => (ClassDeclarationSyntax)syntaxContext.Node)
                    .Where(a => a is not null);

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

                var classContent = new StringBuilder();
                //add save templates
                classContent.AppendLine(SaveGenerator.GenerateSaveMethods(entitiesDeclaration));

                //add query methods
                classContent.AppendLine(DatabaseQueryable.GetTemplate(entitiesDeclaration));

                classContent.AppendLine(EntityEnums.GetTemplate(entitiesDeclaration));

                classContent.AppendLine(DatabaseQuery.GetTemplate(entitiesDeclaration));

                classContent.AppendLine(EntityMapper.GetTemplate(entitiesDeclaration));

                classContent.AppendLine(DatabaseDelete.GetTemplate(entitiesDeclaration));

                //write the class

                var output = WrapClass($"{className}Entity", classContent.ToString());
                context.AddSource($"{className}Entity.g.cs", SyntaxHelper.FormatCode(output));

            }

            StateManager.SaveState(comp, context);
            Syntax.Templates.Helpers.GetTemplate(context);

        }

        private static string WrapClass(string className, string classContents)
        {

            return $@"

                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Data;
                using FirstOfficer.Data;
                using Npgsql;   
                using System.Text;
                using System.Data.Common;

                namespace FirstOfficer
                {{
                    public static class {className}
                    {{
                        {classContents}
                    }}
                }}

            ";
        }


    }
}