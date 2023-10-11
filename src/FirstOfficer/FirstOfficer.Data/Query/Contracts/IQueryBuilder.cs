namespace FirstOfficer.Data.Query.Contracts;

public interface IQueryBuilder
{
    bool GetWhereQuery(string parameter, Operator op, object value, bool ignoreCase = true);
}