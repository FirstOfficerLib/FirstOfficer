using System.Text;
using FirstOfficer.Generator.Extensions;
using FirstOfficer.Generator.Helpers;
using FirstOfficer.Generator.Services;
using Microsoft.CodeAnalysis;
using Pluralize.NET;

namespace FirstOfficer.Generator.Syntax.Templates
{
    internal static class EntityMapper
    {

        internal static string GetTemplate(INamedTypeSymbol entity)
        {
            var sb = new StringBuilder();

            var dbProps = GetPropertySymbols(entity);

            var oneToOne = OrmSymbolService.GetOneToOneProperties(entity);
            var manyToMany = OrmSymbolService.GetManyToManyProperties(entity);
            var entityName = entity.Name;
            var tableName = DataHelper.GetTableName(entityName);

            sb.AppendLine($@"
             private static async Task<IEnumerable<{entity.FullName()}>> {entityName}Mapper(NpgsqlDataReader reader)
            {{
                var entities = new List<{entity.FullName()}>();
                {string.Join("\n\r", manyToMany.Select(a => $"var contains{a.Name} = reader.GetColumnSchema().Any(a => a.ColumnName == \"{DataHelper.GetTableName(a.Name)}_id\");"))}
                
                var ids = new HashSet<long>();
                {string.Join("\r\n", manyToMany.Select(a=> $"var idsFor{a.Name} = new HashSet<long>();\n\r"))}
                 while (await reader.ReadAsync())
                {{
                    var entity = new {entity.FullName()}();
                    if ({(manyToMany.Any() ? "(" : string.Empty)}{string.Join(" && ", manyToMany.Select(a => $"!contains{a.Name}").ToList())}{(manyToMany.Any() ? ") || " : string.Empty)}
                    !ids.Contains((Int64)(reader[""{tableName}_id""])))
                {{
                                        
                    entity = RowMapper(reader);
                    entities.Add(entity);
                    ids.Add(entity.Id);
                }}
                else
                {{
                    entity = entities.FirstOrDefault(a => a.Id == (Int64)reader[""{tableName}_id""]);
                }}
                //add many to many ");

            foreach (var manyToManyProperty in manyToMany)
            {
                sb.AppendLine(GetManyToManyMapperCode(manyToManyProperty));

            }

            sb.AppendLine($@"                
                
              }}   
                return entities;
           }}

            internal static {entity.FullName()} RowMapper(NpgsqlDataReader reader)
            {{
                var entity = new {entity.FullName()}();
                {GetMapping(dbProps, tableName)}  
                return entity;
            }}
            ");

            return sb.ToString();
        }

        private static string GetManyToManyMapperCode(IPropertySymbol prop)
        {
            var namedTypeSymbol = ((INamedTypeSymbol)prop.Type).TypeArguments.First();
            return $@"  
                 if (contains{new Pluralizer().Pluralize(namedTypeSymbol.Name)} && (reader[""{DataHelper.GetTableName(namedTypeSymbol.Name)}_id""] ?? DBNull.Value) != DBNull.Value)
                {{
                    if(entity.{prop.Name} is null)
                    {{
                        throw new Exception(""entity.{prop.Name} is null"");
                    }}

                    if (!idsFor{prop.Name}.Contains((Int64)reader[""{DataHelper.GetTableName(namedTypeSymbol.Name)}_id""]))
                    {{
                        entity.{prop.Name}.Add(Entity{namedTypeSymbol.Name}.RowMapper(reader));
                        idsFor{prop.Name}.Add((Int64)reader[""{DataHelper.GetTableName(namedTypeSymbol.Name)}_id""]);
                    }}
                }}
            ";
        }

        private static List<IPropertySymbol> GetPropertySymbols(INamedTypeSymbol entity)
        {
            return SymbolService.GetAllProperties(entity).Where(a =>
                a.Type is INamedTypeSymbol { IsGenericType: false } symbol &&
                symbol.AllInterfaces.All(b => b.Name != "IEntity")).ToList();
        }

        private static string GetMapping(IEnumerable<IPropertySymbol> propertySymbols, string tableName,
            string propertyName = "")
        {
            var sb = new StringBuilder();

            foreach (var propertySymbol in propertySymbols)
            {
                var columnName = $"{tableName}_{propertySymbol.Name.ToSnakeCase()}";
                if (string.IsNullOrEmpty(propertyName))
                {
                    sb.AppendLine(
                        $"if (reader[\"{columnName}\"] != DBNull.Value) entity.{propertySymbol.Name} = ({propertySymbol.Type.Name})reader[\"{columnName}\"];");
                }
                else
                {
                    sb.AppendLine(
                        $"if (reader[\"{columnName}\"] != DBNull.Value) entity.{propertyName}.{propertySymbol.Name} = ({propertySymbol.Type.Name})reader[\"{columnName}\"];");
                }
            }
            return sb.ToString();
        }


    }
}
