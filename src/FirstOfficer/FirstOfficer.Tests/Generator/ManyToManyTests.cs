using FirstOfficer.Tests.Generator.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirstOfficer.Data.Query;
using FirstOfficer.Data.Exceptions;
using NUnit.Framework.Internal;

namespace FirstOfficer.Tests.Generator
{
    public class ManyToManyTests : FirstOfficerTest
    {
        //many to many tests for books and authors
        [Test]
        public async Task ManyToManyInsertBookTest()
        {
            var book = await CreateBookWithAuthors();

            AssertManyToManySave(new List<Book>() { book }, book.Authors.ToList());
            
        }


        [Test]
        public async Task ManyToManyDeleteBookTest()
        {
            var book = await CreateBookWithAuthors();
            AssertManyToManySave(new List<Book>() { book }, book.Authors.ToList());

            book.Authors.RemoveAt(2);

            var transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection.SaveBook(book, transaction, true);
            await transaction.CommitAsync();

            AssertManyToManySave(new List<Book>() { book }, book.Authors.ToList());

        }

        private async Task<Book> CreateBookWithAuthors()
        {
            var author = new Author
            {
                Name = "Jerry Fitzgerald",
                Email = string.Empty.RandomString(50)
            };

            //already existing author
            var transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection.SaveAuthor(author, transaction);
            await transaction.CommitAsync();


            var book = new Book
            {
                Title = "The Great Gatsby",
                Published = DateTime.Now,
                ISBN = "1234567890",
                Description = "A book about a rich guy",
                Price = 10.99m,
                Authors = new List<Author>
                {
                    new Author
                    {
                        Name = "F. Scott Fitzgerald",
                        Email = string.Empty.RandomString(50)
                    },
                    new Author
                    {
                        Name = "Ernest Hemingway",
                        Email = string.Empty.RandomString(50)
                    },
                    new Author()
                    {
                        Name = "John Steinbeck",
                        Email = string.Empty.RandomString(50)
                    }
                }
            };

            book.Authors.Add(author);

            transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection.SaveBook(book, transaction, true);
            await transaction.CommitAsync();
            return book;
        }
    }
}
