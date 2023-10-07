using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirstOfficer.Data;
using FirstOfficer.Data.Attributes;

namespace FirstOfficer.Tests.Generator.Models
{
    public class Page : Entity
    {
        public long BookId { get; set; }    
        public int? PageNumber { get; set; }
        [TextSize(0)] // Text
        public string Content { get; set; } = string.Empty;

        public Book? Book { get; set; } = null;
      
    }
}
