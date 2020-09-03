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
                ConnectionString = "DB"
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


                    var newEmailType = _dbAdapter.New(tableName);

                    newEmailType.Name = "Unit Test Case";
                    newEmailType.Code = "UNIT_TEST_CASE";
                    newEmailType.MaxRetryCount = 1;
                    newEmailType.RowStatusTypeId = 0;
                    newEmailType.CreatedBy = 2;

                    _dbAdapter.Add(newEmailType);

                    _dbAdapter.Save();

                    var isRefreshed = _dbAdapter.RefreshEntity();

                    if (isRefreshed)
                    {
                        // new data after changes
                        predicate = "s=>s.Code == \"UNIT_TEST_CASE\"";

                        var filtered2 = _dbAdapter.GetBy(tableName, predicate);

                        Logger.LogTrace("Filtered table after refresh: {Table} Predicate: {Predicate} Result: {@FilterTable}",
                            tableName,
                            predicate, filtered2);
                    }
                }
            }
            else
            {
                Logger.LogWarning("Dynamic data store is not successfully built.");
            }
        }
    }
}
