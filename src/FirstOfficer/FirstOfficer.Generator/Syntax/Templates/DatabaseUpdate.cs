using System.Text;
using FirstOfficer.Generator.Extensions;
using FirstOfficer.Generator.Helpers;
using FirstOfficer.Generator.Services;
using Microsoft.CodeAnalysis;

namespace FirstOfficer.Generator.Syntax.Templates
{
    internal static class DatabaseUpdate
    {

        internal static string GetTemplate(INamedTypeSymbol entitySymbol)
        {

            var entityName = entitySymbol.Name;

            var mappedProperties = OrmSymbolService.GetMappedProperties(entitySymbol).ToArray();

            var columnProperties = OrmSymbolService.GetMappedProperties(entitySymbol).Select(a => a.Name.ToSnakeCase()).OrderBy(a => a).ToArray();
            var properties = OrmSymbolService.GetMappedProperties(entitySymbol).Select(a => a.Name).OrderBy(a => a).ToArray();

            var valueProperties = new List<string>() { "Id" };
            valueProperties.AddRange(properties);

            var updateBuilder = new StringBuilder();
            updateBuilder.AppendLine($@"UPDATE {DataHelper.GetTableName(entityName)} as a set ");
            var setters = new List<string>();
            foreach (var column in columnProperties)
            {
                setters.Add($@"{column} = b.{column}");
            }
            updateBuilder.AppendLine($@"{string.Join(",", setters)} FROM (VALUES");

            var rtn = $@" 
        private static async Task Update{entityName}(IDbConnection dbConnection, IEnumerable<{entitySymbol.FullName()}> updateEntities, IDbTransaction transaction)
        {{   

            var take = Convert.ToInt32(65535 / {valueProperties.Count});
            var count = 0;
            var skip = 0;
            var recordCount = updateEntities.Count();
            var batch = updateEntities.Skip(skip).Take(take);
            while (skip < recordCount)
            {{
                var updateBuilder = new StringBuilder(@""{updateBuilder}"");
                var values = new List<string>();
                for (int i = 0; i < batch.Count(); i++)
                {{
                    values.Add($""({string.Join(", ", valueProperties.Select(a => $"@{a}_{{i}}"))})"");
                }}

                updateBuilder.AppendLine($""{{string.Join("","", values)}}"");
                updateBuilder.AppendLine("") as b(id,{string.Join(",", columnProperties)})"");
                updateBuilder.AppendLine($""WHERE a.id = b.id;"");

                var sql = updateBuilder.ToString();
                await using (var command = new NpgsqlCommand(sql, dbConnection as Npgsql.NpgsqlConnection, transaction as Npgsql.NpgsqlTransaction))
                {{
                    for (int i = 0; i < batch.Count(); i++)
                    {{
                        var entity = batch.ElementAt(i);
                        {string.Join("\r\n", mappedProperties.Where(a => a.Name != "Checksum").Select(prop =>
            {
                var rtn = string.Empty;

                if (((INamedTypeSymbol)prop.Type).FullName() == typeof(DateTime).FullName ||
                    ((INamedTypeSymbol)prop.Type).FullName() == typeof(DateTime?).FullName)
                {
                    //GeneratedHelpers.RoundToNearestMillisecond

                    rtn += $@"entity.{prop.Name} = GeneratedHelpers.RoundToNearestMillisecond(entity.{prop.Name});
                                ";
                }


                return rtn + $@"command.Parameters.AddWithValue($""{prop.Name}_{{i}}"", {OrmSymbolService.HandleWhenNull(prop)});";
            }))}
                        command.Parameters.AddWithValue($""Checksum_{{i}}"", entity.Checksum());
                        command.Parameters.AddWithValue($""Id_{{i}}"", entity.Id);
                    }}

                    await command.ExecuteNonQueryAsync();
                }}

                count++;
                skip = count * take;
                batch = updateEntities.Skip(skip).Take(take);
            }}
        }}";

            return rtn;
        }

    }
}
