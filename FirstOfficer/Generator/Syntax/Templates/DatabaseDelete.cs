using System.Text;
using FirstOfficer.Extensions;
using FirstOfficer.Generator.Helpers;
using Microsoft.CodeAnalysis;
using Pluralize.NET;

namespace FirstOfficer.Generator.Syntax.Templates
{
    internal static class DatabaseDelete
    {

        internal static string GetTemplate(INamedTypeSymbol symbol)
        {
            var sb = new StringBuilder();

            var name = symbol.Name;
            sb.Append($@"    public static async Task Delete{new Pluralizer().Pluralize(name)}(this IDbConnection dbConnection, IEnumerable<{symbol.FullName()}> entities, IDbTransaction transaction)
                              {{
                                var db = dbConnection as NpgsqlConnection;
                                
                                var take = 65535;
                                var count = 0;
                                var skip = 0;
                                var recordCount = entities.Count();
                                var batch = entities.Skip(skip).Take(take);
                                while (skip < recordCount)
                                {{
                                
                                    await using (var command = new NpgsqlCommand(string.Empty, dbConnection as Npgsql.NpgsqlConnection, transaction as NpgsqlTransaction))
                                    {{
                                        var indexes = new List<int>();
                                        for (int i = 0; i < batch.Count(); i++)
                                        {{
                                            indexes.Add(i);
                                            command.Parameters.Add(new NpgsqlParameter($""Id_{{i}}"", batch.ElementAt(i).Id));
                                        }}
                                        var sql = $""DELETE FROM {DataHelper.GetTableName(name)} WHERE Id in ({{string.Join("","",indexes.Select(a=> $""@Id_{{a}}""))}});"";
                                        command.CommandText = sql;
                                        await command.ExecuteNonQueryAsync();
                                    }}

                                    count++;
                                    skip = count * take;
                                    batch = entities.Skip(skip).Take(take);
                                }}
                            }}
                ");

            sb.Append($@"    public static async Task Delete{name}(this IDbConnection dbConnection, {symbol.FullName()} entity, IDbTransaction transaction)
                              {{
                                await Delete{new Pluralizer().Pluralize(name)}(dbConnection, new List<{symbol.FullName()}>() {{ entity }}, transaction);
                            }}
                ");


            return sb.ToString();
        }
    }
}
