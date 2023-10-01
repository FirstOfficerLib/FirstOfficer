using FirstOfficer.Tests.Generator.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            var transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection.SaveBook(book, transaction);
            await transaction.CommitAsync();

            await AssertSavedBook(book);

            (await DbConnection.QueryBooks(EntityBook.Includes.None)).BookFilter(a => a.Id == book.Id || a.Checksum == book.Id.ToString());

        }

    }
    }
