using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirstOfficer.Data.Query
{
    public enum Operator
    {
        AreEqual,
        AreNotEqual,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Contains,
        StartsWith,
        EndsWith,
        IsNull,
        IsNotNull,
        In,
        NotIn,
        Between,
        NotBetween
    }
}
