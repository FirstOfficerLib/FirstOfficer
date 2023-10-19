using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FirstOfficer.Data;
using FirstOfficer.Data.Attributes;

namespace FirstOfficer.Tests.Generator.Entities
{
    public class BookCover : Entity
    {
        public int? TypeId { get; set; }
        [TextSize(0)]
        public string? Summary { get; set; }
        [Required]
        [ForeignKey("Book")]

        public long BookId { get; set; }
        public Book Book { get; set; } 


    }
}
