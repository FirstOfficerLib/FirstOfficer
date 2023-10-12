using System;
using System.Collections.Generic;
using System.Text;

namespace FirstOfficer.Data.Query
{
    public interface ISqlParts
    {
         Dictionary<string, string> WhereClauses { get; }

    }
}
