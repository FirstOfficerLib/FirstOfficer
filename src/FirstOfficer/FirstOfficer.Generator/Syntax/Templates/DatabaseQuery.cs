using System.Text;
using FirstOfficer.Generator.Extensions;
using FirstOfficer.Generator.Helpers;
using Microsoft.CodeAnalysis;
using Pluralize.NET;

namespace FirstOfficer.Generator.Syntax.Templates
{
    internal static class DatabaseQuery
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
             public static async Task<IEnumerable<{symbol.FullName()}>> Query{new Pluralizer().Pluralize(name)}(this IDbConnection dbConnection, Includes includes,  System.Linq.Expressions.Expression<Func<{symbol.Name}Queryable, bool>> expression = null)
             {{
                var db = dbConnection as NpgsqlConnection;
                await using (var command = db.CreateCommand())
                {{
                    command.CommandText = GetSql(includes, expression);
                    await using(var reader = await command.ExecuteReaderAsync())
                    {{
                        return await {symbol.Name}Mapper(reader);
                    }}
                }}
             }}

  
             private static string GetSql(Includes includes, System.Linq.Expressions.Expression<Func<{symbol.Name}Queryable, bool>> expression)               
             {{
                var joins = string.Empty;
                var whereClause = GetWhereClause(expression);
                var cols = new List<string>() {{ {string.Join(", ", columnProperties.Select(a => $@"""{a}"""))} }};
");

            foreach (var propertySymbol in flagProperties)
            {
                sb.AppendLine($"if ((includes & Includes.{propertySymbol.Name}) == Includes.{propertySymbol.Name})");
                sb.AppendLine("{");
                if (CodeAnalysisHelper.IsEntity(propertySymbol.Type))
                {
                    var moreCols = new List<string>();
                    var propName = new Pluralizer().Singularize(propertySymbol.Name);
                    moreCols.Add($"{DataHelper.GetTableName(propName)}.id as {DataHelper.GetTableName(propName)}_id ");
                    moreCols.AddRange(CodeAnalysisHelper.GetMappedProperties((INamedTypeSymbol)propertySymbol.Type)
                        .Select(a => $"{DataHelper.GetTableName(propName)}.{a.Name.ToSnakeCase()} as {DataHelper.GetTableName(propName)}_{a.Name.ToSnakeCase()} "));


                    sb.AppendLine($"cols.AddRange(new[]{{ {string.Join(",", moreCols.Select(a=> $@"""{a}"""))} }});");
                    sb.AppendLine($@"joins += "" LEFT OUTER JOIN {DataHelper.GetTableName(propertySymbol.Type.Name)} ON {DataHelper.GetTableName(symbol.Name)}.{(propName + "Id").ToSnakeCase()} = {DataHelper.GetTableName(propertySymbol.Type.Name)}.Id "";");
                }

                sb.AppendLine("}");
            }

            sb.AppendLine($@"var sql = $""SELECT {{string.Join("", "", cols) }} FROM {DataHelper.GetTableName(name)} {{joins}} ;"";");
            sb.AppendLine("return sql;");

            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}
