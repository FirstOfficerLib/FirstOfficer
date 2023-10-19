using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirstOfficer.Api;
using FirstOfficer.Data.Attributes;
using FirstOfficer.Tests.Generator.Entities;

namespace FirstOfficer.Tests.Generator.Dto
{
    public  class Book : Api.Dto   
    {
        public string Title { get; set; } = null!;
        public int PageCount { get; set; }
        public DateTime Published { get; set; }
        public string Isbn { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal Price { get; set; }
        //one to one
        public BookCover? BookCover { get; set; }
        //one to many
        public ICollection<Page> Pages { get; set; } = new Collection<Page>();
        //many to many
        public IList<Author> Authors { get; set; } = new List<Author>();
    }
}
