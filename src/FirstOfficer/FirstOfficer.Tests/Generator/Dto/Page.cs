using FirstOfficer.Data;
using FirstOfficer.Data.Attributes;

namespace FirstOfficer.Tests.Generator.Dto
{
    public class Page : Api.Dto
    {
        public int? PageNumber { get; set; }
        public string Content { get; set; } = string.Empty;
        public Book Book { get; set; } = null;
      
    }
}
