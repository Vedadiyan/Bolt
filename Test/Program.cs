using System;
using Bolt.Core.Abstraction;
using Bolt.Core.Annotations;
using Bolt.MySql;
using Bolt.MySql.Commands;

namespace Test
{
    public class TestDbContext : DbContext
    {
        protected override string ConnectionString => "Server=192.168.233.200;port=7005;User ID=root;Password=;Database=TestDb";
        protected override int Timeout => 3000;
    }
    class Program
    {
        static void Main(string[] args)
        {
            TestDbContext testDbContext = new TestDbContext();
            IQuery query = new Query<User>().Select();
            var test = testDbContext.ExecuteQueryAsync(query).Result;
            Console.WriteLine("Hello World!");
        }
    }
    [Table("User")]
    public class User
    {
        [Column("id")]
        [SurrogateKey]
        [PrimaryKey]
        public ulong Id { get; set; }
        [Column("`first_name`")]
        public string FirstName { get; set; }
        [Column("`last_name`")]
        public string LastName { get; set; }
        [Column("email")]
        public string Email { get; set; }
        [Column("data")]
        [Json]
        public TestJsonClass Data { get; set; }
    }
    public class TestJsonClass
    {
        public string PhoneNumber { get; set; }
    }
}
