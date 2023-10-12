using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using FirstOfficer.Data;
using FirstOfficer.Data.Query;
using FirstOfficer.Tests.Generator.Models;
using Npgsql;

namespace FirstOfficer.Tests.Generator
{
    [TestFixture]
    public class QueryTests : FirstOfficerTest
    {
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

            books.First().Title = "dfafsdafas TestNamefsafddd";

            var transaction = await DbConnection.BeginTransactionAsync();

            await DbConnection.SaveBooks(books, transaction);
            await transaction.CommitAsync();



            // < Contains
            var results = await DbConnection!.QueryBooks(
                EntityBook.Includes.None,
                a => a.Published < Parameter.Value1 && a.Title.Contains(Parameter.Value2),
                new ParameterValues(DateTime.Now, "TestName"));

            Assert.That(results.Count(), Is.EqualTo(1));


            // >=
            results = await DbConnection!.QueryBooks(
                EntityBook.Includes.None,
                a => a.Published >= Parameter.Value1 && a.Title.Contains(Parameter.Value2),
                new ParameterValues(DateTime.Now, "TestName"));

            Assert.That(results.Count(), Is.EqualTo(0));


            // <
            results = await DbConnection!.QueryBooks(
                EntityBook.Includes.None,
                a => a.Published < Parameter.Value1,
                new ParameterValues(DateTime.Now));

            Assert.That(results.Count(), Is.EqualTo(5));

            // == Title
            results = (await DbConnection!.QueryBooks(
                EntityBook.Includes.None,
                a => a.Title ==  Parameter.Value1,
                new ParameterValues(books[2].Title))).ToList();

            Assert.That(results.Count(), Is.EqualTo(1));
            Assert.That(results.First().Title, Is.EqualTo(books[2].Title));

            // == ISBN
            results = (await DbConnection!.QueryBooks(
                               EntityBook.Includes.None,
                                              a => a.ISBN == Parameter.Value1,
                                              new ParameterValues(books[1].ISBN))).ToList();

            Assert.That(results.Count(), Is.EqualTo(1));
            Assert.That(results.First().ISBN, Is.EqualTo(books[1].ISBN));

            // ALL ==
            var book3 = books[3];
            results = (await DbConnection!.QueryBooks(
                EntityBook.Includes.None,
                a => a.ISBN == Parameter.Value1 
                && a.Title == Parameter.Value2
                && a.Published == Parameter.Value3
                && a.Id == Parameter.Value4,
                new ParameterValues(book3.ISBN,book3.Title,book3.Published,book3.Id))).ToList();

            Assert.That(results.Count(), Is.EqualTo(1));
            Assert.That(results.First().ISBN, Is.EqualTo(book3.ISBN));
            Assert.That(results.First().Title, Is.EqualTo(book3.Title));
            Assert.That(results.First().Published, Is.EqualTo(book3.Published));
            Assert.That(results.First().Id, Is.EqualTo(book3.Id));

            // OR 

            results = (await DbConnection!.QueryBooks(
                               EntityBook.Includes.None,
                      a => (a.ISBN == Parameter.Value1 && a.Title == Parameter.Value2) ||
                           (a.Published == Parameter.Value3 && a.Id == Parameter.Value4),
                      new ParameterValues(books[1].ISBN, books[1].Title, books[4].Published, books[4].Id))).ToList();

            Assert.That(results.Count(), Is.EqualTo(2));
            Assert.That(results.First().ISBN, Is.EqualTo(books[1].ISBN));
            Assert.That(results.First().Title, Is.EqualTo(books[1].Title));
            Assert.That(results.Last().Published, Is.EqualTo(books[4].Published));
            Assert.That(results.Last().Id, Is.EqualTo(books[4].Id));

            
        }
    }



}

