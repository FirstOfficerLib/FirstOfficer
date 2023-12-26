using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirstOfficer.Data.Query
{
    public static class Helper
    {
        public static string GetExpressionKey(string value)
        {
            return value
                    .Replace("(", "")
                    .Replace(")", "")
                    .Replace("And", "&")
                    .Replace("Also", "&")
                    .Replace("Or", "|")
                    .Replace("Else", "|")
                    .Replace("Not", "!")
                    .Replace(" ", "")

                ;
        }
    }
}
