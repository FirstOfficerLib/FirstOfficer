using FirstOfficer.Generator.Extensions;
using Pluralize.NET;

namespace FirstOfficer.Generator.Helpers
{
    public static class DataHelper
    {
        public static string GetTableName(string name)
        {
            var tableName = name;
            var namePieces = tableName.ToSnakeCase().Split('_');
            var lastIndex = namePieces.Length - 1;
            namePieces[lastIndex] = new Pluralizer().Pluralize(namePieces[lastIndex]);
            return string.Join("_", namePieces);
        }
        public static string GetDbType(Type type, int size = 255)
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
    }
}
