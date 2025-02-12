using System.Text;
using FirstOfficer.Extensions;
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
            var oneToManyAsChild = OrmSymbolService.GetOneToManyAsChildProperties(entitySymbol).ToArray();
            var properties = OrmSymbolService.GetMappedProperties(entitySymbol).ToArray();
            var name = entitySymbol.Name;
            sb.Append($@"           private static void ValidateChildren(IEnumerable<{entitySymbol.FullName()}> entities, bool saveChildren)
        {{

            ");

            foreach (var oneToManyChild in oneToManyAsChild.Where(a=> !SymbolService.IsNullable(a.Type)))
            {
                sb.Append($@"
                            //one-to-many as child
                            if (entities.Any(a => a.{oneToManyChild.Name}Id == 0 && a.{oneToManyChild.Name} is null))
                            {{
                                throw new FirstOfficer.Data.Exceptions.MissingEntityException(""{oneToManyChild.Name} is required."");
                            }}");
            }
            
            foreach (var oneToOne in oneToOnes)
            {
                if (properties.Any(a =>
                        a.Name == $"{oneToOne.Name}Id" &&
                        ((INamedTypeSymbol)a.Type).FullName() == typeof(long).FullName))
                {
                    sb.Append($@"
                            //one-to-one
                            if (entities.Any(a => a.{oneToOne.Name} == null && a.{new Pluralizer().Singularize(oneToOne.Name)}Id == 0))
                            {{
                                throw new FirstOfficer.Data.Exceptions.MissingEntityException(""{oneToOne.Name} is required."");                             
                            }}");
                }

                sb.Append($@" if (saveChildren && entities.Any(a => ((a.{oneToOne.Name}?.Id) ?? 0) != 0 && a.{new Pluralizer().Singularize(oneToOne.Name)}Id != a.{oneToOne.Name}!.Id))
                                {{
                                    throw new FirstOfficer.Data.Exceptions.ForeignIdMismatchException(""{name} {new Pluralizer().Singularize(oneToOne.Name)}Id does not match {oneToOne.Name} Id."");
                                }}");
            }
            foreach (var many in oneToMany)
            {
                sb.Append($@"

                                //one-to-many
                                if (saveChildren && entities.All(b=> b.{many.Name}.Any(c=> c.{name}Id != b.Id)))
                                {{
                                    throw new FirstOfficer.Data.Exceptions.ForeignIdMismatchException(""{name} Id does not match {new Pluralizer().Pluralize(((INamedTypeSymbol)many.Type).TypeArguments[0].Name)}.{name} Id."");
                                }}");
            }

            sb.Append($@"     }} ");


            return sb.ToString();
        }
    }
}
