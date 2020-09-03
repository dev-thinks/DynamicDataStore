using DynamicDataStore.Core.Db;
using DynamicDataStore.Core.Model;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Collections.Generic;
using System.Text.Json;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace DynamicDataStore.Core.Test
{
    class Program
    {
        private static DbAdapter _dbAdapter;

        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        private static ILogger Logger
        {
            get
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.Console()
                    .CreateLogger();

                var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.ClearProviders();
                    builder.AddSerilog();
                });

                return loggerFactory.CreateLogger("TestApp");
            }
        }

        static void Main(string[] args)
        {
            var config = new Config
            {
                Asynchronized = false,
                BrowsePrimaryKeyColumns = true,
                BrowseForeignKeyColumns = true,
                SaveLibraryRunTime = true,
                FilterSchemas = new List<string> {"'T'", "'C'"},
                ExtendedProperties = false,
                ConnectionString =
                    "Server=52.191.225.157;Database=Voltron;User ID=voltrondb;Password=voltron~adm1n;MultipleActiveResultSets=true;"
            };

            Logger.LogTrace("Building dynamic data store");

            _dbAdapter = new DbAdapter(config, Logger);

            _dbAdapter.OnFinishedLoading += Target;
            _dbAdapter.Load();

            Logger.LogDebug("Completed");
        }

        private static void Target()
        {
            if (_dbAdapter.IsActive)
            {
                if (_dbAdapter != null)
                {
                    var tableName = "T_EmailType";
                    var predicate = "s=>s.Code == \"NEW_REG_EMAIL\"";

                    var filtered = _dbAdapter.GetBy(tableName, predicate);

                    Logger.LogTrace("Filtered table: {Table} Predicate: {Predicate} Result: {@FilterTable}", tableName,
                        predicate, filtered);
                }
            }
            else
            {
                Logger.LogWarning("Dynamic data store is not successfully built.");
            }
        }
    }
}
