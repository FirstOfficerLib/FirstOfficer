using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirstOfficer.Tests.Generator.Dto
{
    public class Author : Api.Dto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; }
        public string Website { get; set; }
        public List<Book> Books { get; set; } = new();
    }
}
