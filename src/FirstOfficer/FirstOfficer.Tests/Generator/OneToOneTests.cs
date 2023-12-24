using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirstOfficer.Data.Query;
using FirstOfficer.Data.Exceptions;
using FirstOfficer.Tests.Generator.Entities;
#pragma warning disable VSTHRD200

namespace FirstOfficer.Tests.Generator
{
    [Parallelizable]
    public class OneToOneTests : FirstOfficerTest
    {

        //test query books  


        [Test]
        public async Task InsertPageWithBook()
        {
            var book = new Book()
            {
                Description = string.Empty.RandomString(100),
                ISBN = string.Empty.RandomString(10),
                Published = DateTime.Now,
                Title = string.Empty.RandomString(10)
            };

            var page = new Page()
            {
                Book = book,
                Content = string.Empty.RandomString(1000),
                PageNumber = 10
            };

            var transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection.SavePage(page, transaction, true);
            await transaction.CommitAsync();

            await AssertSave(book);
            await AssertSave(page);
            await transaction.DisposeAsync();

        }


        [Test]
        public async Task InsertPageWithoutBook()
        {
            var page = new Page()
            {
                Content = "Stuff",
                PageNumber = 10
            };

            var transaction = await DbConnection.BeginTransactionAsync();

            try
            {
                await DbConnection.SavePage(page, transaction, true);
                await transaction.CommitAsync();
                Assert.Fail("Should have thrown an exception");
            }
            catch (MissingEntityException e)
            {
                Assert.That(e.Message, Is.EqualTo("Book is required."));
            }
            finally
            {
                await transaction.RollbackAsync();
            }
            await transaction.DisposeAsync();
        }



        [Test]
        public async Task UpdatePageAndBook()
        {
            var book = new Book()
            {
                Description = string.Empty.RandomString(100),
                ISBN = string.Empty.RandomString(10),
                Published = DateTime.Now,
                Title = string.Empty.RandomString(10)
            };

            var page = new Page()
            {
                Book = book,
                Content = string.Empty.RandomString(1000),
                PageNumber = 10
            };

            var transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection.SavePage(page, transaction, true);
            await transaction.CommitAsync();

            await AssertSave(book);
            await AssertSave(page);

            page.Book.Title = string.Empty.RandomString(10);
            page.Book.Published = DateTime.Now;
            page.Book.ISBN = string.Empty.RandomString(10);
            page.Content = string.Empty.RandomString(1000);
            page.PageNumber = 20;

            transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection.SavePage(page, transaction, true);
            await transaction.CommitAsync();

            await AssertSave(page.Book);
            await AssertSave(page);
            await transaction.DisposeAsync();
        }

        //insert page with book and mismatched book id
        [Test]
        public async Task InsertPageWithBookMismatchedBookId()
        {
            var book = new Book()
            {
                Description = string.Empty.RandomString(100),
                ISBN = string.Empty.RandomString(10),
                Published = DateTime.Now,
                Title = string.Empty.RandomString(10)
            };
            var page = new Page()
            {
                Book = book,
                Content = string.Empty.RandomString(1000),
                PageNumber = 10
            };
            var transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection.SavePage(page, transaction, true);
            await transaction.CommitAsync();
            await AssertSave(book);
            await AssertSave(page);
            page.Book.Id++;
            transaction = await DbConnection.BeginTransactionAsync();
            try
            {
                await DbConnection.SavePage(page, transaction, true);
                await transaction.CommitAsync();
                Assert.Fail("Should have thrown an exception");
            }
            catch (ForeignIdMismatchException e)
            {
                //just looking for this exception
            }
            finally
            {
                await transaction.RollbackAsync();
            }
            await transaction.DisposeAsync();
        }

        //page with book and then save page without child book
        [Test]
        public async Task InsertPageWithBookAndThenSavePageWithoutChildBook()
        {
            var book = new Book()
            {
                Description = string.Empty.RandomString(100),
                ISBN = string.Empty.RandomString(10),
                Published = DateTime.Now,
                Title = string.Empty.RandomString(10)
            };
            var page = new Page()
            {
                Book = book,
                Content = string.Empty.RandomString(1000),
                PageNumber = 10
            };
            var transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection.SavePage(page, transaction, true);
            await transaction.CommitAsync();
            await AssertSave(book);
            await AssertSave(page);
            page.Book = null;
            transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection.SavePage(page, transaction, true);
            await transaction.CommitAsync();
            await AssertSave(book);
            await AssertSave(page);
            await transaction.DisposeAsync();
        }

        //insert page with book and then update page without child book
        [Test]
        public async Task InsertPageWithBookAndThenUpdatePageWithoutChildBook()
        {
            var book = new Book()
            {
                Description = string.Empty.RandomString(100),
                ISBN = string.Empty.RandomString(10),
                Published = DateTime.Now,
                Title = string.Empty.RandomString(10)
            };
            var page = new Page()
            {
                Book = book,
                Content = string.Empty.RandomString(1000),
                PageNumber = 10
            };
            var transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection.SavePage(page, transaction, true);
            await transaction.CommitAsync();
            await AssertSave(book);
            await AssertSave(page);
            page.Book = null;
            page.PageNumber = 15;
            transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection.SavePage(page, transaction, false);
            await transaction.CommitAsync();
            await AssertSave(page);
            await transaction.DisposeAsync();
        }

    }
}
