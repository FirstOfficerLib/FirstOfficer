using System.Reflection;
using FirstOfficer.Extensions;
using Microsoft.CodeAnalysis;
using Pluralize.NET;

namespace FirstOfficer.Data
{
    public static class DataHelper
    {
        public static List<Type> GetEntities()
        {
            var rtn = new List<Type>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (Type type in assembly.GetTypes().Where(t => !t.IsAbstract))
                    {
                        if (typeof(IEntity).IsAssignableFrom(type))
                        {
                            rtn.Add(type);
                        }
                    }
                }
                catch (Exception)
                {
                    //eat it
                }
            }

            return rtn;
        }

        public static string GetIdColumnName(PropertyInfo propertyInfo, bool isTwo = false)
        {
            return GetIdColumnName(propertyInfo.Name, isTwo);
        }

        public static string GetIdColumnName(string propertyName, bool isTwo = false)
        {
            if (isTwo)
            {
                return $"{new Pluralizer().Singularize(propertyName)}_2_Id".ToSnakeCase();
            }

            return $"{new Pluralizer().Singularize(propertyName)}Id".ToSnakeCase();
        }

        public static string GetTableName<T>()
        {
            return GetTableName(typeof(T));
        }

        public static string GetTableName(Type type)
        {
            return GetTableName(type.Name);
        }

        internal static string GetTableName(string name)
        {
            var tableName = name;
            var namePieces = tableName.ToSnakeCase().Split('_');
            var lastIndex = namePieces.Length - 1;
            namePieces[lastIndex] = new Pluralizer().Pluralize(namePieces[lastIndex]);
            return string.Join("_", namePieces);
        }


        public static string GetManyToManyTableName(string type1Name, string prop1Name, string type2Name, string prop2Name)
        {
            var names = new List<string>() { $"{type1Name.ToSnakeCase()}_{prop1Name.ToSnakeCase()}", $"{type2Name.ToSnakeCase()}_{prop2Name.ToSnakeCase()}" }
                .OrderBy(a => a)
                .ToArray();

            var name = $"many_to_many_{names.First()}__{names.Last()}";
            return name;
        }

        public static string GetDbType(IPropertySymbol propertySymbol, int size = 255)
        {
            var fullName = propertySymbol.Type.ToDisplayString(new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));

            if (fullName == typeof(DateTime).FullName)
                return "timestamp  NOT NULL DEFAULT ('1900-01-01') ";
            if (fullName == typeof(int).FullName)
                return "INT NOT NULL  DEFAULT (0) ";
            if (fullName == typeof(long).FullName)
                return "BIGINT NOT NULL  DEFAULT (0) ";
            if (fullName == typeof(bool).FullName)
                return "BOOL NOT NULL DEFAULT ('f') ";
            if (fullName == typeof(decimal).FullName)
                return "decimal(38,15) NOT NULL  DEFAULT (0) ";
            if (fullName == typeof(DateTime?).FullName)
                return "timestamp  NULL ";
            if (fullName == $"{typeof(int).FullName}?")
                return "INT NULL ";
            if (fullName == $"{typeof(long).FullName}?")
                return "BIGINT NULL ";
            if (fullName == $"{typeof(bool).FullName}?")
                return "BOOL NULL ";
            if (fullName == typeof(Guid).FullName)
                return "UUID NOT NULL ";
            if (fullName == $"{typeof(Guid).FullName}?")
                return "UUID NULL ";
            if (fullName == $"{typeof(decimal).FullName}?")
                return "decimal(38,15) NULL ";
            if (size == 0)
                return "TEXT NULL ";
            return $"VARCHAR({size}) NULL ";
        }


        public static string GetColumnName(IPropertySymbol propertyInfo)
        {
            var columnName = propertyInfo.Name.ToSnakeCase();
            return columnName;

        }

    }
}
