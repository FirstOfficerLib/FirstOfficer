using System.Text;
using FirstOfficer.Generator.Extensions;
using FirstOfficer.Generator.Helpers;
using Microsoft.CodeAnalysis;
using Pluralize.NET;

namespace FirstOfficer.Generator.Syntax.Templates
{
    internal static class DatabaseQueryable
    {

        internal static string GetTemplate(INamedTypeSymbol symbol)
        {
            var sb = new StringBuilder();
            var columnProperties = new List<string>() { $"{DataHelper.GetTableName(symbol.Name)}.id as {DataHelper.GetTableName(symbol.Name)}_id" };
            columnProperties.AddRange(CodeAnalysisHelper.GetMappedProperties(symbol)
                .Select(a => $"{DataHelper.GetTableName(symbol.Name)}.{a.Name.ToSnakeCase()} as {DataHelper.GetTableName(symbol.Name)}_{a.Name.ToSnakeCase()}").ToList());
            var flagProperties = CodeAnalysisHelper.GetFlagProperties(symbol);

            var name = symbol.Name;
            sb.Append($@"

            private static string GetWhereClause(System.Linq.Expressions.Expression<Func<{symbol.Name}Queryable, bool>> expression)
            {{
               return """";

            }}

                public struct {symbol.Name}Queryable
                {{
                    public long Id {{ get; }}
                    public string Checksum {{ get; }}

                }}

         
");
            
            
            return sb.ToString();
        }
    }
}
