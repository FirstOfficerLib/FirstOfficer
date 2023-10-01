using System;
using System.Collections.Generic;
using System.Text;

namespace FirstOfficer.Generator.StateManagement
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class StorageItemAttribute : Attribute
    {
        public StorageItemAttribute(string name, string checksum)
        {
            Name = name;
            Checksum = checksum;
        }

        public string Checksum { get; set; }

        public string Name { get; set; }
    }
}
