using FirstOfficer.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Pluralize.NET;
using System.Reflection;

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
        internal static string GetDbType(Type type, int size = 255)
        {
            if (type.ToString() == typeof(DateTime).ToString())
                return "timestamp  NOT NULL  DEFAULT ('1900-01-01') ";
            if (type.ToString() == typeof(int).ToString())
                return "INT NOT NULL  DEFAULT (0) ";
            if (type.ToString() == typeof(long).ToString())
                return "BIGINT NOT NULL  DEFAULT (0) ";
            if (type.ToString() == typeof(bool).ToString())
                return "BOOL NOT NULL DEFAULT ('f') ";
            if (type.ToString() == typeof(decimal).ToString())
                return "decimal(38,15) NOT NULL  DEFAULT (0) ";
            if (type.ToString() == typeof(DateTime?).ToString())
                return "timestamp  NULL ";
            if (type.ToString() == typeof(int?).ToString())
                return "INT NULL ";
            if (type.ToString() == typeof(long?).ToString())
                return "BIGINT NULL ";
            if (type.ToString() == typeof(bool?).ToString())
                return "BOOL NULL ";
            if (type.ToString() == typeof(Guid).ToString())
                return "UUID NOT NULL ";
            if (type.ToString() == typeof(Guid?).ToString())
                return "UUID NULL ";
            if (type.ToString() == typeof(decimal?).ToString())
                return "decimal(38,15) NULL ";
            if (size == 0)
                return "TEXT NULL ";
            return $"VARCHAR({size}) NULL ";
        }

        internal static string GetManyToManyTableName(string type1Name, string prop1Name, string type2Name, string prop2Name)
        {
            var names = new List<string>() { $"{type1Name.ToSnakeCase()}_{prop1Name.ToSnakeCase()}", $"{type2Name.ToSnakeCase()}_{prop2Name.ToSnakeCase()}" }
                .OrderBy(a => a)
                .ToArray();

            var name = $"many_to_many_{names.First()}__{names.Last()}";
            return name;
        }

        internal static Dictionary<string, (IPropertySymbol, IPropertySymbol)> GetManyToMany(INamedTypeSymbol entityType)
        {
            var rtn = new Dictionary<string, (IPropertySymbol, IPropertySymbol)>();

            var props = CodeAnalysisHelper.GetManyToManyProperties(entityType);

            foreach (var prop1 in props)
            {
                if ((prop1.Type as INamedTypeSymbol)?.TypeArguments[0] is not INamedTypeSymbol type1)
                {
                    continue;
                }

                var prop2 = CodeAnalysisHelper.GetManyToManyProperties(type1).FirstOrDefault(a => 
                        a.Type is INamedTypeSymbol symbol &&
                        SymbolEqualityComparer.Default.Equals(symbol.TypeArguments[0],entityType));
                if (prop2 == null)
                {
                    continue;
                }

                var orderedProps = new List<IPropertySymbol>() { prop1, prop2 }.OrderBy(a => a.Name).ToArray();
                var typeName1 = ((INamedTypeSymbol)orderedProps.First().Type).TypeArguments[0].Name;
                var typeName2 = ((INamedTypeSymbol)orderedProps.Last().Type).TypeArguments[0].Name;
                
                var name = GetManyToManyTableName(typeName1, prop2.Name,typeName2, prop1.Name);
                
                if (!rtn.ContainsKey(name))
                {
                    rtn.Add(name, (orderedProps.First(), orderedProps.Last()));
                }
            }




            return rtn;
        }


    }
}
