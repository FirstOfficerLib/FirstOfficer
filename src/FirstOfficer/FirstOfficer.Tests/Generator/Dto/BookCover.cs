using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FirstOfficer.Data.Attributes;

namespace FirstOfficer.Tests.Generator.Dto
{
    public class BookCover : Api.Dto
    {
        public int? TypeId { get; set; }
        public string? Summary { get; set; }
        public Book Book { get; set; } = null!;

    }
}
