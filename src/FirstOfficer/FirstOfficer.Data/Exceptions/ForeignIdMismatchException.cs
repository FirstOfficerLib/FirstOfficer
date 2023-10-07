using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirstOfficer.Data.Exceptions
{
    public class ForeignIdMismatchException : Exception
    {
        public ForeignIdMismatchException(string message) : base (message)
        {
            
        }
    }
}
