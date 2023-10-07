using System.Text;
using FirstOfficer.Generator.Extensions;
using FirstOfficer.Generator.Helpers;
using Microsoft.CodeAnalysis;
using Pluralize.NET;

namespace FirstOfficer.Generator.Syntax.Templates
{
    internal static class DatabaseSaveChildren
    {

        internal static string GetTemplate(INamedTypeSymbol entitySymbol)
        {
            var sb = new StringBuilder();
            var oneToOnes = CodeAnalysisHelper.GetOneToOneProperties(entitySymbol).ToArray();
            var oneToMany = CodeAnalysisHelper.GetOneToManyProperties(entitySymbol).ToArray();
            var properties = CodeAnalysisHelper.GetMappedProperties(entitySymbol).ToArray();
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
            return sb.ToString();
        }
    }
}
