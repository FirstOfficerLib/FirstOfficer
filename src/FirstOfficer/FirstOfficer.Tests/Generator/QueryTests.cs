using FirstOfficer.Tests.Generator.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirstOfficer.Data.Query;

namespace FirstOfficer.Tests.Generator
{
    public class QueryTests : FirstOfficerTest
    {

        //test query books  


        [Test]
        public async Task SaveSingleBook()
        {
            var book = new Book()
            {
                Description = string.Empty.RandomString(100),
                ISBN = string.Empty.RandomString(10),
                Published = DateTime.Now,
                Title = string.Empty.RandomString(10)
            };

            //Queryable<Book> books = new Queryable<Book>();

           // var b = books.ToList();

            var page = new Page()
            {
                //Book = book,
                Content = "Stuff",
                PageNumber = 10
            };

            var transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection.SavePage(page, transaction, true);
            await transaction.CommitAsync();

            await AssertSavedBook(book);


            var results = await DbConnection.QueryPages(EntityPage.Includes.Book,
                a => a.Id == book.Id || a.Checksum == book.Id.ToString());

        }

    }
    }
