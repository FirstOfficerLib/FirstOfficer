using System.CodeDom;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Globalization;
using System.Net.Mime;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using FirstOfficer.Generator.AiServices;
using FirstOfficer.Generator.Attributes;
using FirstOfficer.Generator.Extensions;
using FirstOfficer.Generator.Helpers;
using FirstOfficer.Generator.Services;
using FirstOfficer.Generator.StateManagement;
using FirstOfficer.Generator.Syntax;
using FirstOfficer.Generator.Syntax.Templates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CSharp;
using Pluralize.NET;
using ArgumentSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ArgumentSyntax;
using LiteralExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.LiteralExpressionSyntax;

namespace FirstOfficer.Generator
{
    [Generator]
    public class QueryMethodsGenerator : ISourceGenerator
    {
        private static void CreateSource(SourceProductionContext context, Compilation comp, List<string> methodNames)
        {
            // StateManager.SaveState(comp, context);
            //  Syntax.Templates.Helpers.GetTemplate(context);
        }

        public void Initialize(GeneratorInitializationContext context)
        {
#if DEBUG
            //   DebugGenerator.AttachDebugger();
#endif

        }

        public void Execute(GeneratorExecutionContext context)
        {
            var symbols = new List<INamedTypeSymbol>();

            //load appsettings.Development.json
            var configuration = new AppSettingsService().GetAppSettings(context);
            var openAiService = new OpenAiService(configuration);
            var comp = context.Compilation;

            foreach (var compSyntaxTree in comp.SyntaxTrees)
            {
                var root = compSyntaxTree.GetRoot();
                var classDeclarationSyntax = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                if (classDeclarationSyntax is null || classDeclarationSyntax.SyntaxTree.FilePath.EndsWith(".g.cs"))
                {
                    continue;
                }
                var sModel = comp.GetSemanticModel(compSyntaxTree);
                var classSymbol = sModel.GetDeclaredSymbol(classDeclarationSyntax);

                if (classSymbol is { IsAbstract: false } && classSymbol.AllInterfaces.ToArray().Any(a => a.Name == "IEntity"))
                {
                    symbols.Add(classSymbol);
                }
            }

            var methodNames = symbols.Select(a => $"Query{ new Pluralizer().Pluralize(a.Name)}").ToList();
            var whereMethods = new Dictionary<string, string>();
            foreach (var methodName in methodNames)
            {
                foreach (var compSyntaxTree in comp.SyntaxTrees)
                {
                    var root = compSyntaxTree.GetRoot();
                    var methodSymbol = root.DescendantNodes().OfType<IdentifierNameSyntax>()
                        .Where(b => b.Identifier.ValueText == methodName);
                   

                    foreach (var syntax in methodSymbol)
                    {
                        if (syntax?.Parent?.Parent is not InvocationExpressionSyntax expressionSyntax)
                        {
                            continue;
                        }

                        var args =
                            expressionSyntax
                                .ArgumentList.Arguments;
                        if (args.Count < 2 || IsArgumentNull(args[1]))
                        {
                            continue;
                        }
                        
                        var expression = args[1].ToString().Replace("\n","").Replace("\r","");

                        var key = Data.Query.Helper.GetExpressionKey($"{methodName}-{expression}");
                        var response = (openAiService.GetSqlFromExpression(expression)).Result;

                        whereMethods.Add(key, GetWhereClause(response));
                    }
                }
            }

            string template = $@"
                namespace FirstOfficer.Data.Query
                {{
                       public class SqlParts : FirstOfficer.Data.Query.ISqlParts
                        {{

                               public Dictionary<string, string> WhereClauses {{ get; }} = new Dictionary<string, string>()
                               {{
                                {string.Join(", ", whereMethods.Select(a => $"{{\"{a.Key}\", \"{a.Value}\"}}"))}
                               }};
                        }}

                }}

";

            
            context.AddSource("SqlParts.g.cs",
                SourceText.From( SyntaxHelper.FormatCode(template), Encoding.UTF8));




        }

        private static string GetWhereClause(string sql)
        {
            var rtn = sql.Substring(sql.IndexOf("WHERE", StringComparison.Ordinal));
            rtn = rtn.Replace("\n", "").Replace("\r", "").Replace("Value", "value").Replace("@value_", "@value");
            return rtn;

        }

        private static bool IsArgumentNull(ArgumentSyntax argument)
        {
            return argument.Expression is LiteralExpressionSyntax literal &&
                   literal.Kind() == SyntaxKind.NullLiteralExpression;
        }
    }
}