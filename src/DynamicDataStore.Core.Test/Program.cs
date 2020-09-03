using System;
using System.Linq;
using System.Text.Json;
using DynamicDataStore.Core.Db;

namespace DynamicDataStore.Core.Test
{
    class Program
    {
        private static DbAdapter _dbAdapter;

        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        static void Main(string[] args)
        {
            _dbAdapter = new DbAdapter("Server=52.191.225.157;Database=Voltron;User ID=voltrondb;Password=voltron~adm1n;MultipleActiveResultSets=true;");

            _dbAdapter.Config.Asynchronized = false;
            _dbAdapter.Config.BrowsePrimaryKeyColumns = true;
            _dbAdapter.Config.BrowseForeignKeyColumns = true;
            _dbAdapter.OnFinishedLoading += Target;
            _dbAdapter.Load();

            Console.WriteLine("============COMPLETED===========");
            Console.ReadKey();
        }

        private static void Target()
        {
            if (_dbAdapter.IsActive)
            {
                var lstTables = _dbAdapter.DbSchemaBuilder.Tables.ToList();
                lstTables.ForEach(s => Console.WriteLine($"{s.Schema}.{s.Name}"));

                if (_dbAdapter != null)
                {
                    //var listEmailTypes = _dbAdapter.GetAll("T_EmailType");

                    //Console.WriteLine("============Entire Set===========");
                    //Console.WriteLine(JsonSerializer.Serialize(listEmailTypes, Options));

                    //Console.WriteLine();


                    var filtered2 = _dbAdapter.GetBy("T_EmailType", "s=>s.Code == \"NEW_REG_EMAIL\"");

                    //var filtered = ((IQueryable)_dbAdapter.Instance.T_EmailType)
                    //    .AsQueryable();
                        //.Where("s => s.Code == \"NEW_REG_EMAIL\"").ToDynamicList();

                    var txt = JsonSerializer.Serialize(filtered2, Options);

                    Console.WriteLine("============Filtered Set===========");
                    Console.WriteLine(txt);
                }
            }
            else
            {
                Console.WriteLine("Not Connected.");
            }
        }
    }
}
