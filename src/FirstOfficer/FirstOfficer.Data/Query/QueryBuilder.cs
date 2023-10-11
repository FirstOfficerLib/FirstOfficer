using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirstOfficer.Data.Query.Contracts;

namespace FirstOfficer.Data.Query
{
    public abstract class QueryBuilder : IQueryBuilder
    {
        public abstract bool GetWhereQuery(string parameter, Operator op, object value, bool ignoreCase = true);

        protected string GetOperator(Operator op)
        {
            return op switch
            {
                Operator.AreEquals => "=",
                Operator.NotAreEquals => "!=",
                Operator.GreaterThan => ">",
                Operator.GreaterThanOrEqual => ">=",
                Operator.LessThan => "<",
                Operator.LessThanOrEqual => "<=",
                Operator.Contains => "LIKE",
                Operator.StartsWith => "LIKE",
                Operator.EndsWith => "LIKE",
                Operator.In => "IN",
                Operator.NotIn => "NOT IN",
                Operator.IsNull => "IS NULL",
                Operator.IsNotNull => "IS NOT NULL",
                _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
            };
        }

    }
}
