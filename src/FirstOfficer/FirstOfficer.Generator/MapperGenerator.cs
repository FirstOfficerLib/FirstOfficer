using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using FirstOfficer.Generator.Compilation;
using FirstOfficer.Generator.Diagnostics;
using FirstOfficer.Generator.Extensions;
using FirstOfficer.Generator.Services;
using FirstOfficer.Generator.Syntax.Templates;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FirstOfficer.Generator
{
    [Generator]
    public class MapperGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
#if DEBUG
           //    DebugGenerator.AttachDebugger();
#endif

            //diagnostics
            var diagnostic = context.CompilationProvider.SelectMany(
                static (compilation, _) => CompilationDiagnostics.BuildCompilationDiagnostics(compilation, DiagnosticCategories.Mapper)
            );
            context.RegisterSourceOutput(diagnostic, static (context, diagnostic) => context.ReportDiagnostic(diagnostic));

            var compilationContext = context.CompilationProvider
                .Select(static (c, _) => new CompilationContext(c, new FileNameBuilder()))
                .WithTrackingName("BuildCompilationContext");

            context.RegisterSourceOutput(context.CompilationProvider,
                static (spc, source) =>
                    CreateSource(spc, source));
        }

        private static void CreateSource(SourceProductionContext sourceProductionContext, Microsoft.CodeAnalysis.Compilation compilation)
        {
            var mappings = MapperSymbolService.GetEntityDtoMappings(compilation);

            foreach (var mapping in mappings)
            {
                 var source = DtoMapper.GetTemplate(mapping);
                 sourceProductionContext.AddSource(mapping.DtoSymbol.Name + "Mapper.g.cs", source);
            }
            
        }
    }

 
}
