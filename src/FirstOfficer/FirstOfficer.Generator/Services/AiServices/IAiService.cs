using System;
using System.Collections.Generic;
using System.Text;

namespace FirstOfficer.Generator.Services.AiServices
{
    public interface IAiService
    {
        Task<string> GetSqlFromExpression(string expression);
    }
}
