using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirstOfficer.Data.Query
{
    public sealed class ParameterValues
    {
        //list of valid types
        private static List<Type> ValidTypes = new List<Type>()
            {
                typeof(string),
                typeof(int),
                typeof(long),
                typeof(decimal),
                typeof(DateTime),
                typeof(bool),
                typeof(Guid),
                typeof(int?),
                typeof(long?),
                typeof(decimal?),
                typeof(DateTime?),
                typeof(bool?),
                typeof(Guid?),
                typeof(string[]),
                typeof(int[]),
                typeof(long[]),
                typeof(decimal[]),
                typeof(DateTime[]),
                typeof(Guid[])
            };

        private object CheckType(object value)
        {
            if (value is null)
            {
                return DBNull.Value;
            }
            var type = value.GetType();
            if (ValidTypes.Contains(type))
            {
                return value;
            }
            throw new ArgumentException($"Invalid type {type.Name}.");
        }

        public ParameterValues(object value1)
        {
            Value1 = CheckType(value1);
        }

        public ParameterValues(object value1, object value2)
        {
            Value1 = CheckType(value1);
            Value2 = CheckType(value2);
        }

        public ParameterValues(object value1, object value2, object value3)
        {
            Value1 = CheckType(value1);
            Value2 = CheckType(value2);
            Value3 = CheckType(value3);
        }

        public ParameterValues(object value1, object value2, object value3, object value4)
        {
            Value1 = CheckType(value1);
            Value2 = CheckType(value2);
            Value3 = CheckType(value3);
            Value4 = CheckType(value4);
        }

        public ParameterValues(object value1, object value2, object value3, object value4, object value5)
        {
            Value1 = CheckType(value1);
            Value2 = CheckType(value2);
            Value3 = CheckType(value3);
            Value4 = CheckType(value4);
            Value5 = CheckType(value5);
        }

        public ParameterValues(object value1, object value2, object value3, object value4, object value5,
            object value6)
        {
            Value1 = CheckType(value1);
            Value2 = CheckType(value2);
            Value3 = CheckType(value3);
            Value4 = CheckType(value4);
            Value5 = CheckType(value5);
            Value6 = CheckType(value6);
        }

        public ParameterValues(object value1, object value2, object value3, object value4, object value5,
            object value6, object value7)
        {
            Value1 = CheckType(value1);
            Value2 = CheckType(value2);
            Value3 = CheckType(value3);
            Value4 = CheckType(value4);
            Value5 = CheckType(value5);
            Value6 = CheckType(value6);
            Value7 = CheckType(value7);
        }

        public ParameterValues(object value1, object value2, object value3, object value4, object value5,
            object value6, object value7, object value8)
        {
            Value1 = CheckType(value1);
            Value2 = CheckType(value2);
            Value3 = CheckType(value3);
            Value4 = CheckType(value4);
            Value5 = CheckType(value5);
            Value6 = CheckType(value6);
            Value7 = CheckType(value7);
            Value8 = CheckType(value8);
        }
        public ParameterValues(object value1, object value2, object value3, object value4, object value5,
            object value6, object value7, object value8, object value9)
        {
            Value1 = CheckType(value1);
            Value2 = CheckType(value2);
            Value3 = CheckType(value3);
            Value4 = CheckType(value4);
            Value5 = CheckType(value5);
            Value6 = CheckType(value6);
            Value7 = CheckType(value7);
            Value8 = CheckType(value8);
            Value9 = CheckType(value9);
        }

        public ParameterValues(object value1, object value2, object value3, object value4, object value5,
            object value6, object value7, object value8, object value9, object value10)
        {
            Value1 = CheckType(value1);
            Value2 = CheckType(value2);
            Value3 = CheckType(value3);
            Value4 = CheckType(value4);
            Value5 = CheckType(value5);
            Value6 = CheckType(value6);
            Value7 = CheckType(value7);
            Value8 = CheckType(value8);
            Value9 = CheckType(value9);
            Value10 = CheckType(value10);
        }

        public object Value1 { get; }
        public object Value2 { get; }
        public object Value3 { get; }
        public object Value4 { get; }
        public object Value5 { get; }
        public object Value6 { get; }
        public object Value7 { get; }
        public object Value8 { get; }
        public object Value9 { get; }
        public object Value10 { get; }
    }

}
