using System.CodeDom;
using System.Text;
using FirstOfficer.Generator.Extensions;
using FirstOfficer.Generator.Syntax.Templates;
using Microsoft.CodeAnalysis;
using Pluralize.NET;

namespace FirstOfficer.Generator.Syntax
{
    internal static class SaveGenerator
    {
        internal static CodeSnippetTypeMember GenerateSaveMethods(INamedTypeSymbol entitySymbol)
        {
            string unformattedCode = string.Empty;


            var bodyBuilder = new StringBuilder();
            bodyBuilder.AppendLine(
                $"public static async Task Save{new Pluralizer().Pluralize(entitySymbol.Name)}(this IDbConnection dbConnection, IEnumerable<{entitySymbol.FullName()}> entities, IDbTransaction transaction, bool saveChildren = false)");
            bodyBuilder.AppendLine("{");
            bodyBuilder.AppendLine($"var insertEntities = new List<{entitySymbol.FullName()}>();");
            bodyBuilder.AppendLine($"var updateEntities = new List<{entitySymbol.FullName()}>();");
            bodyBuilder.AppendLine("foreach (var entity in entities)");
            bodyBuilder.AppendLine("{");
            bodyBuilder.AppendLine("if (entity.Id == 0)");
            bodyBuilder.AppendLine("{");
            bodyBuilder.AppendLine("insertEntities.Add(entity);");
            bodyBuilder.AppendLine("}");
            bodyBuilder.AppendLine("else");
            bodyBuilder.AppendLine("{");
            bodyBuilder.AppendLine("updateEntities.Add(entity);");
            bodyBuilder.AppendLine("}");
            bodyBuilder.AppendLine("}");
            bodyBuilder.AppendLine($"await Insert{entitySymbol.Name}(dbConnection, insertEntities, transaction, saveChildren);");
            bodyBuilder.AppendLine($"await Update{entitySymbol.Name}(dbConnection, updateEntities, transaction, saveChildren);");
            bodyBuilder.AppendLine("}");
            bodyBuilder.AppendLine();

            bodyBuilder.AppendLine(
                $"public static async Task Save{entitySymbol.Name}(this IDbConnection dbConnection, {entitySymbol.FullName()} entity, IDbTransaction transaction, bool saveChildren = false)");
            bodyBuilder.AppendLine("{");
            bodyBuilder.AppendLine("if (entity.Id == 0)");
            bodyBuilder.AppendLine("{");
            bodyBuilder.AppendLine($"await Insert{entitySymbol.Name}(dbConnection, new List<{entitySymbol.FullName()}>() {{ entity }}, transaction, saveChildren);");
            bodyBuilder.AppendLine("}");
            bodyBuilder.AppendLine("else");
            bodyBuilder.AppendLine("{");
            bodyBuilder.AppendLine($"await Update{entitySymbol.Name}(dbConnection, new List<{entitySymbol.FullName()}>(){{ entity }}, transaction, saveChildren);");
            bodyBuilder.AppendLine("}");
            bodyBuilder.AppendLine("}");

            bodyBuilder.AppendLine(DatabaseInsert.GetTemplate(entitySymbol));
            bodyBuilder.AppendLine(DatabaseUpdate.GetTemplate(entitySymbol));

            unformattedCode += bodyBuilder.ToString();


            return new CodeSnippetTypeMember(unformattedCode);
        }

    }
}
