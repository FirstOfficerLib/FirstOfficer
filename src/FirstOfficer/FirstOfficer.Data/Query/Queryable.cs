using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FirstOfficer.Data.Query
{
    public class Queryable<T> : IQueryable<T>  where T : class
    {
        public IEnumerator<T> GetEnumerator()
        {
            var gen = new NpgsqlQuerySqlGenerator(null, false, new Version(10, 0));
            var command = gen.GetCommand(Expression);
            return (IEnumerator<T>)new List<T>().AsEnumerable();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Type ElementType { get; set; }
        public Expression Expression { get; set; }
        public IQueryProvider Provider { get; set; }

        
    }
}
