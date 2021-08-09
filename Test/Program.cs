using System;
using System.Data.SqlClient;
using System.Reflection;
using System.Threading.Tasks;
using Bolt.Core.Abstraction;
using Bolt.Core.Annotations;
using Bolt.Core.Interpretation;
using Bolt.Core.Mappers;
using Bolt.Core.Storage;
using Bolt.SqlServer;
using Bolt.SqlServer.Commands;
namespace Test
{
    public class TestDbContext : DbContext
    {
        protected override string ConnectionString { get; }

        protected override int Timeout => 3000;
    }
    class Program
    {
        static void Main(string[] args)
        {

            TestDbContext testDbContext = new TestDbContext();
            IQuery s = new Query<ScheduleModel>().Select<Bolt.Core.Void>(x=> DBO.Function<Bolt.Core.Void>("MAX(id)")).Select().Top(100).Distinct();
            Query<ScheduleModel> ss = (Query<ScheduleModel>)s;
            var zz = ss.GetSqlQuery();
            Console.WriteLine("Hello World!");
        }
    }
   
    [Table("Schedule")]
    public class ScheduleModel
    {
        [Column("id")]
        public long Id { get; set; }
        [Column("`name`")]
        public string FriendlyName { get; set; }
        [Column("`trigger`")]
        public string TriggerMethod { get; set; }
        [Column("times_of_day")]
        public string TimesJSON { get; set; }
        [Column("days_of_week")]
        public string DaysJSON { get; set; }
        [Column("start")]
        public DateTime Start { get; set; }
        [Column("end")]
        public DateTime End { get; set; }
        [Column("service")]
        public string Service { get; set; }
        [Column("payload")]
        public string PayloadJSON { get; set; }

    }
    [Table("[VideoContents]")]
    public class VideoContent
    {
        [PrimaryKey]
        [Column]
        public Guid Id { get; set; }
        [Column]
        public DateTimeOffset CreatedAt { get; set; }
        [Column]
        public string Title { get; set; }
        [Column]
        public Guid RelatedVideoContentId { get; set; }
        [Column]
        public Guid RelatedVideoContent2Id { get; set; }
        [Column]
        public int? Season { get; set; }
        [Column]
        public int? Episode { get; set; }
        [Column]
        public TimeSpan? Duration { get; set; }

    }
    [Table("[VideoContentTags]")]
    public class VideoContentTag
    {
        [PrimaryKey]
        [Column]
        public Guid Id { get; set; }
        [Column]
        public Guid ContentId { get; set; }
        [Column]
        public Guid TagId { get; set; }
    }
    [Table("[VideoContentCategories]")]
    public class VideoContentCategory
    {
        [PrimaryKey]
        [Column]
        public Guid Id { get; set; }
        [Column]
        public Guid ContentId { get; set; }
        [Column]
        public Guid CategoryId { get; set; }
    }
    [Table("[Tags]")]
    public class Tag
    {
        [PrimaryKey]
        [Column]
        public Guid Id { get; set; }
        [Column]
        public string Title { get; set; }
    }
    [Table("[Categories]")]
    public class Category
    {
        [PrimaryKey]
        [Column]
        public Guid Id { get; set; }
        [Column]
        public string Title { get; set; }
    }
}
