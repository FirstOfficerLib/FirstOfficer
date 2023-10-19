using FirstOfficer.Generator.Extensions;
using FirstOfficer.Generator.Helpers;
using FirstOfficer.Generator.Services;
using Microsoft.CodeAnalysis;
using Pluralize.NET;

namespace FirstOfficer.Generator.Syntax.Templates
{
    internal static class DatabaseInsert
    {

        internal static string GetTemplate(INamedTypeSymbol entitySymbol)
        {
            var entityName = entitySymbol.Name;
            var properties = OrmSymbolService.GetMappedProperties(entitySymbol).ToArray();
            var columnProperties = OrmSymbolService.GetMappedProperties(entitySymbol).Select(a => a.Name.ToSnakeCase()).ToArray();
            var valueProperties = OrmSymbolService.GetMappedProperties(entitySymbol).Select(p => p.Name).ToArray();
          

            var rtn = $@"
        private static async Task Insert{entityName}(IDbConnection dbConnection, IEnumerable<{entitySymbol.FullName()}> insertEntities, IDbTransaction transaction)
        {{          
            var take = Convert.ToInt32(65535 / {columnProperties.Length});
            var count = 0;
            var skip = 0;
            var recordCount = insertEntities.Count();
            var batch = insertEntities.Skip(skip).Take(take);
            while (skip < recordCount)
            {{
                var values = new List<string>();
                for (int i = 0; i < batch.Count(); i++)
                {{
                    values.Add($""({string.Join(", ", valueProperties.Select(a => $"@{a}_{{i}}"))})"");
                }}

                var sql = $""INSERT INTO {DataHelper.GetTableName(entitySymbol.Name)} ({string.Join(", ", columnProperties)}) VALUES {{string.Join("","", values)}} RETURNING id;"";
                using (var command = new Npgsql.NpgsqlCommand(sql, dbConnection as Npgsql.NpgsqlConnection, transaction as Npgsql.NpgsqlTransaction))
                {{
                    for (int i = 0; i < batch.Count(); i++)
                    {{
                        var entity = batch.ElementAt(i);
";

            foreach (var prop in properties.Where(a => a.Name != "Checksum"))
            {
                if (((INamedTypeSymbol)prop.Type).FullName() == typeof(DateTime).FullName ||
                    ((INamedTypeSymbol)prop.Type).FullName() == typeof(DateTime?).FullName)
                {
                    //GeneratedHelpers.RoundToNearestMillisecond

                    rtn += $@"entity.{prop.Name} = GeneratedHelpers.RoundToNearestMillisecond(entity.{prop.Name});
                                ";
                }

                rtn += $@"command.Parameters.AddWithValue($""@{prop.Name}_{{i}}"", {OrmSymbolService.HandleWhenNull(prop)});
                                      ";

            }

            rtn += $@"command.Parameters.AddWithValue($""@Checksum_{{i}}"", entity.Checksum());
                        ";

            rtn += $@"}}

                    using (var reader = await command.ExecuteReaderAsync())
                    {{
                        foreach (var entity in batch)
                        {{
                            if (await reader.ReadAsync())
                            {{
                                entity.Id = (long)reader.GetInt64(0);
                            }}
                        }}
                    }}
                }}

                count++;
                skip = count * take;
                batch = insertEntities.Skip(skip).Take(take);
            }}
        }}
";

            return rtn;
        }

    }
}
