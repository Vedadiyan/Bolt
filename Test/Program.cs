using System;
using System.Data.Common;
using Bolt.Core.Abstraction;
using Bolt.Core.Annotations;
using Bolt.SqlServer;
using Bolt.SqlServer.Commands;

namespace Test
{
    public class TestDbContext : DbContext
    {
        protected string Connection => "Data Source=192.168.150.41;Initial Catalog=FinancialAnalysisDb;Integrated Security=true;";
        protected override int Timeout => 3000;

        protected override DbConnection GetConnection()
        {
            throw new NotImplementedException();
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            TestDbContext testDbContext = new TestDbContext();
            IQuery query = new StoredProcedure<Test, Bolt.Core.Void>();
            var z = testDbContext.ExecuteQueryAsync(query).Result;
            Console.WriteLine("Hello World!");
        }
    }
    [Table("Tbl01_ServiceCustomer", "DIT")]
    public class ServiceCustomer {
        [Column("SerC_ID")]
        public long Id {get; set;}
        [Column("SerC_UserName")]
        public string Username {get; set;}
    }
    [StoredProcedure("Sp500_GetMarketData", "DIT")]
    public class Test {
        [Column("ID")]
        public long Id {get;set;}
        [Column]
        public string DisplayName {get; set;}
    }
    public class TestP {}
}

