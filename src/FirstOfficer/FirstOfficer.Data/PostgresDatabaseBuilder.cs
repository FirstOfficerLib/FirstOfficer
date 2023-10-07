using System.Collections;
using System.Data;
using System.Reflection;
using System.Text;
using FirstOfficer.Core.Extensions;
using FirstOfficer.Data.Attributes;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using Npgsql;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace FirstOfficer.Data
{
    public class PostgresDatabaseBuilder : IDatabaseBuilder
    {

        private readonly ILogger<PostgresDatabaseBuilder> _logger;
        private readonly NpgsqlConnection _connection;

        public PostgresDatabaseBuilder(ILogger<PostgresDatabaseBuilder> logger, IDbConnection connection)
        {
            _logger = logger;
            _connection = (connection as NpgsqlConnection) ?? new NpgsqlConnection();
            _connection.Open();
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        public void BuildDatabase()
        {
            _logger.LogInformation("PostgresDatabaseBuilder Started");

            var entityTypes = DataHelper.GetEntities();

            if (CheckChecksum(entityTypes))
            {
                _logger.LogInformation("PostgresDatabaseBuilder Finished with Checksum match");
                return;
            }

            foreach (var entityType in entityTypes)
            {
                var tableName = DataHelper.GetTableName(entityType);

                var cols = GetColumnInfo(entityType);

                if (!cols.Any())
                {
                    CreateTable(tableName);
                }

                var colNames = cols.Select(a => (a.Name ?? string.Empty).ToSnakeCase()).ToArray();
                foreach (var pi in GetDataProperties(entityType).Where(a => colNames.All(b => b != a.Name.ToSnakeCase())))
                {
                    var colName = pi.Name.ToSnakeCase();
                    if (colName == "id")
                    {
                        continue;
                    }

                    AddColumn(tableName, pi);
                }
                AddForeignKey(entityType, entityTypes);
            }

            _logger.LogInformation("PostgresDatabaseBuilder Finished");
        }

        private string GetChecksum(List<Type> entityTypes)
        {
            var sb = new StringBuilder();
            foreach (var entityType in entityTypes.OrderBy(a => a.FullName))
            {
                sb.Append(entityType.FullName);
                foreach (var pi in GetDataProperties(entityType))
                {
                    sb.Append(pi.Name);
                    sb.Append(pi.PropertyType.FullName);
                }
            }

            byte[] hashBytes = System.Security.Cryptography.SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return BitConverter.ToString(hashBytes).Replace(" - ", "");
        }

        private static IEnumerable<PropertyInfo> GetDataProperties(Type entityType)
        {
            return entityType.GetProperties().Where(a => a.CanWrite
                                                         && !a.PropertyType.GetInterfaces().Contains(typeof(IEntity))
                                                         && !a.PropertyType.GetInterfaces().Contains(typeof(IList))
                                                         && !a.PropertyType.GetInterfaces().Contains(typeof(ICollection))
                                                         && !a.PropertyType.IsAbstract);
        }

        private bool CheckChecksum(List<Type> entityTypes)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = @"SELECT count(1) FROM information_schema.columns WHERE table_schema = 'public' AND table_name = '_first_officer';";
            var count = command.ExecuteScalar();
            if (count != null && (long)count > 0)
            {
                var checksum = GetChecksum(entityTypes);

                command.CommandText = "SELECT checksum FROM _first_officer;";

                var dbChecksum = command.ExecuteScalar();
                if (dbChecksum != null && checksum == dbChecksum.ToString())
                {
                    return true;
                }

                return false;
            }

            command.CommandText = "CREATE TABLE _first_officer (checksum varchar(64));";
            command.ExecuteNonQuery();
            return false;
        }

        private void AddForeignKey(Type entityType, List<Type> entityTypes)
        {
            var tableName = DataHelper.GetTableName(entityType);
            var allTables = entityTypes.Select(a => a.Name).ToList();
            var props = entityType.GetProperties().ToList();
            foreach (var prop in props.Where(p => p.Name.EndsWith("Id") && allTables.Contains(p.Name.Substring(0, p.Name.Length - 2))))
            {
                //add foreign key
                var fkTableName = DataHelper.GetTableName(entityTypes.First(a => a.Name == prop.Name.Substring(0, prop.Name.Length - 2)));
                var colName = DataHelper.GetColumnName(prop);
                string fkName = $"fk_{tableName}_{fkTableName}_{colName}";

                var sql = $"SELECT conname AS constraint_name FROM pg_constraint WHERE conname = '{fkName}';";
                using var command = _connection.CreateCommand();
                command.CommandText = sql;

                if (command.ExecuteScalar() != null)
                {
                    continue;
                }

                sql = $@"ALTER TABLE {tableName}
                                ADD CONSTRAINT {fkName}
                                FOREIGN KEY ({colName}) REFERENCES {fkTableName}(id);";
                _connection.Execute(sql);
            }

        }

        private void AddColumn(string tableName, PropertyInfo pi)
        {

            var colName = DataHelper.GetColumnName(pi);
            int? textSize = null;

            foreach (var textSizeAttribute in Attribute.GetCustomAttributes(pi, typeof(TextSizeAttribute)))
            {
                textSize = ((TextSizeAttribute)textSizeAttribute).Size;
            }

            var dataType = DataHelper.GetDbType(pi, textSize ?? 255);

            _logger.LogInformation($"Adding Column {tableName}.{colName} {dataType}");

            var sql = $"ALTER TABLE {tableName} ADD COLUMN {colName} {dataType};";
            _connection.Execute(sql);
        }

        private void CreateTable(string? tableName)
        {
            _logger.LogInformation($"Creating table {tableName}");
            var sql = $"CREATE TABLE {tableName}(id BIGSERIAL PRIMARY KEY);";
            _connection.Execute(sql);
        }


        private List<ColumnInfo> GetColumnInfo(Type type)
        {
            var tableName = DataHelper.GetTableName(type);

            using var command = _connection.CreateCommand();
            command.CommandText = @"SELECT column_name AS Name, data_type AS DataType, character_maximum_length AS MaxLength, is_nullable AS IsNullable
                      FROM information_schema.columns
                      WHERE table_schema = 'public' AND table_name = @TableName";
            command.Parameters.Add(new NpgsqlParameter("@TableName", DbType.String) { Value = tableName });

            using var reader = command.ExecuteReader();
            var columns = new List<ColumnInfo>();
            while (reader.Read())
            {
                columns.Add(new ColumnInfo(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.IsDBNull(2) ? null : (int?)reader.GetInt32(2),
                    reader.GetString(3)
                ));
            }

            return columns;
        }
    }
    public class ColumnInfo
    {
        public ColumnInfo(string? name,
            string? dataType,
            int? maxLength,
            string? isNullable)

        {
            Name = name;
            DataType = dataType;
            MaxLength = maxLength;
            IsNullable = isNullable;
        }


        public string? Name { get; set; }
        public string? DataType { get; set; }
        public int? MaxLength { get; set; }
        public string? IsNullable { get; set; }


    }
}
