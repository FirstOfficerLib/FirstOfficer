using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirstOfficer.Data.Query;
using FirstOfficer.Data.Exceptions;
using FirstOfficer.Tests.Generator.Entities;
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

            var readBook = (await DbConnection.QueryBooks(b => b.Id == Parameter.Value1, new ParameterValues(book.Id), BookEntity.Includes.Authors)).First();
            
            Assert.That(readBook.Authors.Count(), Is.EqualTo(book.Authors.Count()));
            Assert.That(readBook.Authors.First().Name, Is.EqualTo(book.Authors.First().Name));
            Assert.That(readBook.Authors.Last().Email, Is.EqualTo(book.Authors.Last().Email));
            
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

            //saving another book because more than one book is required for the test
            var book2 = await CreateBookWithAuthors();
            AssertManyToManySave(new List<Book>() { book }, book.Authors.ToList());

            AssertManyToManySave(new List<Book>() { book2 }, book2.Authors.ToList());

        }

        [Test]
        public async Task ManyToManyRelatedBookTest()
        {
            var book = await CreateBookWithAuthors();
            var relatedBook = await CreateBookWithAuthors();

            book.RelatedBooks.Add(relatedBook);
            var transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection.SaveBook(book, transaction, true);
            await transaction.CommitAsync();

            var readBook = (await DbConnection.QueryBooks(b => b.Id == Parameter.Value1, new ParameterValues(book.Id), BookEntity.Includes.RelatedBooks | BookEntity.Includes.Authors)).First();
            Assert.That(readBook.RelatedBooks.Count(), Is.EqualTo(1));
            Assert.That(readBook.RelatedBooks.First().Title, Is.EqualTo(relatedBook.Title));
            Assert.That(readBook.Authors.Count(), Is.EqualTo(book.Authors.Count()));
            Assert.That(readBook.Authors.First().Name, Is.EqualTo(book.Authors.First().Name));
            Assert.That(readBook.Authors.Last().Email, Is.EqualTo(book.Authors.Last().Email));
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
