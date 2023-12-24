using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using FirstOfficer.Data;
using FirstOfficer.Tests.Generator.Entities;
using Npgsql;
#pragma warning disable VSTHRD200

namespace FirstOfficer.Tests.Generator
{
    [TestFixture]
    [Parallelizable]
    public class SaveTests : FirstOfficerTest
    {
        [Test]
        public async Task SaveBooksTest()
        {
            var bookCount = 1000;
            var books = new List<Book>();
            for (int i = 0; i < bookCount; i++)
            {
                books.Add(new Book()
                {
                    Description = string.Empty.RandomString(100),
                    ISBN = string.Empty.RandomString(10),
                    Published = DateTime.Now,
                    Title = string.Empty.RandomString(10)
                });

            }
            var transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection.SaveBooks(books, transaction);
            await transaction.CommitAsync();

            var sql = $"SELECT COUNT(*) FROM {DataHelper.GetTableName<Book>()}";
            await using (var command = DbConnection.CreateCommand())
            {
                command.CommandText = sql;
                var count = await command.ExecuteScalarAsync();
                Assert.That(bookCount, Is.EqualTo(count)); //check count
            }

            var allBooks = (await DbConnection.QueryBooks()).ToList();
            Assert.That(allBooks.Count(), Is.EqualTo(bookCount));

            var source = books.OrderBy(a => a.Id).ToList();
            var destination = allBooks.OrderBy(a => a.Id).ToList();

            for (int a = 0; a < allBooks.Count(); a++)
            {
                Assert.That(source[a].Id, Is.EqualTo(destination[a].Id));
                Assert.That(source[a].Title, Is.EqualTo(destination[a].Title));
                Assert.That(source[a].PageCount, Is.EqualTo(destination[a].PageCount));
                Assert.That(source[a].ISBN, Is.EqualTo(destination[a].ISBN));
                Assert.That(source[a].Description, Is.EqualTo(destination[a].Description));
            }


            //delete all books
            sql = $"DELETE FROM {DataHelper.GetTableName<Book>()};";
            await using (var command = DbConnection.CreateCommand())
            {
                command.CommandText = sql;
                await command.ExecuteNonQueryAsync();
            }
            await transaction.DisposeAsync();
        }

        [Test]
        public async Task UpdateBooksTest()
        {
            var bookCount = 100000;
            var books = new List<Book>();
            for (int i = 0; i < bookCount; i++)
            {
                books.Add(new Book()
                {
                    Description = string.Empty.RandomString(100),
                    ISBN = string.Empty.RandomString(10),
                    Published = DateTime.Now,
                    Title = string.Empty.RandomString(10)
                });

            }
            //insert books
            var transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection.SaveBooks(books, transaction);
            await transaction.CommitAsync();
            var sql = $"SELECT COUNT(*) FROM {DataHelper.GetTableName<Book>()}";
            await using (var command = DbConnection.CreateCommand())
            {
                command.CommandText = sql;
                var count = await command.ExecuteScalarAsync();
                Assert.That(bookCount, Is.EqualTo(count)); //check count
            }

            //update books
            foreach (var book in books)
            {
                book.Title = string.Empty.RandomString(10);
                book.Published = DateTime.Now;
                book.ISBN = string.Empty.RandomString(10);
            }
            transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection.SaveBooks(books, transaction);
            await transaction.CommitAsync();
            var allBooks = (await DbConnection.QueryBooks()).ToList();
            Assert.That(allBooks.Count(), Is.EqualTo(bookCount));

            var source = books.OrderBy(a => a.Id).ToList();
            var destination = allBooks.OrderBy(a => a.Id).ToList();

            for (int a = 0; a < allBooks.Count(); a++)
            {
                Assert.That(source[a].Id, Is.EqualTo(destination[a].Id));
                Assert.That(source[a].Title, Is.EqualTo(destination[a].Title));
                Assert.That(source[a].PageCount, Is.EqualTo(destination[a].PageCount));
                Assert.That(source[a].ISBN, Is.EqualTo(destination[a].ISBN));
                Assert.That(source[a].Description, Is.EqualTo(destination[a].Description));
            }

            //delete all books
            sql = $"DELETE FROM {DataHelper.GetTableName<Book>()};";
            await using (var command = DbConnection.CreateCommand())
            {
                command.CommandText = sql;
                await command.ExecuteNonQueryAsync();
            }
            await transaction.DisposeAsync();
        }


        //test for author
        [Test]
        public async Task SaveSingleAuthor()
        {
            var author = new Author()
            {
                Email = string.Empty.RandomString(10),
                Name = string.Empty.RandomString(10),
                Website = string.Empty.RandomString(10)
            };
            var transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection.SaveAuthor(author, transaction);
            await transaction.CommitAsync();

            //fetch author from DB
            var sql = $"SELECT * FROM {DataHelper.GetTableName<Author>()} WHERE id = @id";
            var command = DbConnection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@id", author.Id);
            var reader = await command.ExecuteReaderAsync();
            //use data adapter to fill data table
            var dataTable = new DataTable();
            dataTable.Load(reader);

            //assert that the author was saved
            Assert.That(dataTable.Rows.Count, Is.EqualTo(1));
            //asset that all properties were saved
            Assert.That(dataTable.Rows[0]["name"], Is.EqualTo(author.Name));
            Assert.That(dataTable.Rows[0]["email"], Is.EqualTo(author.Email));
            Assert.That(dataTable.Rows[0]["website"], Is.EqualTo(author.Website));

            //remove author
            sql = $"DELETE FROM {DataHelper.GetTableName<Author>()} WHERE id = @id";
            command.CommandText = sql;
            command.Parameters.AddWithValue("@id", author.Id);
            command.ExecuteNonQuery();
            command.Dispose();
            await transaction.DisposeAsync();
        }

        //test for book
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

            await AssertSave(book);

            //test for update author
            book.Title = string.Empty.RandomString(10);
            book.Published = DateTime.Now;
            book.ISBN = string.Empty.RandomString(10);
            transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection.SaveBook(book, transaction);
            await transaction.CommitAsync();
            await AssertSave(book);

            //remove book
            var sql = $"DELETE FROM {DataHelper.GetTableName<Book>()} WHERE id = @id";
            var command = DbConnection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@id", book.Id);
            await command.ExecuteNonQueryAsync();
            await command.DisposeAsync();
            await transaction.DisposeAsync();

        }

        [Test]
        public async Task ChecksumTest()
        {
            var book = new Book()
            {
                Description = string.Empty.RandomString(10),
                ISBN = string.Empty.RandomString(10),
                Published = DateTime.Now,
                Title = string.Empty.RandomString(10)
            };


            var transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection.SaveBook(book, transaction);
            await transaction.CommitAsync();

            var checksum = book.Checksum();
            Assert.That(checksum, Is.EqualTo(book.Checksum()));

            var readBook =
                (await DbConnection.QueryBooks()).First();

            Assert.That(readBook.Checksum(), Is.EqualTo(checksum));

            transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection.DeleteBook(book, transaction);
            await transaction.CommitAsync();
            await transaction.DisposeAsync();
        }


    }
}
