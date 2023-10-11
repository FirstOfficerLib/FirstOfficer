using System.Data;
using System.Data.Common;
using System.Text;
using FirstOfficer.Data.Query;
using FirstOfficer.Data.Query.Contracts;
using FirstOfficer.Data.Query.Npgsql;
using FirstOfficer.Tests.Generator.Models;

namespace FirstOfficer.Tests.Generator
{
    public class BookQueryable : QueryBuilder
    {
        private readonly DbConnection _dbConnection;
        private readonly IDbTransaction _transaction;
        private readonly IQueryBuilder _queryBuilder;

        protected readonly StringBuilder WhereBuilder = new();

        public BookQueryable(DbConnection dbConnection, IDbTransaction transaction, IQueryBuilder queryBuilder)
        {
            _dbConnection = dbConnection;
            _transaction = transaction;
            _queryBuilder = queryBuilder;
            throw new NotImplementedException();
        }

        public bool Where(Func<BookQueryable, bool> expression)
        {
            throw new NotImplementedException();
        }

        public override bool GetWhereQuery(string parameter, Operator op, object value, bool ignoreCase = true)
        {
            throw new NotImplementedException();
        }
    }

    

    public static class DbConnectionExtensions
    {
        public static BookQueryable Books(this DbConnection dbConnection, IDbTransaction transaction) => new BookQueryable(dbConnection, transaction, new NpgsqlQueryBuilder());
    }
}
