using System.Text;
using System.Xml.Linq;
using FirstOfficer.Generator.Extensions;
using FirstOfficer.Generator.Helpers;
using FirstOfficer.Generator.Services;
using Microsoft.CodeAnalysis;
using Pluralize.NET;

namespace FirstOfficer.Generator.Syntax.Templates
{
    internal static class DatabaseQuery
    {

        internal static string GetTemplate(INamedTypeSymbol symbol)
        {
            var sb = new StringBuilder();
            var columnProperties = new List<string>() { $"{DataHelper.GetTableName(symbol.Name)}.id as {DataHelper.GetTableName(symbol.Name)}_id" };
            columnProperties.AddRange(OrmSymbolService.GetMappedProperties(symbol)
                .Select(a => $"{DataHelper.GetTableName(symbol.Name)}.{a.Name.ToSnakeCase()} as {DataHelper.GetTableName(symbol.Name)}_{a.Name.ToSnakeCase()}").ToList());
            var flagProperties = OrmSymbolService.GetFlagProperties(symbol);

            var name = symbol.Name;
            sb.Append($@"
                public static async Task<IEnumerable<{symbol.FullName()}>> Query{new Pluralizer().Pluralize(symbol.Name)}(this IDbConnection dbConnection,
                        System.Linq.Expressions.Expression<Func<{symbol.Name}Queryable, bool>> expression = null,
                        FirstOfficer.Data.Query.ParameterValues values = null,
                        Includes? includes = null,
                        {symbol.Name}OrderBy[] orderBy = null,
                        long? startIndex = null,
                        long? recordCount = null)
             {{
                 
            var sql = GetSql(includes ?? Includes.None, expression, orderBy, startIndex, recordCount);

            using var command = new NpgsqlCommand(sql, dbConnection as NpgsqlConnection);
            
            if(values is not null)
            {{

            if (values.Value1 != null)
            {{
                command.Parameters.AddWithValue(""@value1"", values.Value1);
            }}

            if (values.Value2 != null)
            {{
                command.Parameters.AddWithValue(""@value2"", values.Value2);
            }}

            if (values.Value3 != null)
            {{
                command.Parameters.AddWithValue(""@value3"", values.Value3);
            }}

            if (values.Value4 != null)
            {{
                command.Parameters.AddWithValue(""@value4"", values.Value4);
            }}

            if (values.Value5 != null)
            {{
                command.Parameters.AddWithValue(""@value5"", values.Value5);
            }}

            if (values.Value6 != null)
            {{
                command.Parameters.AddWithValue(""@value6"", values.Value6);
            }}

            if (values.Value7 != null)
            {{
                command.Parameters.AddWithValue(""@value7"", values.Value7);
            }}

            if (values.Value8 != null)
            {{
                command.Parameters.AddWithValue(""@value8"", values.Value8);
            }}

            if (values.Value9 != null)
            {{
                command.Parameters.AddWithValue(""@value9"", values.Value9);
            }}

            if (values.Value10 != null)
            {{
                command.Parameters.AddWithValue(""@value10"", values.Value10);
            }}
            }}

            using var reader = await command.ExecuteReaderAsync();
            return await {symbol.Name}Mapper(reader);
             }}

  
               private static string GetSql(Includes includes, System.Linq.Expressions.Expression<Func<{symbol.Name}Queryable, bool>> expression,
            {symbol.Name}OrderBy[] orderBys, long? startIndex, long? recordCount)          
             {{
                var joins = string.Empty;
               
                var cols = new List<string>() {{ {string.Join(", ", columnProperties.Select(a => $@"""{a}"""))} }};
");
            var manyToManyProps = OrmSymbolService.GetManyToManyProperties(symbol);

            foreach (var propertySymbol in flagProperties)
            {
                sb.AppendLine($"if ((includes & Includes.{propertySymbol.Name}) == Includes.{propertySymbol.Name})");
                sb.AppendLine("{");

                //many-to-many
                AddManyToManyLogic(symbol, manyToManyProps, propertySymbol, sb);

                //one-to-one
                if (OrmSymbolService.IsEntity(propertySymbol.Type))
                {
                    var moreCols = new List<string>();
                    var propName = new Pluralizer().Singularize(propertySymbol.Name);
                    moreCols.Add($"{DataHelper.GetTableName(propName)}.id as {DataHelper.GetTableName(propName)}_id ");
                    moreCols.AddRange(OrmSymbolService.GetMappedProperties((INamedTypeSymbol)propertySymbol.Type)
                        .Select(a => $"{DataHelper.GetTableName(propName)}.{a.Name.ToSnakeCase()} as {DataHelper.GetTableName(propName)}_{a.Name.ToSnakeCase()} "));


                    sb.AppendLine($"cols.AddRange(new[]{{ {string.Join(",", moreCols.Select(a => $@"""{a}"""))} }});");
                    sb.AppendLine($@"joins += "" LEFT OUTER JOIN {DataHelper.GetTableName(propertySymbol.Type.Name)} ON {DataHelper.GetTableName(symbol.Name)}.{(propName + "Id").ToSnakeCase()} = {DataHelper.GetTableName(propertySymbol.Type.Name)}.Id "";");
                }

                sb.AppendLine("}");
            }

            sb.AppendLine($@"           
            var whereClause = string.Empty;
            if (expression != null)
            {{
                var whereKey = FirstOfficer.Data.Query.Helper.GetExpressionKey($""Query{new Pluralizer().Pluralize(name)}-{{expression.ToString()}}"");

                if (!new FirstOfficer.Data.Query.SqlParts().WhereClauses.TryGetValue(whereKey, out whereClause))
                {{
                    throw new ApplicationException(""Where clause not found"");
                }}
            }} 

            var offset = string.Empty;
            if (startIndex.HasValue)
            {{
                offset = $""OFFSET {{startIndex.Value}} "";
            }}

            var limit = string.Empty;
            if (recordCount.HasValue)
            {{
                limit += $""LIMIT {{recordCount.Value}} "";
            }}

            var orderBy = string.Empty;
            if (orderBys != null && orderBys.Any())
            {{
                var orderByBuilder = new StringBuilder(""ORDER BY "");
                foreach (var orderByItem in orderBys)
                {{
                    switch (orderByItem)
                    {{ ");

            var tableName = DataHelper.GetTableName(symbol.Name);
            //get order by properties
            var props = OrmSymbolService.GetOrderByProperties(symbol);

            foreach (var prop in props)
            {
                sb.AppendLine($@"case {symbol.Name}OrderBy.{prop.Name}Asc:
                            orderByBuilder.Append(""{tableName}.{prop.Name.ToSnakeCase()} ASC, "");
                            break;
                        case {symbol.Name}OrderBy.{prop.Name}Desc:
                            orderByBuilder.Append(""{tableName}.{prop.Name.ToSnakeCase()} DESC, "");
                            break;");

            }

            sb.AppendLine($@"
                }}
             }}

                orderBy = orderByBuilder.ToString().TrimEnd(',', ' ');
            }}
");
            sb.AppendLine($@"var sql = $""SELECT {{string.Join("", "", cols) }} FROM {DataHelper.GetTableName(name)} {{joins}} {{whereClause}} {{orderBy}} {{offset}} {{limit}};"";");
            sb.AppendLine("return sql;");

            sb.AppendLine("}");

            return sb.ToString();
        }

        private static void AddManyToManyLogic(INamedTypeSymbol symbol, IPropertySymbol[] manyToManyProps,
            IPropertySymbol propertySymbol, StringBuilder sb)
        {
            var manyToManyProp = manyToManyProps.FirstOrDefault(a => a.Name == propertySymbol.Name);
            if (manyToManyProp == null)
            {
                return;
            }

            var manyToMany = DataHelper.GetManyToMany(symbol).FirstOrDefault(a => a.Value.Item1.Name == propertySymbol.Name || a.Value.Item2.Name == propertySymbol.Name);

            string manyToManyTableName = manyToMany.Key;

            string tableAlias = $"{propertySymbol.Name.ToSnakeCase()}";

            var childType = ((INamedTypeSymbol)manyToManyProp.Type).TypeArguments.First();

            var childCols = new List<string> { $@"""{tableAlias}.id as {tableAlias}_id """ };

            foreach (var prop in OrmSymbolService.GetMappedProperties((INamedTypeSymbol)childType))
            {
                childCols.Add(
                    $@"""{tableAlias}.{prop.Name.ToSnakeCase()} as {tableAlias}_{prop.Name.ToSnakeCase()} """);
            }

 

            sb.AppendLine($"cols.AddRange(new[] {{ {string.Join(", ", childCols)} }});");

            var parentName = manyToMany.Value.Item1.Name;
            var childName = manyToMany.Value.Item2.Name;

            if (((INamedTypeSymbol)((INamedTypeSymbol)manyToMany.Value.Item2.Type).TypeArguments[0]).FullName() == symbol.FullName())
            {
                parentName = manyToMany.Value.Item2.Name;
                childName = manyToMany.Value.Item1.Name;
            }

            var parentColumnIdName = Data.DataHelper.GetIdColumnName(parentName);
            var childColumnIdName = Data.DataHelper.GetIdColumnName(childName);
            childColumnIdName = Data.DataHelper.GetIdColumnName(childName, parentColumnIdName == childColumnIdName); // handle many-to-many with self

            sb.AppendLine(
                $" joins += \" LEFT OUTER JOIN {manyToManyTableName} ON {DataHelper.GetTableName(symbol.Name)}.id = {manyToManyTableName}.{parentColumnIdName} \"; ");
            sb.AppendLine(
                $" joins += \" LEFT OUTER JOIN {DataHelper.GetTableName(childType.Name)} {tableAlias} ON {manyToManyTableName}.{childColumnIdName} = {tableAlias}.id  \"; ");

        }
    }
}
