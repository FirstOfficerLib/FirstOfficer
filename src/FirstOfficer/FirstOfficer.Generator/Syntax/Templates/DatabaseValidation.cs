using System.Text;
using FirstOfficer.Generator.Extensions;
using FirstOfficer.Generator.Services;
using Microsoft.CodeAnalysis;
using Pluralize.NET;

namespace FirstOfficer.Generator.Syntax.Templates
{
    internal static class DatabaseValidation
    {

        internal static string GetTemplate(INamedTypeSymbol entitySymbol)
        {
            var sb = new StringBuilder();
            var oneToOnes = OrmSymbolService.GetOneToOneProperties(entitySymbol).ToArray();
            var oneToMany = OrmSymbolService.GetOneToManyProperties(entitySymbol).ToArray();
            var properties = OrmSymbolService.GetMappedProperties(entitySymbol).ToArray();
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
                    sb.Append($@"
                            //one-to-one
                            if (entities.Any(a => a.{oneToOne.Type.Name} == null && a.{oneToOne.Type.Name}Id == 0))
                            {{
                                throw new FirstOfficer.Data.Exceptions.MissingEntityException(""{oneToOne.Type.Name} is required."");                             
                            }}");
                }

                sb.Append($@" if (saveChildren && entities.Any(a => ((a.{oneToOne.Type.Name}?.Id) ?? 0) != 0 && a.{oneToOne.Type.Name}Id != a.{oneToOne.Type.Name}!.Id))
                                {{
                                    throw new FirstOfficer.Data.Exceptions.ForeignIdMismatchException(""{name} {oneToOne.Type.Name}Id does not match {oneToOne.Type.Name} Id."");
                                }}");
            }
            foreach (var many in oneToMany)
            {
                sb.Append($@"

                                //one-to-many
                                if (saveChildren && entities.All(b=> b.{new Pluralizer().Pluralize(((INamedTypeSymbol)many.Type).TypeArguments[0].Name)}.Any(c=> c.{name}Id != b.Id)))
                                {{
                                    throw new FirstOfficer.Data.Exceptions.ForeignIdMismatchException(""{name} Id does not match {new Pluralizer().Pluralize(((INamedTypeSymbol)many.Type).TypeArguments[0].Name)}.{name} Id."");
                                }}");
            }

            sb.Append($@"     }} ");


            return sb.ToString();
        }
    }
}
