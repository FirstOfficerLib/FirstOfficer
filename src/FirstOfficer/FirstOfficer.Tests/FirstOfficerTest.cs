using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirstOfficer.Data;
using FirstOfficer.Tests.Generator.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FirstOfficer.Tests
{
    [TestFixture]
    public abstract class FirstOfficerTest
    {
        protected readonly NpgsqlConnection DbConnection = null!;
        protected IServiceProvider? ServiceProvider { get; private set; }
        protected string? ConnectionString { get; private set; }

        protected FirstOfficerTest()
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            if (ServiceProvider != null)
            {
                return;
            }

            var builder = Host.CreateDefaultBuilder();

            builder.ConfigureHostConfiguration(a =>
                a.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddEnvironmentVariables()
            );

            var app = builder.ConfigureServices((builderContext, services) =>
            {
                services.AddLogging(a => a.AddConsole());
                services.AddTransient<IDbConnection>(a =>
                {
                    var config = a.GetService<IConfiguration>();
                    ConnectionString = config!.GetConnectionString("default");
                    return new NpgsqlConnection(ConnectionString);

                });
                services.AddSingleton<IDatabaseBuilder, PostgresDatabaseBuilder>();

            }).Build();

            ServiceProvider = app.Services;

            DbConnection = ServiceProvider.GetService<IDbConnection>() as NpgsqlConnection ?? null!;
            if (DbConnection == null)
            {
                throw new Exception("DbConnection is null");
            }       
            DbConnection.Open();

            var tables = new List<string>();
            do
            {
                //drop all tables
                var sql = @"SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'";
                var command = DbConnection!.CreateCommand();
                command.CommandText = sql;
                var reader = command.ExecuteReader();
                tables.Clear();
                while (reader.Read())
                {
                    tables.Add(reader.GetString(0));
                }

                reader.Close();

                foreach (var table in tables)
                {
                    sql = $"DROP TABLE {table};";
                    command.CommandText = sql;
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch
                    {
                        //FK issue loop until all tables are dropped
                    }
                }
            } while (tables.Any());

            var dbBuilder = ServiceProvider!.GetService<IDatabaseBuilder>();
            dbBuilder!.BuildDatabase();

        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await DbConnection!.CloseAsync();
            await DbConnection.DisposeAsync();
        }


        protected async Task AssertSave(Book book)
        {
            //fetch book from DB    
            var sql = $"SELECT * FROM {DataHelper.GetTableName<Book>()} WHERE id = @id";
            var command = DbConnection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@id", book.Id);
            var reader = await command.ExecuteReaderAsync();

            //use data adapter to fill data table
            var dataTable = new DataTable();
            dataTable.Load(reader);

            //assert that the book was saved
            Assert.That(dataTable.Rows.Count, Is.EqualTo(1));
            //asset that all properties were saved
            Assert.That(book.Title, Is.EqualTo(dataTable.Rows[0]["title"]));
            Assert.That(book.Description, Is.EqualTo(dataTable.Rows[0]["description"]));
            Assert.That(book.ISBN, Is.EqualTo(dataTable.Rows[0]["i_sb_n"]));
            Assert.That(book.Published, Is.EqualTo(dataTable.Rows[0]["published"]));


            command.Dispose();
        }

        protected async Task AssertSave(Page page)
        {
            //fetch book from DB    
            var sql = $"SELECT * FROM {DataHelper.GetTableName<Page>()} WHERE id = @id";
            var command = DbConnection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@id", page.Id);
            var reader = await command.ExecuteReaderAsync();

            //use data adapter to fill data table
            var dataTable = new DataTable();
            dataTable.Load(reader);

            //assert that the book was saved
            Assert.That(dataTable.Rows.Count, Is.EqualTo(1));
            //asset that all properties were saved

            Assert.That(page.BookId,Is.EqualTo(dataTable.Rows[0]["book_id"]));
            Assert.That(page.Content,Is.EqualTo(dataTable.Rows[0]["content"]));
            Assert.That(page.PageNumber,Is.EqualTo(dataTable.Rows[0]["page_number"]));

            command.Dispose();
        }

        protected async Task AssertSave(Author author)
        {
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
            Assert.That(author.Email, Is.EqualTo(dataTable.Rows[0]["email"]));
            Assert.That(author.Name, Is.EqualTo(dataTable.Rows[0]["name"]));
            Assert.That(author.Website, Is.EqualTo(dataTable.Rows[0]["website"]));
            command.Dispose();
        }


        protected Book GetBookWithPages()
        {
            var book = new Book()
            {
                Description = string.Empty.RandomString(100),
                ISBN = string.Empty.RandomString(10),
                Published = DateTime.Now,
                Title = string.Empty.RandomString(10)
            };

            book.Pages.Add(
                new Page()
                {
                    Content = string.Empty.RandomString(1000),
                    PageNumber = 10
                });
            book.Pages.Add(
                new Page()
                {
                    Content = string.Empty.RandomString(1000),
                    PageNumber = 11
                });
            book.Pages.Add(
                new Page()
                {
                    Content = string.Empty.RandomString(1000),
                    PageNumber = 12
                });

            return book;
        }
    }
}
