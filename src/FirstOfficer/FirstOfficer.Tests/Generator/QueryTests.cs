using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using FirstOfficer.Data;
using FirstOfficer.Data.Query;
using FirstOfficer.Data.Query.Npgsql;
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
            await DbConnection.QueryBooks(EntityBook.Includes.None, 
                a => 
                    a.Id == EntityBook.BookParameters.Value1,
                new EntityBook.BookParameterValues(1));
        }
    }



}

