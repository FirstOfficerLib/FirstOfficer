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
using Npgsql;
#pragma warning disable VSTHRD200

namespace FirstOfficer.Tests.Generator
{
    [Parallelizable]
    public class OneToManyTests : FirstOfficerTest
    {
        //test saving book with pages
        [Test]
        public async Task InsertBookWithPages()
        {
            var book = GetBookWithPages();

            var transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection.SaveBook(book, transaction, true);
            await transaction.CommitAsync();

            await AssertSave(book);
            foreach (var page in book.Pages)
            {
                await AssertSave(page);
            }
            await transaction.DisposeAsync();
        }

        //test update book with pages
        [Test]
        public async Task UpdateBookWithPages()
        {
            var book = GetBookWithPages();

            var transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection.SaveBook(book, transaction, true);
            await transaction.CommitAsync();

            await AssertSave(book);
            foreach (var page in book.Pages)
            {
                await AssertSave(page);
            }

            foreach (var page in book.Pages)
            {
                page.Content = string.Empty.RandomString(1000);
            }

            transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection.SaveBook(book, transaction, true);
            await transaction.CommitAsync();

            await AssertSave(book);
            foreach (var page in book.Pages)
            {
                await AssertSave(page);
            }
            await transaction.DisposeAsync();
        }

        //test saving book with pages with incorrect book id
        [Test]
        public async Task UpdateBookWithPagesIncorrectBookId()
        {
            var book = GetBookWithPages();

            var transaction = await DbConnection.BeginTransactionAsync();
            await DbConnection.SaveBook(book, transaction, true);
            await transaction.CommitAsync();

            await AssertSave(book);
            foreach (var page in book.Pages)
            {
                await AssertSave(page);
            }

            foreach (var page in book.Pages)
            {
                page.BookId++;
            }
            
            transaction = await DbConnection.BeginTransactionAsync();
            var exception = Assert.ThrowsAsync<ForeignIdMismatchException>(async () => await DbConnection.SaveBook(book, transaction, true))!;
            Assert.That(exception, Is.Not.Null);
            await transaction.RollbackAsync();

            await AssertSave(book);
            var exceptions = new List<Exception?>();
            
            //transaction never happened
            using (new TestExecutionContext.IsolatedContext())
            {
                foreach (var page in book.Pages)
                {
                    exceptions.Add(Assert.ThrowsAsync<AssertionException>(async () => await AssertSave(page)));
                }
            }

            Assert.That(exceptions.All(a => a != null), Is.True);
            await transaction.DisposeAsync();
        }

        //test inserting book with pages with incorrect book id
        [Test]
        public async Task InsertBookWithPagesIncorrectBookId()
        {
            var book = GetBookWithPages();

            var transaction = await DbConnection.BeginTransactionAsync();
            foreach (var page in book.Pages)
            {
                page.BookId++;
            }
            var exception = Assert.ThrowsAsync<ForeignIdMismatchException>(async () => await DbConnection.SaveBook(book, transaction, true))!;
            Assert.That(exception, Is.Not.Null);
            await transaction.RollbackAsync();
            var exceptions = new List<Exception?>();
            //transaction never happened
            using (new TestExecutionContext.IsolatedContext())
            {
                exceptions.Add(Assert.ThrowsAsync<AssertionException>(async () => await AssertSave(book)));
                foreach (var page in book.Pages)
                {
                    exceptions.Add(Assert.ThrowsAsync<AssertionException>(async () => await AssertSave(page)));
                }

            }

            Assert.That(exceptions.All(a => a != null), Is.True);

            await transaction.DisposeAsync();
        }

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

            await AssertSave(page.Book);
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
            catch (PostgresException e)
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
