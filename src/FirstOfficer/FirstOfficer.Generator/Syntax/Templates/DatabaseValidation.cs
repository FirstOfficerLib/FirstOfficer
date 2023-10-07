using System.Text;
using FirstOfficer.Generator.Extensions;
using FirstOfficer.Generator.Helpers;
using Microsoft.CodeAnalysis;
using Pluralize.NET;

namespace FirstOfficer.Generator.Syntax.Templates
{
    internal static class DatabaseValidation
    {

        internal static string GetTemplate(INamedTypeSymbol entitySymbol)
        {
            var sb = new StringBuilder();
            var oneToOnes = CodeAnalysisHelper.GetOneToOneProperties(entitySymbol).ToArray();
            var properties = CodeAnalysisHelper.GetMappedProperties(entitySymbol).ToArray();
            var name = entitySymbol.Name;
            sb.Append($@"           private static void ValidateChildren(IEnumerable<{entitySymbol.FullName()}> entities, bool saveChildren)
        {{

            ");
            foreach (var oneToOne in oneToOnes)
            {
                if (properties.Any(a =>
                        a.Name == $"{oneToOne.Name}Id" &&
                        ((INamedTypeSymbol)a.Type).FullName() == typeof(long).FullName))
                {
                    sb.Append($@"if (entities.Any(a => a.{oneToOne.Type.Name} == null && a.{oneToOne.Type.Name}Id == 0))
                            {{
                                throw new FirstOfficer.Data.Exceptions.MissingEntityException(""{oneToOne.Type.Name} is required."");                             
                            }}");
                }

                sb.Append($@" if (saveChildren && entities.Any(a => ((a.{oneToOne.Type.Name}?.Id) ?? 0) != 0 && a.{oneToOne.Type.Name}Id != a.{oneToOne.Type.Name}!.Id))
                                {{
                                    throw new FirstOfficer.Data.Exceptions.ForeignIdMismatchException(""{entitySymbol.Name} {oneToOne.Type.Name}Id does not match {oneToOne.Type.Name} Id."");
                                }}");
            }

            sb.Append($@"     }} ");

            return sb.ToString();
        }
    }
}
