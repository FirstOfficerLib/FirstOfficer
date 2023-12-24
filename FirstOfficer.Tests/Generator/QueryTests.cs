using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using FirstOfficer.Data;
using FirstOfficer.Data.Query;
using FirstOfficer.Tests.Generator.Entities;
using Npgsql;
#pragma warning disable VSTHRD200

namespace FirstOfficer.Tests.Generator
{
    [TestFixture]
    [Parallelizable]
    public class QueryTests : FirstOfficerTest
    {


        //test for query with start and limit
        [Test]
        public async Task BookQueryWithStartAndLimitTest()
        {
            var alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            var books = new List<Book>();
            for (int i = 0; i < 5; i++)
            {
                books.Add(new Book()
                {
                    Description = string.Empty.RandomString(100),
                    ISBN = alphabet[i] + string.Empty.RandomString(10),
                    Published = DateTime.Now,
                    Title = alphabet[i] + string.Empty.RandomString(10)
                });

            }
            var transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection.SaveBooks(books, transaction);
            await transaction.CommitAsync();

            var results = (await DbConnection.QueryBooks(null, null, null, new [] { BookEntity.BookOrderBy.IdAsc }, 0, 2)).ToArray();

            Assert.That(results.Count(), Is.EqualTo(2));
            Assert.That(results.First().Title, Is.EqualTo(books.First().Title));
            Assert.That(results.Last().Title, Is.EqualTo(books[1].Title));

            results = (await DbConnection.QueryBooks(null, null, null, new[] { BookEntity.BookOrderBy.IdAsc }, 2, 2)).ToArray();

            Assert.That(results.Count(), Is.EqualTo(2));
            Assert.That(results.First().Title, Is.EqualTo(books[2].Title));
            Assert.That(results.Last().Title, Is.EqualTo(books[3].Title));

            results = (await DbConnection.QueryBooks(null, null, null, new[] { BookEntity.BookOrderBy.IdAsc }, 4, 2)).ToArray();

            Assert.That(results.Count(), Is.EqualTo(1));
            Assert.That(results.First().Title, Is.EqualTo(books[4].Title));

            var sql = $"DELETE FROM {DataHelper.GetTableName<Book>()};";
            var command = DbConnection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
            command.Dispose();
            await transaction.DisposeAsync();
        }



        [Test]
        public async Task BookOrderByQueryTest()
        {
            var alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            //Test order by title
            var books = new List<Book>();
            for (int i = 0; i < 5; i++)
            {
                books.Add(new Book()
                {
                    Description = string.Empty.RandomString(100),
                    ISBN =  alphabet[i] + string.Empty.RandomString(10),
                    Published = DateTime.Now,
                    Title = alphabet[i] + string.Empty.RandomString(10)
                });

            }
            var transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection.SaveBooks(books, transaction);
            await transaction.CommitAsync();

            var results = (await DbConnection.QueryBooks(null, null, null,
                new[] { BookEntity.BookOrderBy.TitleDesc })).ToArray();

            Assert.That(results.Count(), Is.EqualTo(5));
            Assert.That(results.First().Title, Is.EqualTo(books.Last().Title));

            //test order by title asc
            results = (await DbConnection.QueryBooks(null, null, null,
                               new[] { BookEntity.BookOrderBy.TitleAsc })).ToArray();       

            Assert.That(results.Count(), Is.EqualTo(5));
            Assert.That(results.First().Title, Is.EqualTo(books.First().Title));
            
            //test order by ISBN desc and then title 
            results = (await DbConnection.QueryBooks(null, null, null,
                                              new[] { BookEntity.BookOrderBy.ISBNDesc, BookEntity.BookOrderBy.TitleAsc })).ToArray();   

            Assert.That(results.Count(), Is.EqualTo(5));
            Assert.That(results.First().ISBN, Is.EqualTo(books.Last().ISBN));
            Assert.That(results.First().Title, Is.EqualTo(books.Last().Title));
            
            
            var sql = $"DELETE FROM {DataHelper.GetTableName<Book>()};";
            var command = DbConnection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
            command.Dispose();
            await transaction.DisposeAsync();

        }





        [Test]
        public async Task BookWhereQueryTest()
        {

            var books = new List<Book>();
            for (int i = 0; i < 5; i++)
            {
                books.Add(new Book()
                {
                    Description = string.Empty.RandomString(100),
                    ISBN = string.Empty.RandomString(10),
                    Published = DateTime.Now,
                    Title = string.Empty.RandomString(10)
                });

            }

            books.First().Title = "Changed TestTitle";
            books.Last().ISBN = null;

            var transaction = await DbConnection.BeginTransactionAsync();

            await DbConnection.SaveBooks(books, transaction);
            await transaction.CommitAsync();
                     
            var nullResults = await DbConnection!.QueryBooks(a=> a.ISBN== Parameter.Value1,
                new ParameterValues(null));
            
            Assert.That(nullResults.Count(), Is.EqualTo(1));


            // < Contains
            var results = await DbConnection!.QueryBooks(a => a.Published < Parameter.Value1 && a.Title.Contains(Parameter.Value2),
                new ParameterValues(DateTime.Now, "TestTitle"), BookEntity.Includes.None);

            Assert.That(results.Count(), Is.EqualTo(1));


            // >=
            results = await DbConnection!.QueryBooks(a => a.Published >= Parameter.Value1 && a.Title.Contains(Parameter.Value2),
                new ParameterValues(DateTime.Now, "TestTitle"), BookEntity.Includes.None);

            Assert.That(results.Count(), Is.EqualTo(0));


            // <
            results = await DbConnection!.QueryBooks(a => a.Published < Parameter.Value1,
                new ParameterValues(DateTime.Now), BookEntity.Includes.None);

            Assert.That(results.Count(), Is.EqualTo(5));

            // == Title
            results = (await DbConnection!.QueryBooks(a => a.Title == Parameter.Value1,
                new ParameterValues(books[2].Title), BookEntity.Includes.None)).ToList();

            Assert.That(results.Count(), Is.EqualTo(1));
            Assert.That(results.First().Title, Is.EqualTo(books[2].Title));

            // == ISBN
            results = (await DbConnection!.QueryBooks(a => a.ISBN == Parameter.Value1,
                                              new ParameterValues(books[1].ISBN), BookEntity.Includes.None)).ToList();

            Assert.That(results.Count(), Is.EqualTo(1));
            Assert.That(results.First().ISBN, Is.EqualTo(books[1].ISBN));

            // ALL ==
            var book3 = books[3];
            results = (await DbConnection!.QueryBooks(a => a.ISBN == Parameter.Value1
                                                           && a.Title == Parameter.Value2
                                                           && a.Published == Parameter.Value3
                                                           && a.Id == Parameter.Value4,
                new ParameterValues(book3.ISBN, book3.Title, book3.Published, book3.Id), BookEntity.Includes.None)).ToList();

            Assert.That(results.Count(), Is.EqualTo(1));
            Assert.That(results.First().ISBN, Is.EqualTo(book3.ISBN));
            Assert.That(results.First().Title, Is.EqualTo(book3.Title));
            Assert.That(results.First().Published, Is.EqualTo(book3.Published));
            Assert.That(results.First().Id, Is.EqualTo(book3.Id));

            // OR 

            results = (await DbConnection!.QueryBooks(a => (a.ISBN == Parameter.Value1 && a.Title == Parameter.Value2) ||
                                                           (a.Published == Parameter.Value3 && a.Id == Parameter.Value4),
                      new ParameterValues(books[1].ISBN, books[1].Title, books[4].Published, books[4].Id), BookEntity.Includes.None)).ToList();

            Assert.That(results.Count(), Is.EqualTo(2));
            Assert.That(results.First().ISBN, Is.EqualTo(books[1].ISBN));
            Assert.That(results.First().Title, Is.EqualTo(books[1].Title));
            Assert.That(results.Last().Published, Is.EqualTo(books[4].Published));
            Assert.That(results.Last().Id, Is.EqualTo(books[4].Id));

          
            var sql = $"DELETE FROM {DataHelper.GetTableName<Book>()};";
            var command = DbConnection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
            command.Dispose();
            await transaction.DisposeAsync();
        }
    }



}

