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

            var i = 0;
            foreach (var manyToManyProperty in manyToMany)
            {
                var tableName = manyToManyProperty.Key;
                var prop1IsEntity =
                    SymbolEqualityComparer.Default.Equals((manyToManyProperty.Value.Item1.Type as INamedTypeSymbol)?.TypeArguments[0], entitySymbol);
                var prop1 = prop1IsEntity ? manyToManyProperty.Value.Item1 : manyToManyProperty.Value.Item2;
                var type1 = (prop1.Type as INamedTypeSymbol)?.TypeArguments[0];
                var idName1 = Data.DataHelper.GetIdColumnName(prop1.Name);
                var prop2 = prop1IsEntity ? manyToManyProperty.Value.Item2 : manyToManyProperty.Value.Item1;
                var type2 = (prop2.Type as INamedTypeSymbol)?.TypeArguments[0];
                var idName2 = Data.DataHelper.GetIdColumnName(prop2.Name);
                idName2 = Data.DataHelper.GetIdColumnName(prop2.Name, idName1 == idName2);  //handle many-to-many to self

                sb.AppendLine($" await dbConnection.Save{new Pluralizer().Pluralize(type2.Name)}(entities.SelectMany(a=> a.{prop2.Name}), transaction);");

                sb.AppendLine($@"      
                        //handle many-to-many
                                var pairs{i} = entities
                                    .SelectMany(a => a.{prop2.Name},
                                        (b, c) => new KeyValuePair<long, long>(b.Id, c.Id)).ToList();


                        var pairs1{i} = pairs{i}.Select(p => p.Key).ToArray();
                        var pairs2{i} = pairs{i}.Select(p => p.Value).ToArray();


                        //handle insert
                    string insertSql{i} = @""
                        WITH input_pairs AS (
                            SELECT UNNEST(@a::bigint[]) AS col1, UNNEST(@b::bigint[]) AS col2
                        )
                        INSERT INTO {tableName} ({idName1}, {idName2})
                        SELECT ip.col1, ip.col2 
                        FROM input_pairs ip
                        LEFT JOIN {tableName} ba ON ip.col1 = ba.{idName1} AND ip.col2 = ba.{idName2}
                        WHERE ba.{idName1} IS NULL AND ba.{idName2} IS NULL;
                "";

                    using var insertCommand{i} = new NpgsqlCommand(insertSql{i}, dbConnection as NpgsqlConnection, transaction as NpgsqlTransaction);
                    insertCommand{i}.Parameters.AddWithValue(""a"", pairs1{i});
                    insertCommand{i}.Parameters.AddWithValue(""b"", pairs2{i});
                    insertCommand{i}.ExecuteNonQuery();

                    //handle delete
                    string deleteSql{i} = @""
                                WITH input_pairs AS (
                                    SELECT UNNEST(@a::bigint[]) AS col1, UNNEST(@b::bigint[]) AS col2
                                )
                                DELETE FROM {tableName} 
                                WHERE {idName1} = ANY(@c::bigint[]) AND ({idName1}, {idName2}) NOT IN (SELECT col1, col2 FROM input_pairs);
                            "";
                    using var deleteCommand{i} = new NpgsqlCommand(deleteSql{i}, dbConnection as NpgsqlConnection, transaction as NpgsqlTransaction);
                    deleteCommand{i}.Parameters.AddWithValue(""a"", pairs1{i});
                    deleteCommand{i}.Parameters.AddWithValue(""b"", pairs2{i});
                    deleteCommand{i}.Parameters.AddWithValue(""c"", pairs1{i}.Distinct().ToArray());
                    deleteCommand{i}.ExecuteNonQuery();


                ");

                i++;
            }

            sb.AppendLine($@"}}");

            return sb.ToString();
        }
    }
}
