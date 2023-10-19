using System.Text;
using FirstOfficer.Generator.Extensions;
using FirstOfficer.Generator.Services;
using Microsoft.CodeAnalysis;
using Pluralize.NET;

namespace FirstOfficer.Generator.Syntax.Templates
{
    internal static class DatabaseQueryable
    {

        internal static string GetTemplate(INamedTypeSymbol symbol)
        {
            var sb = new StringBuilder();
            var props = OrmSymbolService.GetQueryableProperties(symbol);

            var name = symbol.Name;
            sb.Append($@"

                public struct {symbol.Name}Queryable
                {{
");
            foreach (var prop in props)
            {
                sb.AppendLine($"public FirstOfficer.Data.Query.Value {prop.Name} {{ get; }}");
            }
            sb.AppendLine($@" 
                }}

         
");

            sb.AppendLine($@"  public enum {name}OrderBy
        {{");

            foreach (var prop in OrmSymbolService.GetOrderByProperties(symbol))
            {
                sb.AppendLine($"{prop.Name}Asc,");
                sb.AppendLine($"{prop.Name}Desc,");
            }

            sb.AppendLine(" }");


            return sb.ToString();
        }
    }
}
