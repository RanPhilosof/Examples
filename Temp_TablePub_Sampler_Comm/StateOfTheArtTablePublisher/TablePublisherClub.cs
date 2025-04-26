using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.Marshalling.IIUnknownCacheStrategy;

namespace StateOfTheArtTablePublisher
{
    public class A : ITablePublisherCacheMonitoring
    {
        public bool Available => true;

        public string TableName => "Ran";

        private Guid g = Guid.NewGuid();
        public Guid TableGuid => g;

        public static List<string> Headers = new List<string>() { "Index", "N1", "N2", "N3", "State" };

        //private List<List<string>> innerCachedTable = new List<List<string>>();

        private Queue<List<string>> q = new Queue<List<string>>();

        public List<List<string>> GetInnerCachedTable()
        {
            for (int i = 0; i < 5; i++)
                q.Enqueue(GenerateNewLine());

            while (q.Count > 10)
                q.Dequeue();

            var res = new List<List<string>>() { Headers };
            res.AddRange(q.ToList());

            return res;
        }

        public A()
        {
            TablePublisherClub.TablePublisherClubSingleton.Register(this);
        }

        private static int counter = 0;

        public List<string> GenerateNewLine()
        {
            counter++;

            return new List<string>() { $"{counter}", $"{Random.Shared.Next(1, 10)}", $"{Random.Shared.Next(11, 20)}", $"{Random.Shared.Next(21, 30)}", $"{strs[Random.Shared.Next(0, 4)]}"  };
        }

        public static List<string> strs = new List<string>() { "T1T1", "T3T1", "Clear", "Min", "Mean" };
    }


    [ApiController]
    [Route("api/[controller]")]
    public class TablePublisherMonitoringController : ControllerBase
    {
        [HttpGet("CachedTablesNames")]
        public ActionResult<List<ExtendedTableInfo>> GetCachedTablesNames()
        {
            return TablePublisherClub.TablePublisherClubSingleton.GetCachedTablesNames();
        }

        [HttpPost("CachedTables")]
        public ActionResult<List<Table>> GetCachedTables([FromBody] List<Guid> tablesGuid)
        {
            return TablePublisherClub.TablePublisherClubSingleton.GetCachedTables(tablesGuid);
        }
    }

    public class TableInfo
    {
        public string Name { get; set; }
        public Guid Guid { get; set; }
    }

    public class ExtendedTableInfo : TableInfo
    {
        public bool Available { get; set; }
    }

    public class Table
    {
        public TableInfo TableInfo { get; set; }

        public List<List<string>> TableData { get; set; }
    }

    public class TablePublisherClub
    {
        public static TablePublisherClub TablePublisherClubSingleton = new TablePublisherClub();

        private TablePublisherClub() { }

        private object locker = new object();

        public List<ITablePublisherCacheMonitoring> tablePublisherCacheMonitorings = new List<ITablePublisherCacheMonitoring>();
        public void Register(ITablePublisherCacheMonitoring tablePublisherCacheMonitoring)
        {
            lock (locker)
            {
                tablePublisherCacheMonitorings.Add(tablePublisherCacheMonitoring);
            }
        }

        public void Unregister(ITablePublisherCacheMonitoring tablePublisherCacheMonitoring)
        {
            lock (locker)
            {
                tablePublisherCacheMonitorings.Remove(tablePublisherCacheMonitoring);
            }
        }

        public List<ExtendedTableInfo> GetCachedTablesNames()
        {
            var tables = new List<ExtendedTableInfo>();

            lock (locker)
            {
                foreach (var tablePublisherCacheMonitoring in tablePublisherCacheMonitorings)
                {
                    var tableInfo = new ExtendedTableInfo();
                    
                    tableInfo.Name = tablePublisherCacheMonitoring.TableName;
                    tableInfo.Guid = tablePublisherCacheMonitoring.TableGuid;
                    tableInfo.Available = tablePublisherCacheMonitoring.Available;

                    tables.Add(tableInfo);
                }
            }

            return tables;
        }

        public List<Table> GetCachedTables(List<Guid> tablesGuid)
        {
            var tablesGuidHashset = tablesGuid.ToHashSet();

            var tables = new List<Table>();
            
            lock (locker)
            {
                foreach (var tablePublisherCacheMonitoring in tablePublisherCacheMonitorings)
                {
                    if (!tablesGuidHashset.Contains(tablePublisherCacheMonitoring.TableGuid))
                        continue;

                    var table = new Table();

                    if (tablePublisherCacheMonitoring.Available)
                    {
                        table.TableInfo = new TableInfo();
                        table.TableInfo.Name = tablePublisherCacheMonitoring.TableName;
                        table.TableInfo.Guid = tablePublisherCacheMonitoring.TableGuid;

                        table.TableData = tablePublisherCacheMonitoring?.GetInnerCachedTable() ?? new List<List<string>>();
                    }

                    tables.Add(table);
                }
            }

            return tables;
        }
    }
}
