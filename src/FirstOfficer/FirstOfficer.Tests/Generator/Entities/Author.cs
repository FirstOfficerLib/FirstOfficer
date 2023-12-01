using FirstOfficer.Data;
using FirstOfficer.Data.Attributes;

namespace FirstOfficer.Tests.Generator.Entities
{
    public class Author : Entity
    {
        [Queryable]
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; }
        public string Website { get; set; }    
        public List<Book> Books { get; set; } = new();
      
    }
}
