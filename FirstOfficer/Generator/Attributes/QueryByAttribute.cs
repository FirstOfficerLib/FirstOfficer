using System.Linq.Expressions;

namespace FirstOfficer.Generator.Attributes
{
    public class QueryByAttribute : Attribute
    {
        public QueryByAttribute(string queryName, LambdaExpression lambdaFunction)
        {
            QueryName = queryName;
            LambdaFunction = lambdaFunction;
        }
        public LambdaExpression? LambdaFunction { get; private set; }

        public string QueryName { get; private set; }

        public string GetQuery()
        {
            return LambdaFunction?.Body.ToString() ?? string.Empty;
        }
    }
}
