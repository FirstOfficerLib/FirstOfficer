using FirstOfficer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirstOfficer.Data.Query;
using FirstOfficer.Tests.Generator.Entities;

namespace FirstOfficer.Tests.Generator
{
    [TestFixture]
    public class IncludeTests : FirstOfficerTest
    {
        //book include Authors test
        [Test]
        public async Task BookWithIncludedAuthors()
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
                    Title = alphabet[i] + string.Empty.RandomString(10),
                    Authors = new List<Author>()
                    {
                        new Author()
                        {
                            Name = string.Empty.RandomString(10),
                            Email = string.Empty.RandomString(10),
                            Website = string.Empty.RandomString(10)
                        },
                        new Author()
                        {
                            Name = string.Empty.RandomString(10),
                            Email = string.Empty.RandomString(10),
                            Website = string.Empty.RandomString(10)

                        }
                    }
                    
                });

            }
            var transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection.SaveBooks(books, transaction, true);
            await transaction.CommitAsync();

            var results = (await DbConnection.QueryBooks(b=> b.Id == Parameter.Value1, 
                new ParameterValues(books[0].Id), EntityBook.Includes.Authors)).ToList();

            Assert.That(results.Count(), Is.EqualTo(1));
            Assert.That(results.First().Authors.Count(), Is.EqualTo(2));
            Assert.That(results.First().Authors.First().Name, Is.EqualTo(books.First().Authors.First().Name));
            Assert.That(results.First().Authors.Last().Email, Is.EqualTo(books.First().Authors.Last().Email));


            transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection.DeleteBooks(books, transaction);
            await transaction.DisposeAsync();
        }

        //book include BookCover test

        //book include Pages test

        //book include Pages include Book test




    }
}
