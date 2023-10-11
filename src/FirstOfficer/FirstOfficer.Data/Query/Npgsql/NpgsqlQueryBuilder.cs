using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query;

namespace FirstOfficer.Data.Query.Npgsql
{
    public class NpgsqlQueryBuilder : QueryBuilder
    {
        public override bool GetWhereQuery(string parameter, Operator op, object value, bool ignoreCase = true)
        {
            throw new NotImplementedException();
        }
    }
}
