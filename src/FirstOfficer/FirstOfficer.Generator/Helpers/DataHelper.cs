using FirstOfficer.Generator.Extensions;
using FirstOfficer.Generator.Services;
using Microsoft.CodeAnalysis;
using Pluralize.NET;

namespace FirstOfficer.Generator.Helpers
{
    internal static class DataHelper
    {
        internal static string GetTableName(string name)
        {
            var tableName = name;
            var namePieces = tableName.ToSnakeCase().Split('_');
            var lastIndex = namePieces.Length - 1;
            namePieces[lastIndex] = new Pluralizer().Pluralize(namePieces[lastIndex]);
            return string.Join("_", namePieces);
        }
        internal static Dictionary<string, (IPropertySymbol, IPropertySymbol)> GetManyToMany(INamedTypeSymbol entityType)
        {
            var rtn = new Dictionary<string, (IPropertySymbol, IPropertySymbol)>();

            var props = OrmSymbolService.GetManyToManyProperties(entityType);

            foreach (var prop1 in props)
            {
                if ((prop1.Type as INamedTypeSymbol)?.TypeArguments[0] is not INamedTypeSymbol type1)
                {
                    continue;
                }

                var prop2 = OrmSymbolService.GetManyToManyProperties(type1).FirstOrDefault(a => 
                        a.Type is INamedTypeSymbol symbol &&
                        SymbolEqualityComparer.Default.Equals(symbol.TypeArguments[0],entityType));
                if (prop2 == null)
                {
                    continue;
                }

                var orderedProps = new List<IPropertySymbol>() { prop1, prop2 }.OrderBy(a => a.Name).ToArray();
                var typeName1 = ((INamedTypeSymbol)orderedProps.First().Type).TypeArguments[0].Name;
                var typeName2 = ((INamedTypeSymbol)orderedProps.Last().Type).TypeArguments[0].Name;
                
                var name = Data.DataHelper.GetManyToManyTableName(typeName1, orderedProps.Last().Name, typeName2, orderedProps.First().Name);
                
                if (!rtn.ContainsKey(name))
                {
                    rtn.Add(name, (orderedProps.First(), orderedProps.Last()));
                }
            }




            return rtn;
        }


    }
}
