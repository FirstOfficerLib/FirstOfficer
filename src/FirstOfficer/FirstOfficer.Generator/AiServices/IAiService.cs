using System;
using System.Collections.Generic;
using System.Text;

namespace FirstOfficer.Generator.AiServices
{
    internal interface IAiService
    {
        Task<string> GetSqlFromExpression(string expression);
    }
}
