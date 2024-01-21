namespace FirstOfficer.Data
{
    public static  class DataReaderExtensions
    {
        //does column exist
        public static bool HasColumn(this System.Data.Common.DbDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
