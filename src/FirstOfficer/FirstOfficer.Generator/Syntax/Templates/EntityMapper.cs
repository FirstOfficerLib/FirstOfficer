using System.Text;
using FirstOfficer.Generator.Extensions;
using FirstOfficer.Generator.Helpers;
using Microsoft.CodeAnalysis;

namespace FirstOfficer.Generator.Syntax.Templates
{
    internal static class EntityMapper
    {

        internal static string GetTemplate(INamedTypeSymbol entity)
        {
            var sb = new StringBuilder();

            var dbProps = CodeAnalysisHelper.GetAllProperties(entity).Where(a =>
                a.Type is INamedTypeSymbol { IsGenericType: false } symbol &&
                symbol.AllInterfaces.All(b => b.Name != "IEntity")).ToList();

            var entityName = entity.Name;
            var tableName = DataHelper.GetTableName(entityName);

            sb.AppendLine($@"
             private static async Task<IEnumerable<{entity.FullName()}>> {entityName}Mapper(NpgsqlDataReader reader)
            {{
                var entities = new List<{entity.FullName()}>();
                 while (await reader.ReadAsync())
                {{
                    var entity = new {entity.FullName()}();
                    { GetMapping(dbProps, tableName) }
                    entities.Add(entity);
                }}
                return entities;
            }}
            ");

            return sb.ToString();
        }

        private static string GetMapping(IEnumerable<IPropertySymbol> propertySymbols, string tableName)
        {
            var sb = new StringBuilder();

            foreach (var propertySymbol in propertySymbols)
            {
                var columnName = $"{tableName}_{propertySymbol.Name.ToSnakeCase()}";
                sb.AppendLine($"if (reader[\"{columnName}\"] != DBNull.Value) entity.{propertySymbol.Name} = ({propertySymbol.Type.Name})reader[\"{columnName}\"];");
            }
            return sb.ToString();
        }


    }
}
