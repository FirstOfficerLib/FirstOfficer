using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirstOfficer.Data;

namespace FirstOfficer.Tests.Generator.Models
{
    public class Author : Entity
    {
        public string Name { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }    
        public virtual List<Book> Books { get; set; } = new();
      
    }
}
