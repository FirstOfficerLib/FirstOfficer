using System;
using System.Collections.Generic;
using System.Text;

namespace FirstOfficer.Data.Attributes
{
    public class UniqueAttribute : Attribute
    {
        private readonly string _groupName;

        public UniqueAttribute()
        {
            _groupName = "default";
        }

        public UniqueAttribute(string groupName)
        {
            if (groupName.ToLower() == "default")
            {
                throw new ArgumentException("default is not a valid group name");
            }
            _groupName = groupName;
        }

    }
}
