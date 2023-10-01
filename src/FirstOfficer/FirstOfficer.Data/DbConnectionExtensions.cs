using System.Data;
using System.Data.Common;

namespace FirstOfficer.Data
{
    public static class DbConnectionExtensions
    {
        //execute extension
        public static void Execute(this IDbConnection connection, string sql)
        {
            var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
    }
}
