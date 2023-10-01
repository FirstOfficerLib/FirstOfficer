using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirstOfficer.Data;
using FirstOfficer.Data.Attributes;

namespace FirstOfficer.Tests.Generator.Models
{
    [Table("books")]
    public class Book : Entity
    {
        [Column("title")]
        [Queryable]
        public string? Title { get; set; }
        [Column("page_count")]
        //page count
        public int PageCount => Pages.Count();

        [Column("published")]
        public DateTime Published { get; set; }
        [Column("i_sb_n")]
        public string? ISBN { get; set; }
        [Column("description")]
        public string? Description { get; set; }
        [Column("price")]
        public decimal Price { get; set; }
        
        public ICollection<Page> Pages { get; set; } = new Collection<Page>();
        public List<Author> Authors { get; set; } = new();

    }
}
