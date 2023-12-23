using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using FirstOffice.Npgsql;
using FirstOfficer.Data;
using FirstOfficer.Generator.Extensions;
using FirstOfficer.Tests.Generator.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Pluralize.NET;
using DotNet.Testcontainers.Containers;
using Testcontainers.PostgreSql;

namespace FirstOfficer.Tests
{
    [TestFixture]
    public abstract class FirstOfficerTest
    {
        protected NpgsqlConnection DbConnection = null!;
        private PostgreSqlContainer? _postgresContainer;
        protected IServiceProvider? ServiceProvider { get; private set; }
        protected string? ConnectionString { get; private set; }

        protected FirstOfficerTest()
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }


        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {

            if (ServiceProvider != null)
            {
                return;
            }

            _postgresContainer = new PostgreSqlBuilder().WithDatabase("postgres").WithUsername("postgres").WithPassword("postgres").Build();
            await _postgresContainer.StartAsync();

            var builder = Host.CreateDefaultBuilder();

            builder.ConfigureHostConfiguration(a =>
                a.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddEnvironmentVariables()
            );

            var app = builder.ConfigureServices((builderContext, services) =>
            {
                services.AddLogging(a => a.AddConsole());

                services.AddTransient<IDbConnection>(a =>
                {
                    var config = a.GetService<IConfiguration>();
                    ConnectionString = _postgresContainer.GetConnectionString();
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
            await DbConnection.OpenAsync();

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

                await reader.CloseAsync();

                foreach (var table in tables)
                {
                    sql = $"DROP TABLE {table} CASCADE;";
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
            if (_postgresContainer != null)
            {
                await _postgresContainer.DisposeAsync();
            }
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
            Assert.That(book.ISBN, Is.EqualTo(dataTable.Rows[0]["isbn"]));
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

            Assert.That(page.BookId, Is.EqualTo(dataTable.Rows[0]["book_id"]));
            Assert.That(page.Content, Is.EqualTo(dataTable.Rows[0]["content"]));
            Assert.That(page.PageNumber, Is.EqualTo(dataTable.Rows[0]["page_number"]));

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

        protected void AssertManyToManySave<T1, T2>(List<T1> input1, List<T2> input2) where T1 : IEntity where T2 : IEntity
        {
            var type1Name = input1.GetType().GetGenericArguments()[0].Name;
            var type2Name = input2.GetType().GetGenericArguments()[0].Name;
            var prop1Name = new Pluralizer().Pluralize(type2Name);
            var prop2Name = new Pluralizer().Pluralize(type1Name);

            //assert T2 count 
            var sql = $"SELECT distinct author_id FROM {DataHelper.GetManyToManyTableName(type1Name, prop1Name, type2Name, prop2Name)}  WHERE {type1Name.ToSnakeCase()}_id in ({string.Join(",", input1.Select(a => a.Id))})";
            var command = DbConnection.CreateCommand();
            command.CommandText = sql;
            using var reader = command.ExecuteReader();
            var dataTable = new DataTable();
            dataTable.Load(reader);
            Assert.That(dataTable.Rows.Count, Is.EqualTo(input2.Count));

            //Assert the T2 entities were saved
            command.CommandText = "SELECT * FROM " + DataHelper.GetTableName<T2>() + " WHERE id in (" + string.Join(",", input2.Select(a => a.Id)) + ")";
            using var reader2 = command.ExecuteReader();
            var dataTable2 = new DataTable();
            dataTable2.Load(reader2);
            Assert.That(dataTable2.Rows.Count, Is.EqualTo(input2.Count));

            //assert T1 count
            sql = $"SELECT distinct book_id FROM {DataHelper.GetManyToManyTableName(type1Name, prop1Name, type2Name, prop2Name)}  WHERE {type2Name.ToSnakeCase()}_id in ({string.Join(",", input2.Select(a => a.Id))})";
            command.CommandText = sql;
            using var reader3 = command.ExecuteReader();
            var dataTable3 = new DataTable();
            dataTable3.Load(reader3);
            Assert.That(dataTable3.Rows.Count, Is.EqualTo(input1.Count));

            //Assert the T1 entities were saved
            command.CommandText = "SELECT * FROM " + DataHelper.GetTableName<T1>() + " WHERE id in (" + string.Join(",", input1.Select(a => a.Id)) + ")";
            using var reader4 = command.ExecuteReader();
            var dataTable4 = new DataTable();
            dataTable4.Load(reader4);
            Assert.That(dataTable4.Rows.Count, Is.EqualTo(input1.Count));

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
