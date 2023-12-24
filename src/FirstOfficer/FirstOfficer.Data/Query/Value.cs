using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirstOfficer.Data.Query
{
    public sealed class Value : IEqualityComparer<Value>
    {
        public bool Equals(Value x, Value y)
        {
            throw new NotSupportedException();
        }

        public bool Contains(Value value)
        {
            throw new NotSupportedException();
        }

        public int GetHashCode(Value obj)
        {
            throw new NotSupportedException();
        }

        public override bool Equals(object obj)
        {
            throw new NotSupportedException();
        }

        public override int GetHashCode()
        {
            throw new NotSupportedException();
        }

        // Override operators
        public static bool operator ==(Value left, Value right)
        {
            throw new NotSupportedException();
        }

        public static bool operator !=(Value left, Value right)
        {
            throw new NotSupportedException();
        }

        public static bool operator >(Value left, Value right)
        {
            throw new NotSupportedException();
        }

        public static bool operator <(Value left, Value right)
        {
            throw new NotSupportedException();
        }

        public static bool operator >=(Value left, Value right)
        {
            throw new NotSupportedException();
        }

        public static bool operator <=(Value left, Value right)
        {
            throw new NotSupportedException();
        }


    }
}
