using System.Text;
using FirstOfficer.Generator.Extensions;
using FirstOfficer.Generator.Helpers;
using FirstOfficer.Generator.Services;
using Microsoft.CodeAnalysis;
using Pluralize.NET;

namespace FirstOfficer.Generator.Syntax.Templates
{
    internal static class DatabaseSaveChildren
    {

        internal static string GetTemplate(INamedTypeSymbol entitySymbol)
        {
            var sb = new StringBuilder();
            var oneToOnes = OrmSymbolService.GetOneToOneProperties(entitySymbol).ToArray();
            var oneToMany = OrmSymbolService.GetOneToManyProperties(entitySymbol).ToArray();
            var properties = OrmSymbolService.GetMappedProperties(entitySymbol).ToArray();
            var name = entitySymbol.Name;

            sb.AppendLine($@"

            private static async Task SaveOneToOne(IDbConnection dbConnection, IEnumerable<{entitySymbol.FullName()}> entities, IDbTransaction transaction)
            {{
       
            
            //handle one-to-one
            ");

            if (oneToOnes.Any())
            {

                foreach (var oneToOne in oneToOnes)
                {
                    sb.AppendLine(
                        $"await dbConnection.Save{new Pluralizer().Pluralize(oneToOne.Type.Name)}(entities.Where(a=> a.{oneToOne.Name} != null).Select(a=> a.{oneToOne.Name}), transaction);");
                }

                sb.AppendLine($@"  
            foreach(var entity in entities)
            {{
                    ");

                foreach (var oneToOne in oneToOnes)
                {

                    sb.AppendLine($@"if (entity.{oneToOne.Type.Name} != null)                            
                        {{
                             entity.{oneToOne.Type.Name}Id = entity.{oneToOne.Type.Name}.Id;
                        }}                     
                    ");
                }

                sb.AppendLine($@"            }}
     
                    ");

            }

            sb.AppendLine($@"  
            }}
            private static async Task SaveOneToMany(IDbConnection dbConnection, IEnumerable<{entitySymbol.FullName()}> entities, IDbTransaction transaction)
            {{       

            //handle one-to-many    
            ");

            if (oneToMany.Any())
            {
                foreach (var oneToManyProperty in oneToMany)
                {
                    var childType = ((INamedTypeSymbol)oneToManyProperty.Type).TypeArguments[0];
                    sb.AppendLine($"entities.ToList().ForEach(a=> a.{oneToManyProperty.Name}.ToList().ForEach(b=> b.{entitySymbol.Name}Id = a.Id));");
                    sb.AppendLine($"await dbConnection.Save{new Pluralizer().Pluralize(childType.Name)}(entities.SelectMany(a=> a.{oneToManyProperty.Name}), transaction);");
                }
            }

            sb.AppendLine($@" }}");

            var manyToManyTypes = OrmSymbolService.GetManyToManyProperties(entitySymbol).ToArray();

            var manyToMany = DataHelper.GetManyToMany(entitySymbol);

            sb.AppendLine($@" private static async Task SaveManyToMany(IDbConnection dbConnection,
                                    IEnumerable<{entitySymbol.FullName()}> entities, IDbTransaction transaction)
                    {{
                    ");
            foreach (var manyToManyProperty in manyToMany)
            {
                var tableName = manyToManyProperty.Key;
                var prop1IsEntity =
                    SymbolEqualityComparer.Default.Equals((manyToManyProperty.Value.Item1.Type as INamedTypeSymbol)?.TypeArguments[0], entitySymbol);
                var prop1 = prop1IsEntity ? manyToManyProperty.Value.Item1 : manyToManyProperty.Value.Item2;
                var type1 = (prop1.Type as INamedTypeSymbol)?.TypeArguments[0];
                var idName1 = $"{type1.Name.ToSnakeCase()}_id";
                var prop2 = prop1IsEntity ? manyToManyProperty.Value.Item2 : manyToManyProperty.Value.Item1;
                var type2 = (prop2.Type as INamedTypeSymbol)?.TypeArguments[0];
                var idName2 = $"{type2.Name.ToSnakeCase()}_id";
               
   
                sb.AppendLine($"await dbConnection.Save{new Pluralizer().Pluralize(type2.Name)}(entities.SelectMany(a=> a.{prop2.Name}), transaction);");

                sb.AppendLine($@"      //handle many-to-many
                                var pairs = entities
                                    .SelectMany(a => a.{prop2.Name},
                                        (b, c) => new KeyValuePair<long, long>(b.Id, c.Id)).ToList();


                        var pairs1 = pairs.Select(p => p.Key).ToArray();
                        var pairs2 = pairs.Select(p => p.Value).ToArray();


                        //handle insert
                    string insertSql = @""
                        WITH input_pairs AS (
                            SELECT UNNEST(@a::bigint[]) AS col1, UNNEST(@b::bigint[]) AS col2
                        )
                        INSERT INTO {tableName} ({idName1}, {idName2})
                        SELECT ip.col1, ip.col2 
                        FROM input_pairs ip
                        LEFT JOIN {tableName} ba ON ip.col1 = ba.{idName1} AND ip.col2 = ba.{idName2}
                        WHERE ba.{idName1} IS NULL AND ba.{idName2} IS NULL;
                "";

                    using var insertCommand = new NpgsqlCommand(insertSql, dbConnection as NpgsqlConnection, transaction as NpgsqlTransaction);
                    insertCommand.Parameters.AddWithValue(""a"", pairs1);
                    insertCommand.Parameters.AddWithValue(""b"", pairs2);
                    insertCommand.ExecuteNonQuery();

                    //handle delete
                    string deleteSql = @""
                                WITH input_pairs AS (
                                    SELECT UNNEST(@a::int[]) AS col1, UNNEST(@b::int[]) AS col2
                                )
                                DELETE FROM {tableName} 
                                WHERE ({idName1}, {idName2}) NOT IN (SELECT col1, col2 FROM input_pairs);
                            "";
                    using var deleteCommand = new NpgsqlCommand(deleteSql, dbConnection as NpgsqlConnection, transaction as NpgsqlTransaction);
                    deleteCommand.Parameters.AddWithValue(""a"", pairs1);
                    deleteCommand.Parameters.AddWithValue(""b"", pairs2);
                    deleteCommand.ExecuteNonQuery();

                ");
            }

            sb.AppendLine($@"}}");

            return sb.ToString();
        }
    }
}
