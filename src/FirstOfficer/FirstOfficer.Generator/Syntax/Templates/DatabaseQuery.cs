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
             public static async Task<IQueryable<{symbol.FullName()}>> Query{new Pluralizer().Pluralize(name)}(this IDbConnection dbConnection, Includes flags)
             {{
                var db = dbConnection as NpgsqlConnection;
                await using (var command = db.CreateCommand())
                {{
                    command.CommandText = GetSql(flags);
                    await using(var reader = await command.ExecuteReaderAsync())
                    {{
                        return (await {name}Mapper(reader)).AsQueryable();
                    }}
                }}
             }}

            public static IQueryable<{symbol.FullName()}> {symbol.Name}Filter(
                this IQueryable<{symbol.FullName()}> list, System.Linq.Expressions.Expression<Func<{symbol.Name}Queryable, bool>> expression)
            {{
                    var body = expression.Body.ToString();

                    // Replace parameter name with table name
                    body = body.Replace(""b."", """");

                    // Handle equality/inequality
                    body = body.Replace(""=="", ""="");
                    body = body.Replace(""!="", ""<>"");

                    // Handle logical operators
                    body = body.Replace(""&&"", ""AND"");
                    body = body.Replace(""||"", ""OR"");

                    // Surround strings with single quotes for SQL
                    var matches = System.Text.RegularExpressions.Regex.Matches(body, ""\""(.*?)\"""");
                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {{
                        var value = match.Groups[1].Value;
                        body = body.Replace($""\""{{value}}\"""", $""'{{value}}'"");
                    }}

                    // Handle SQL column naming conventions if needed
                    body = body.Replace(""Title"", ""\""Title\"""");
                    body = body.Replace(""Author"", ""\""Author\"""");
                    body = body.Replace(""Year"", ""\""Year\"""");

                    return default;
            }}

                public struct {symbol.Name}Queryable
                {{
                    public long Id {{ get; }}
                    public string Checksum {{ get; }}

                }}

             private static string GetSql(Includes flags)               
             {{
                var joins = string.Empty;

");
            
           
            foreach (var propertySymbol in flagProperties)
            {
                sb.AppendLine($"if ((flags & Includes.{propertySymbol.Name}) == Includes.{propertySymbol.Name})");
                sb.AppendLine("{");
                if (CodeAnalysisHelper.IsEntity(propertySymbol.Type))
                {
                    var propName = new Pluralizer().Singularize(propertySymbol.Name);
                    columnProperties.Add($"{DataHelper.GetTableName(propName)}.id as {DataHelper.GetTableName(propName)}_id ");
                    columnProperties.AddRange(CodeAnalysisHelper.GetMappedProperties((INamedTypeSymbol)propertySymbol.Type)
                        .Select(a => $"{DataHelper.GetTableName(propName)}.{a.Name.ToSnakeCase()} as {DataHelper.GetTableName(propName)}_{a.Name.ToSnakeCase()} "));
                
                 sb.AppendLine($@"joins += "" LEFT OUTER JOIN {DataHelper.GetTableName(propertySymbol.Type.Name)} ON {DataHelper.GetTableName(symbol.Name)}.{ (propName + "Id").ToSnakeCase()} = {DataHelper.GetTableName(propertySymbol.Type.Name)}.Id "";");
                }

                sb.AppendLine("}");
            }

            sb.AppendLine($@"var sql = $""SELECT {string.Join(", ", columnProperties)} FROM {DataHelper.GetTableName(name)} {{joins}} ;"";");
            sb.AppendLine("return sql;");

            sb.AppendLine("}");
            
            return sb.ToString();
        }
    }
}
