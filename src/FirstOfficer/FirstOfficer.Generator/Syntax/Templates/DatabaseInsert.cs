using FirstOfficer.Generator.Extensions;
using FirstOfficer.Generator.Helpers;
using Microsoft.CodeAnalysis;
using Pluralize.NET;

namespace FirstOfficer.Generator.Syntax.Templates
{
    internal static class DatabaseInsert
    {

        internal static string GetTemplate(INamedTypeSymbol entitySymbol)
        {
            var entityName = entitySymbol.Name;
            var properties = CodeAnalysisHelper.GetMappedProperties(entitySymbol).ToArray();
            var columnProperties = CodeAnalysisHelper.GetMappedProperties(entitySymbol).Select(a => a.Name.ToSnakeCase()).ToArray();
            var valueProperties = CodeAnalysisHelper.GetMappedProperties(entitySymbol).Select(p => p.Name).ToArray();
            var oneToOnes = CodeAnalysisHelper.GetOneToOneProperties(entitySymbol).ToArray();

            var rtn = $@"
        private static async Task Insert{entityName}(IDbConnection dbConnection, IEnumerable<{entitySymbol.FullName()}> insertEntities, IDbTransaction transaction, bool saveChildren = false)
        {{

            if(saveChildren)
            {{
            ValidateChildren(insertEntities);
            //handle one-to-one
            ";
            foreach (var oneToOne in oneToOnes)
            {
                rtn += $"await dbConnection.Save{new Pluralizer().Pluralize(oneToOne.Type.Name)}(insertEntities.Where(a=> a.{oneToOne.Name} != null).Select(a=> a.{oneToOne.Name}), transaction);";
            }

            rtn += $@"  
            foreach(var entity in insertEntities)
            {{
";

            foreach (var oneToOne in oneToOnes)
            {
                rtn += $@"if(entity.{oneToOne.Type.Name} != null)
                            {{
                            entity.{oneToOne.Type.Name}Id = entity.{oneToOne.Type.Name}.Id;
                            }}
                            ";
            }

            rtn += $@"
            }} //saveChildren
            }}
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

                rtn += $@"command.Parameters.AddWithValue($""@{prop.Name}_{{i}}"", entity.{prop.Name});
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

        private static void ValidateChildren(IEnumerable<{entitySymbol.FullName()}> insertEntities)
        {{

";
            foreach (var oneToOne in oneToOnes)
            {
                if (properties.Any(a =>
                        a.Name == $"{oneToOne.Name}Id" &&
                        ((INamedTypeSymbol)a.Type).FullName() == typeof(long).FullName))
                {
                    rtn += $@"if (insertEntities.Any(a => a.Book == null))
                            {{
                                throw new FirstOfficer.Data.Exceptions.MissingEntityException(""{oneToOne.Name} is required."");                             
                            }}";
                }
            }

            rtn += $@"      }} ";
            return rtn;
        }

    }
}
