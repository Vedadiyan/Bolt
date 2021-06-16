using System;
using Bolt.Core.Annotations;
using Bolt.Core.Storage;
using Bolt.MySql;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            DSS.RegisterTableStructure<ScheduleModel>();   
            NonQuery nonQuery = new NonQuery("Server=192.168.136.129;User ID=root;Password=toor;Database=EventsDB");
            nonQuery.UpdateAsync<ScheduleModel>(new ScheduleModel {
                FriendlyName = "g",
                Service = "",
                PayloadJSON = "{}",
                TriggerMethod = "once",
                TimesJSON = "{}",
                DaysJSON = "{}"

            }, x=> x.Id == 1).Wait();
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
}
