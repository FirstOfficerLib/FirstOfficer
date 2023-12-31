﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using FirstOfficer.Tests.Generator.Entities;
using Npgsql;
#pragma warning disable VSTHRD200

namespace FirstOfficer.Tests.Generator
{
    [Parallelizable]
    [TestFixture]
    public class DeleteTests : FirstOfficerTest
    {
        [Test]
        public async Task DeleteBooksTest()
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

            var transaction = await DbConnection.BeginTransactionAsync();

            await DbConnection.SaveBooks(books, transaction);

            await transaction.CommitAsync();

            var sql = $"SELECT COUNT(*) FROM books;";
            await using (var command = DbConnection.CreateCommand())
            {
                command.CommandText = sql;
                var count = command.ExecuteScalar();
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

            transaction = await DbConnection.BeginTransactionAsync();
            //delete all books
            await DbConnection.DeleteBooks(books, transaction);

            await transaction.CommitAsync();

            var allBooksCount = (await DbConnection.QueryBooks()).ToList();
            Assert.That(0, Is.EqualTo(allBooksCount.Count));
            await transaction.DisposeAsync();
        }

        [Test]
        public async Task DeleteBooksTransactionTest()
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

            transaction = await DbConnection.BeginTransactionAsync();
            //delete all books
            await DbConnection.DeleteBooks(books, transaction);
            var conn2 = new NpgsqlConnection(ConnectionString);
            conn2.Open();
            var allBooks2 = (await conn2.QueryBooks()).ToList();
            Assert.That(allBooks2.Count(), Is.EqualTo(bookCount));
        
            await transaction.CommitAsync();

            var allBooksCount = (await conn2.QueryBooks()).ToList();
            Assert.That(0, Is.EqualTo(allBooksCount.Count));
            await transaction.DisposeAsync();
            await conn2.CloseAsync();
            conn2.Dispose();

        }


    }
}
