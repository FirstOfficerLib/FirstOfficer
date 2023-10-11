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
    public class QueryMethodsGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {

#if DEBUG
             //   DebugGenerator.AttachDebugger();
#endif

            //context.RegisterPostInitializationOutput(PostInitializationCallBack);

            var entitiesProvider = context.CompilationProvider.Select(MethodNames);
            
            context.RegisterImplementationSourceOutput(entitiesProvider,
                static (spc, source) =>
                CreateSource(spc, source.Item2, source.Item1.ToList()));
        }


        private (IEnumerable<string>, Compilation) MethodNames(Compilation comp, CancellationToken cancellationToken)
        {
          
            var symbols = new List<INamedTypeSymbol>();
            
            
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
                    symbols.Add(classSymbol);
                }
            }

            var methodNames = symbols.Select(a =>$"Query{a.Name}").ToList();

            foreach (var methodName in methodNames)
            {
                foreach (var compSyntaxTree in comp.SyntaxTrees)
                {
                    var root = compSyntaxTree.GetRoot();
                    var sModel = comp.GetSemanticModel(compSyntaxTree);
                    var methodSymbol = root.DescendantNodes().OfType<IdentifierNameSyntax>()
                        .Where(b => b.Identifier.ValueText.Contains(methodName));

                    foreach (var syntax in methodSymbol)
                    {
                        if (syntax?.Parent?.Parent is not InvocationExpressionSyntax expressionSyntax)
                        {
                            continue;
                        }

                        var args =
                            expressionSyntax
                            .ArgumentList.Arguments;
                    }
                }
            }

            return (methodNames, comp);
        }

        private static void CreateSource(SourceProductionContext context, Compilation comp, List<string> methodNames)
        {
            StateManager.SaveState(comp, context);
            Syntax.Templates.Helpers.GetTemplate(context);

        }
    }
}