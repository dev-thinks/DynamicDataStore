using DynamicDataStore.Core.Db;
using DynamicDataStore.Core.Model;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Text.Json;

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
                var loggerFactory = LoggerFactory.Create(builder =>
                    {
                        builder.SetMinimumLevel(LogLevel.Trace)
                            .AddConsole(options =>
                            {
                                options.TimestampFormat = "[HH:mm:ss]";
                            });
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
                //var lstTables = _dbAdapter.GetDynamicEntities();

                if (_dbAdapter != null)
                {
                    var filtered = _dbAdapter.GetBy("T_EmailType", "s=>s.Code == \"NEW_REG_EMAIL\"");

                    Logger.LogTrace("Filtered record: {@FilterTable}", filtered);
                }
            }
            else
            {
                Logger.LogWarning("Dynamic data store is not successfully built.");
            }
        }
    }
}
