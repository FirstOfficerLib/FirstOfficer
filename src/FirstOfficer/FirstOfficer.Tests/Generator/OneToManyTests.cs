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
    }
}
