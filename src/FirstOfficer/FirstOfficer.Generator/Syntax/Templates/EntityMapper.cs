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

            var dbProps = GetPropertySymbols(entity);

            var oneToOne = CodeAnalysisHelper.GetOneToOneProperties(entity);

            var entityName = entity.Name;
            var tableName = DataHelper.GetTableName(entityName);

            sb.AppendLine($@"
             private static async Task<IEnumerable<{entity.FullName()}>> {entityName}Mapper(NpgsqlDataReader reader)
            {{
                var entities = new List<{entity.FullName()}>();
                 while (await reader.ReadAsync())
                {{
                    var entity = new {entity.FullName()}();
                    {GetMapping(dbProps, tableName)}                       
");
            foreach (var propertySymbol in oneToOne)
            {
                sb.Append($"if(reader[\"{DataHelper.GetTableName(propertySymbol.Name)}_id\"] != DBNull.Value)");
                sb.AppendLine("{");
                sb.AppendLine($"entity.{propertySymbol.Name} = new();");
                sb.AppendLine("}");
                sb.AppendLine(GetMapping(GetPropertySymbols((INamedTypeSymbol)propertySymbol.Type), DataHelper.GetTableName(propertySymbol.Name), propertySymbol.Name));
            }

            sb.AppendLine($@"                
                 entities.Add(entity);
              }}   
                return entities;
           }}
            ");

            return sb.ToString();
        }

        private static List<IPropertySymbol> GetPropertySymbols(INamedTypeSymbol entity)
        {
            return CodeAnalysisHelper.GetAllProperties(entity).Where(a =>
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
