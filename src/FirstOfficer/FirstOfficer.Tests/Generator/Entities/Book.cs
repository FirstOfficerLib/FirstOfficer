using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using FirstOfficer.Data;
using FirstOfficer.Data.Attributes;

namespace FirstOfficer.Tests.Generator.Entities
{
    [Table("books")]
    public class Book : Entity
    {
        [Column("title")]   //for benchmarking for EF Core
        [Queryable]
        [OrderBy]
        public string Title { get; set; }
        [Column("page_count")] //for benchmarking for EF Core
        //page count
        public int PageCount {get; set; }
        [Queryable]
        [OrderBy]
        [Column("published")]
        public DateTime Published { get; set; }
        [Column("isbn")]
        [TextSize(50)]
        [Queryable]
        [OrderBy]
        public string ISBN { get; set; }
        [Column("description")]
        public string Description { get; set; }
        [Column("price")]
        [OrderBy]
        public decimal Price { get; set; }
        [Column("book_cover_id")]
        public long? BookCoverId { get; set; }

        //one to one
        public BookCover? BookCover { get; set; }
        //one to many
        public ICollection<Page> Pages { get; set; } = new Collection<Page>();
        //many to many
        public IList<Author> Authors { get; set; } = new List<Author>();

    }
}
