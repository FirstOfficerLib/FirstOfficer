using System.Collections;
using System.Data;
using System.Reflection;
using System.Text;
using FirstOfficer.Data.Attributes;
using FirstOfficer.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FirstOfficer.Data.Npgsql
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

            }

            AddForeignKeys(entityTypes);

            AddManyToManyTables(entityTypes);

            AddIndexes(entityTypes);

            _logger.LogInformation("PostgresDatabaseBuilder Finished");
        }

        private void AddIndexes(List<Type> entityTypes)
        {
            foreach (var entityType in entityTypes)
            {
                var tableName = DataHelper.GetTableName(entityType);
                var props = entityType.GetProperties().Where(a => (a.Name != "Id" && a.Name.EndsWith("Id")) ||
                                                                  a.GetCustomAttribute<QueryableAttribute>() != null ||
                                                                  a.GetCustomAttribute<OrderByAttribute>() != null).ToList();
                foreach (var prop in props)
                {
                    var colName = DataHelper.GetColumnName(prop);
                    var indexName = $"ix_{tableName}_{colName}";
                    var sql = $"SELECT indexname FROM pg_indexes WHERE tablename = '{tableName}' AND indexname = '{indexName}';";
                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = sql;

                        if (command.ExecuteScalar() != null)
                        {
                            continue;
                        }

                        sql = $"CREATE INDEX {indexName} ON {tableName} ({colName});";
                        _connection.Execute(sql);
                    }
                }
            }
        }

        private void AddManyToManyTables(List<Type> entityTypes)
        {
            var manyToMany = GetManyToMany(entityTypes);

            foreach (var props in manyToMany)
            {
                if (GetColumnInfo(props.Key).Any())
                {
                    continue;
                }

                var colName1 = DataHelper.GetIdColumnName(props.Value.Item1);
                var colName2 = DataHelper.GetIdColumnName(props.Value.Item2);

                colName2 = DataHelper.GetIdColumnName(props.Value.Item2, colName1 == colName2);  // handle case where both sides of many to many are the same type

                CreateManyTable(props.Key, colName1, colName2);
                AddManyForeignKey(props.Value.Item1, props.Key);
                AddManyForeignKey(props.Value.Item2, props.Key);
            }
        }


        private void CreateManyTable(string tableName, string column1, string column2)
        {
            _logger.LogInformation($"Creating many to many table {tableName}");
            var sql = $@"CREATE TABLE {tableName} (
                    {column1} BIGINT NOT NULL,
                    {column2} BIGINT NOT NULL,
                    PRIMARY KEY ({column1}, {column2})
                    );";
            _connection.Execute(sql);
        }

        private Dictionary<string, (PropertyInfo, PropertyInfo)> GetManyToMany(List<Type> entityTypes)
        {
            var rtn = new Dictionary<string, (PropertyInfo, PropertyInfo)>();

            foreach (var type1 in entityTypes)
            {
                foreach (var type2 in entityTypes)
                {
                    var props = type1.GetProperties().Where(a =>
                        a.PropertyType.GenericTypeArguments.Any() && a.PropertyType.GenericTypeArguments[0] == type2).ToList();

                    foreach (var prop1 in props)
                    {
                        var prop2 = type2.GetProperties().FirstOrDefault(a =>
                            a.PropertyType.GenericTypeArguments.Any() && a.PropertyType.GenericTypeArguments[0] == type1);
                        if (prop2 == null)
                        {
                            continue;
                        }

                        var name = DataHelper.GetManyToManyTableName(type1.Name, prop1.Name, type2.Name, prop2.Name);

                        if (!rtn.ContainsKey(name))
                        {
                            rtn.Add(name, (prop1, prop2));
                        }
                    }
                }

            }

            return rtn;
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
            using (var command = _connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT count(1) FROM information_schema.columns WHERE table_schema = 'public' AND table_name = '_first_officer';";
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
        }

        private void AddManyForeignKey(PropertyInfo propertyInfo, string tableName)
        {
            var entityType = propertyInfo.PropertyType.GenericTypeArguments[0];
            var colName = DataHelper.GetIdColumnName(propertyInfo);
            var fkTableName = DataHelper.GetTableName(entityType);
            string fkName = $"fk_{tableName}_{fkTableName}_{colName}";

            WriteFk(fkName, tableName, colName, fkTableName);

        }

        private void AddForeignKeys(List<Type> entityTypes)
        {
            foreach (var entityType in entityTypes)
            {
                var tableName = DataHelper.GetTableName(entityType);
                var allTables = entityTypes.Select(a => a.Name).ToList();
                var props = entityType.GetProperties().ToList();
                foreach (var prop in props.Where(p =>
                             p.Name.EndsWith("Id") && allTables.Contains(p.Name.Substring(0, p.Name.Length - 2))))
                {
                    //add foreign key
                    var fkTableName =
                        DataHelper.GetTableName(entityTypes.First(a =>
                            a.Name == prop.Name.Substring(0, prop.Name.Length - 2)));
                    var colName = DataHelper.GetColumnName(prop);
                    string fkName = $"fk_{tableName}_{fkTableName}_{colName}";
                    WriteFk(fkName, tableName, colName, fkTableName);
                }
            }
        }

        private void WriteFk(string fkName, string tableName, string colName, string fkTableName)
        {
            var sql = $"SELECT conname AS constraint_name FROM pg_constraint WHERE conname = '{fkName}';";
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = sql;

                if (command.ExecuteScalar() != null)
                {
                    return;
                }

                sql = $@"ALTER TABLE {tableName}
                                ADD CONSTRAINT {fkName}
                                FOREIGN KEY ({colName}) REFERENCES {fkTableName}(id) ON DELETE CASCADE;";
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

        private void CreateTable(string tableName)
        {
            _logger.LogInformation($"Creating table {tableName}");
            var sql = $"CREATE TABLE {tableName}(id BIGSERIAL PRIMARY KEY);";
            _connection.Execute(sql);
        }


        private List<ColumnInfo> GetColumnInfo(Type type)
        {
            var tableName = DataHelper.GetTableName(type);
            return GetColumnInfo(tableName);
        }
        private List<ColumnInfo> GetColumnInfo(string tableName)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT column_name AS Name, data_type AS DataType, character_maximum_length AS MaxLength, is_nullable AS IsNullable
                      FROM information_schema.columns
                      WHERE table_schema = 'public' AND table_name = @TableName";
                command.Parameters.Add(new NpgsqlParameter("@TableName", DbType.String) { Value = tableName });

                using (var reader = command.ExecuteReader())
                {
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
        }
    }
    public class ColumnInfo
    {
        public ColumnInfo(string name,
            string dataType,
            int? maxLength,
            string isNullable)

        {
            Name = name;
            DataType = dataType;
            MaxLength = maxLength;
            IsNullable = isNullable;
        }


        public string Name { get; set; }
        public string DataType { get; set; }
        public int? MaxLength { get; set; }
        public string IsNullable { get; set; }


    }
}
