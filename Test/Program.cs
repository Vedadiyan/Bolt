using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
using Bolt.Core.Abstraction;
using Bolt.Core.Annotations;
using Bolt.Core.Mappers;
using Bolt.Core.Storage;
using Bolt.SqlServer.Commands;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<Benchmark>();

            Console.WriteLine("Hello World!");
        }
    }
    public class Benchmark
    {
        private static Guid id = Guid.Parse("6ed37ce0-9e75-4e24-9b1f-fe678a3667e4");
        public Benchmark()
        {
            DSS.RegisterTableStructure<VideoContent>();
            DSS.RegisterTableStructure<VideoContentTag>();
            DSS.RegisterTableStructure<VideoContentCategory>();
            DSS.RegisterTableStructure<Tag>();
            DSS.RegisterTableStructure<Category>();
        }

        [Benchmark]
        public async Task Bolt()
        {
            IQuery contentQuery =
                new Query<VideoContent>()
                .LefJoin<VideoContent, VideoContentTag>(x => x.Left.Id == x.Right.ContentId)
                .LefJoin<VideoContent, VideoContentCategory>(x => x.Left.Id == x.Right.ContentId)
                .LefJoin<VideoContentTag, Tag>(x => x.Left.TagId == x.Right.Id)
                .LefJoin<VideoContentCategory, Category>(x => x.Left.CategoryId == x.Right.Id)
                .Where<VideoContent>(x => x.Id == id)
                .Select();

            Bolt.Core.Mappers.ResultSet contentResultSet = new ResultSet(contentQuery);
            await contentResultSet.LoadAsync("");
        }

        [Benchmark]
        public async Task ADONet()
        {
            using (SqlConnection con = new SqlConnection(""))
            {
                con.Open();
                SqlCommand cmd = con.CreateCommand();
                cmd.CommandText = @"
                        SELECT [VideoContent].Id AS C16377, 
        [VideoContent].CreatedAt AS C31804, 
        [VideoContent].Title AS C23139, 
        [VideoContent].RelatedVideoContentId AS C70478, 
        [VideoContent].RelatedVideoContent2Id AS C72451, 
        [VideoContent].Season AS C25459, 
        [VideoContent].Episode AS C27674, 
        [VideoContent].Duration AS C30643, 
        [VideoContentTag].Id AS C21937, 
        [VideoContentTag].ContentId AS C40097, 
        [VideoContentTag].TagId AS C28439, 
        [VideoContentCategory].Id AS C36856, 
        [VideoContentCategory].ContentId AS C59402, 
        [VideoContentCategory].CategoryId AS C62950, 
        [Tag].Id AS C3901, 
        [Tag].Title AS C7594, 
        [Category].Id AS C11248, 
        [Category].Title AS C16987 FROM [VideoContents] AS [VideoContent]
        LEFT JOIN [VideoContentTags] AS [VideoContentTag] ON ([VideoContent].Id = [VideoContentTag].ContentId)
        LEFT JOIN [VideoContentCategories] AS [VideoContentCategory] ON ([VideoContent].Id = [VideoContentCategory].ContentId)
        LEFT JOIN [Tags] AS [Tag] ON ([VideoContentTag].TagId = [Tag].Id)
        LEFT JOIN [Categories] AS [Category] ON ([VideoContentCategory].CategoryId = [Category].Id) WHERE ([VideoContent].Id = '6ed37ce0-9e75-4e24-9b1f-fe678a3667e4')
                        ";
                await cmd.ExecuteReaderAsync();
            }
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
