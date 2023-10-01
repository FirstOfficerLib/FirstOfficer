using FirstOfficer.Tests.Generator.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using System.Data;
using FirstOfficer.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FirstOfficer.Tests.Generator
{
    [TestFixture]
    internal class PerformanceTests : FirstOfficerTest
    {
        //test query books
        [Test]
        [Ignore("")]
        public async Task Saving10000Test()
        {
            var bookCount = 10000;
            await SaveTest(bookCount);
        }
        [Test]
        public async Task Saving1000Test()
        {
            var bookCount = 1000;
            await SaveTest(bookCount);
        }

        [Test]
        public async Task Saving10Test()
        {
            var bookCount = 10;
            await SaveTest(bookCount);
        }

        [Test]
        public async Task Saving100Test()
        {
            var bookCount = 100;
            await SaveTest(bookCount);
        }

        [Test]
        public async Task Saving1Test()
        {
            var bookCount = 1;
            await SaveTest(bookCount);
        }

        private async Task SaveTest(int bookCount)
        {
            Console.WriteLine($"Saving {bookCount} book(s) Test");
            var books = GetBooks(bookCount);

            Stopwatch stopwatch = new Stopwatch();
            var transaction = await DbConnection.BeginTransactionAsync();
            stopwatch.Start();
            await DbConnection!.SaveBooks(books, transaction);
            stopwatch.Stop();
            await transaction.CommitAsync();
            TimeSpan timeTaken = stopwatch.Elapsed;
            Console.WriteLine($"Code Generated Time taken: {timeTaken.TotalMilliseconds}ms");

            Assert.That(books.Any(a => a.Id == 0), Is.False);

            string insertQuery = @"INSERT INTO books(description, i_sb_n, published, title) 
                           VALUES (@Description, @ISBN, @Published, @Title) RETURNING id;";

            books = GetBooks(bookCount);

            stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var book in books)
            {
                var insertedId = await DbConnection.ExecuteScalarAsync<int>(insertQuery, book);
                book.Id = insertedId;
            }
            stopwatch.Stop();
            timeTaken = stopwatch.Elapsed;
            Console.WriteLine($"Dapper Time taken: {timeTaken.TotalMilliseconds}ms");
            Assert.That(books.Any(a => a.Id == 0), Is.False);

            books = GetBooks(bookCount);
            stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var book in books)
            {
                var insertedId = await DbConnection.ExecuteScalarAsync<int>(insertQuery, book);
                book.Id = insertedId;
            }
            stopwatch.Stop();
            timeTaken = stopwatch.Elapsed;
            Console.WriteLine($"Dapper #2 Time taken: {timeTaken.TotalMilliseconds}ms");
            Assert.That(books.Any(a => a.Id == 0), Is.False);


            books = GetBooks(bookCount);
            using (var context = new BookContext() { ConnectionString = ConnectionString })
            {
                context.Books.AddRange(books);
                stopwatch = new Stopwatch();
                stopwatch.Start();
                await context.SaveChangesAsync();
                timeTaken = stopwatch.Elapsed;
                Console.WriteLine($"EF Core Time taken: {timeTaken.TotalMilliseconds}ms");
                Assert.That(books.Any(a => a.Id == 0), Is.False);
            }

            //delete all books
            var sql = $"DELETE FROM {DataHelper.GetTableName<Book>()};";
            await using var command = DbConnection!.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        [Test]
        public async Task Delete1000Test()
        {
            var bookCount = 1000;
            await DeleteTest(bookCount);
        }
        [Test]
        public async Task Delete100Test()
        {
            var bookCount = 100;
            await DeleteTest(bookCount);
        }
        [Test]
        public async Task Delete10Test()
        {
            var bookCount = 10;
            await DeleteTest(bookCount);
        }
        [Test]
        public async Task Delete1Test()
        {
            var bookCount = 1;
            await DeleteTest(bookCount);
        }

        private async Task DeleteTest(int bookCount)
        {
            Console.WriteLine($"Delete {bookCount} book(s) Test");
            var books = GetBooks(bookCount);

            var transaction = await DbConnection.BeginTransactionAsync();

            await DbConnection!.SaveBooks(books, transaction);
            await transaction.CommitAsync();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection.SaveBooks(books, transaction);
            await transaction.CommitAsync();
            stopwatch.Stop();
            TimeSpan timeTaken = stopwatch.Elapsed;
            Console.WriteLine($"Code Generated Time taken: {timeTaken.TotalMilliseconds}ms");

            Assert.That(books.Any(a => a.Id == 0), Is.False);

            string insertQuery = @"INSERT INTO books(description, i_sb_n,  published, title) 
                           VALUES (@Description, @ISBN, @Published, @Title) RETURNING id;";

            books = GetBooks(bookCount);
            foreach (var book in books)
            {
                var insertedId = await DbConnection.ExecuteScalarAsync<int>(insertQuery, book);
                book.Id = insertedId;
            }
            Assert.That(books.Any(a => a.Id == 0), Is.False);

            string deleteQuery = $"DELETE FROM {DataHelper.GetTableName<Book>()} WHERE Id = @Id;";
            stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var book in books)
            {
                await DbConnection.ExecuteAsync(deleteQuery, new { book.Id });
            }
            stopwatch.Stop();
            timeTaken = stopwatch.Elapsed;
            Console.WriteLine($"Dapper Time taken: {timeTaken.TotalMilliseconds}ms");

            books = GetBooks(bookCount);
            using (var context = new BookContext() { ConnectionString = ConnectionString })
            {
                context.Books.AddRange(books);
                await context.SaveChangesAsync();
                Assert.That(books.Any(a => a.Id == 0), Is.False);
                stopwatch = new Stopwatch();
                stopwatch.Start();
                context.Books.RemoveRange(books);
                await context.SaveChangesAsync();
                timeTaken = stopwatch.Elapsed;
                Console.WriteLine($"EF Core Time taken: {timeTaken.TotalMilliseconds}ms");
            }
        }


        [Test]
        public async Task Query1Test()
        {
            var bookCount = 1;
            await QueryTest(bookCount);
        }

        [Test]
        public async Task Query10Test()
        {
            var bookCount = 10;
            await QueryTest(bookCount);
        }

        [Test]
        public async Task Query100Test()
        {
            var bookCount = 100;
            await QueryTest(bookCount);
        }

        [Test]
        public async Task Query1000Test()
        {
            var bookCount = 1000;
            await QueryTest(bookCount);
        }

        private async Task QueryTest(int bookCount)
        {
            Console.WriteLine($"Query {bookCount} book(s) Test");
            var books = GetBooks(bookCount);
            var transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection!.SaveBooks(books, transaction);
            await transaction.CommitAsync();
            var sql = $"SELECT * FROM {DataHelper.GetTableName<Book>()};";

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var rtnBooks = (await DbConnection.QueryBooks(EntityBook.Includes.None)).ToList();
            stopwatch.Stop();
            TimeSpan timeTaken = stopwatch.Elapsed;
            Console.WriteLine($"Code Generated Time taken: {timeTaken.TotalMilliseconds}ms");
            
            stopwatch = new Stopwatch();
            stopwatch.Start();
            rtnBooks = (await DbConnection.QueryAsync<Book>(sql)).ToList();
            stopwatch.Stop();
            timeTaken = stopwatch.Elapsed;
            Console.WriteLine($"Dapper Time taken: {timeTaken.TotalMilliseconds}ms");


            var context = new BookContext() { ConnectionString = ConnectionString };

            stopwatch = new Stopwatch();
            stopwatch.Start();
            rtnBooks = context.Books.ToList();
            stopwatch.Stop();
            timeTaken = stopwatch.Elapsed;
            Console.WriteLine($"EF Core taken: {timeTaken.TotalMilliseconds}ms");


            //delete all books
            sql = $"DELETE FROM {DataHelper.GetTableName<Book>()};";
            await using var command = DbConnection!.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        private static List<Book> GetBooks(int bookCount)
        {
            List<Book> books = new List<Book>();
            for (int i = 0; i < bookCount; i++)
            {
                books.Add(new Book()
                {
                    Description = string.Empty.RandomString(100),
                    ISBN = string.Empty.RandomString(10),
                    Published = DateTime.Now,
                    Title = string.Empty.RandomString(10),
                    Checksum = string.Empty.RandomString(10)
                });
                
            }
            return books;
        }

        private class BookContext : DbContext
        {
            public DbSet<Book> Books { get; set; }
            public string ConnectionString { get; init; }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseNpgsql(ConnectionString);
            }
        }
    }


}
