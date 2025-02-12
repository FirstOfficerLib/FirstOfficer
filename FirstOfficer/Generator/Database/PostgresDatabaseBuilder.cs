using System.Collections;
using System.Collections.Immutable;
using System.Data;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using FirstOfficer.Data;
using FirstOfficer.Data.Attributes;
using FirstOfficer.Extensions;
using FirstOfficer.Generator.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FirstOfficer.Generator.Database
{
    public static class PostgresDatabaseBuilder
    {

        public static string GenerateSource(Microsoft.CodeAnalysis.Compilation comp, ImmutableArray<ClassDeclarationSyntax> classDeclarationSyntaxes)
        {
            var entityTypes = DataHelper.GetEntities();
            var declarations = new List<INamedTypeSymbol>();
            var sb = new StringBuilder();
            sb.AppendLine("BEGIN;");
            foreach (var entityDeclarationSyntax in classDeclarationSyntaxes)
            {
                var entitiesDeclaration = comp.GetSemanticModel(entityDeclarationSyntax.SyntaxTree).GetDeclaredSymbol(entityDeclarationSyntax);
                if (entitiesDeclaration == null || !OrmSymbolService.IsEntity(entitiesDeclaration))
                {
                    continue;
                }

                declarations.Add(entitiesDeclaration);

                var className = entitiesDeclaration.Name;

                var tableName = DataHelper.GetTableName(className);

                var cols = GetColumnInfo(entitiesDeclaration);

                sb.AppendLine(CreateTable(tableName));
                cols.ForEach(a => sb.AppendLine(AddColumn(tableName, a)));
            }

            sb.AppendLine(AddManyToManyTables(declarations));

            sb.AppendLine(AddIndexes(declarations));

            sb.AppendLine(AddForeignKeys(declarations));

            sb.AppendLine("COMMIT;");

            return $@"

                using System;
                using System.Collections;
                using System.Data;
                using System.Reflection;
                using System.Text;
                using FirstOfficer.Data;
                using FirstOfficer.Extensions;
                using Microsoft.Extensions.Logging;
                using Npgsql;

                namespace FirstOfficer
                {{

                public class DatabaseBuilder : FirstOfficer.Data.IDatabaseBuilder                
                {{
                    private readonly IDbConnection _connection;
                    private readonly ILogger<DatabaseBuilder> _logger;

                    public DatabaseBuilder(IDbConnection connection, ILogger<DatabaseBuilder> logger)
                    {{
                        _connection = connection;
                        _logger = logger;
                        if (_connection.State != ConnectionState.Open)
                        {{
                            _connection.Open();
                        }}
                    }}

                    public void BuildDatabase()
                    {{
                        var sql = @""{sb.ToString()}"";
                        _connection.Execute(sql);
                    }}

                    public void Dispose()
                    {{
                        _connection?.Dispose();
                    }}
                }}
                }}

                ";
        }

        private static string AddForeignKeys(List<INamedTypeSymbol> symbols)
        {
            var rtn = new StringBuilder();

            foreach (var symbol in symbols)
            {
                var props = OrmSymbolService.GetOneToOneProperties(symbol).ToList();
                props.AddRange(OrmSymbolService.GetOneToManyProperties(symbol));

                foreach (var propertySymbol in props)
                {
                    var fkTableName = DataHelper.GetTableName(symbol.Name);
                    var colName = DataHelper.GetIdColumnName(symbol.Name);
                    var tableName = propertySymbol.Type is INamedTypeSymbol s && SymbolService.IsCollection(s) ?
                            DataHelper.GetTableName(s.TypeArguments.First().Name) :
                            DataHelper.GetTableName(propertySymbol.Type.Name); 
                    
                    string fkName = $"fk_{tableName}_{fkTableName}_{colName}";

                    rtn.AppendLine(WriteFk(fkName, tableName, colName, fkTableName));
                }
            }
            return rtn.ToString();
        }

        private static string AddIndexes(List<INamedTypeSymbol> symbols)
        {
            var rtn = new StringBuilder();

            foreach (var symbol in symbols)
            {
                var tableName = DataHelper.GetTableName(symbol.Name);
                var props = OrmSymbolService.GetQueryableProperties(symbol);

                foreach (var prop in props)
                {
                    var colName = DataHelper.GetColumnName(prop);
                    var indexName = $"ix_{tableName}_{colName}";


                    var sql = $@"DO $$
                        BEGIN
                            IF NOT EXISTS (
                                SELECT indexname FROM pg_indexes WHERE tablename = '{tableName}' AND indexname = '{indexName}'
                            ) THEN
                                CREATE INDEX {indexName} ON {tableName} ({colName});
                            END IF;
                        END
                        $$;";

                    rtn.AppendLine(sql);

                }
            }

            return rtn.ToString();
        }

        private static string AddManyToManyTables(List<INamedTypeSymbol> entityTypes)
        {
            var rtn = new StringBuilder();

            foreach (var symbol in entityTypes)
            {
                var manyToMany = OrmSymbolService.GetManyToManyProperties(symbol);
                foreach (var prop1 in manyToMany)
                {
                    var type1 = (INamedTypeSymbol)((INamedTypeSymbol)prop1.Type).TypeArguments.First();

                    var prop2 = OrmSymbolService.GetManyToManyProperties(type1)
                        .FirstOrDefault(a => a.Type is INamedTypeSymbol s &&
                                                              SymbolService.IsCollection(s) &&
                                                              s.TypeArguments.Count() == 1 &&
                                                              s.TypeArguments.All(c => SymbolEqualityComparer.Default.Equals(c, symbol)));

                    var type2 = (INamedTypeSymbol)((INamedTypeSymbol)prop2!.Type).TypeArguments.Last();

                    var colName1 = DataHelper.GetIdColumnName(prop1.Name);
                    var colName2 = DataHelper.GetIdColumnName(prop2.Name);
                    colName2 = DataHelper.GetIdColumnName(prop2.Name, colName1 == colName2);  // handle case where both sides of many to many are the same type
                    var tableName = DataHelper.GetManyToManyTableName(type1.Name, prop2.Name, type2.Name, prop1.Name);

                    rtn.AppendLine(CreateManyTable(tableName, colName1, colName2));
                    rtn.AppendLine(AddManyForeignKey(prop1, tableName, colName1));
                    rtn.AppendLine(AddManyForeignKey(prop2, tableName, colName2));
                }


            }

            return rtn.ToString();
        }



        private static string CreateManyTable(string tableName, string column1, string column2)
        {

            return $@"CREATE TABLE IF NOT EXISTS {tableName} (
                    {column1} BIGINT NOT NULL,
                    {column2} BIGINT NOT NULL,
                    PRIMARY KEY ({column1}, {column2})
                    );";

        }
        private static string AddManyForeignKey(IPropertySymbol propertySymbol, string tableName, string colName)
        {
            var entityType = (INamedTypeSymbol)((INamedTypeSymbol)propertySymbol.Type).TypeArguments.First();
            var fkTableName = DataHelper.GetTableName(entityType.Name);
            string fkName = $"fk_{tableName}_{fkTableName}_{colName}";

            return WriteFk(fkName, tableName, colName, fkTableName);

        }


        private static string WriteFk(string fkName, string tableName, string colName, string fkTableName)
        {
            var sql = $@"DO $$
                        BEGIN
                            IF NOT EXISTS (
                                SELECT conname AS constraint_name FROM pg_constraint WHERE conname = '{fkName}'
                            ) THEN
                                ALTER TABLE {tableName}
                                ADD CONSTRAINT {fkName}
                                FOREIGN KEY ({colName}) REFERENCES {fkTableName}(id) ON DELETE CASCADE;
                            END IF;
                        END
                        $$;";
            return sql;
        }

        private static string AddColumn(string tableName, ColumnInfo columnInfo)
        {
            var sql = $@"DO $$
                        BEGIN
                            IF NOT EXISTS (
                                SELECT FROM information_schema.columns 
                                WHERE table_schema = 'public' 
                                AND table_name = '{tableName}' 
                                AND column_name = '{columnInfo.Name}'
                            ) THEN
                                ALTER TABLE {tableName} ADD COLUMN {columnInfo.Name} {columnInfo.DataType};
                            END IF;
                        END
                        $$;";
            return sql;
        }

        private static string CreateTable(string tableName)
        {
            return $"CREATE TABLE IF NOT EXISTS {tableName}(id BIGSERIAL PRIMARY KEY);";
        }

        private static List<ColumnInfo> GetColumnInfo(INamedTypeSymbol namedTypeSymbol)
        {
            var props = OrmSymbolService.GetMappedProperties(namedTypeSymbol);

            var rtn = new List<ColumnInfo>();

            foreach (var prop in props)
            {
                var colName = DataHelper.GetColumnName(prop);
                int? textSize = null;

                foreach (var textSizeAttribute in prop.GetAttributes().Where(a => a.AttributeClass?.Name == "TextSizeAttribute"))
                {
                    textSize = textSizeAttribute.ConstructorArguments[0].Value as int?;
                }

                var dataType = DataHelper.GetDbType(prop, textSize ?? 255);
                rtn.Add(new ColumnInfo(colName, dataType));
            }
            return rtn;
        }
    }

    public class ColumnInfo
    {
        public ColumnInfo(string name, string dataType)
        {
            Name = name;
            DataType = dataType;
        }
        public string Name { get; set; }
        public string DataType { get; set; }

    }
}
