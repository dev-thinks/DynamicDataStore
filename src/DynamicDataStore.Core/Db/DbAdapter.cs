using DynamicDataStore.Core.Model;
using DynamicDataStore.Core.Runtime;
using DynamicDataStore.Core.Util;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Dynamic.Core;

namespace DynamicDataStore.Core.Db
{
    public delegate void FinishedLoading();

    public class DbAdapter
    {
        public event FinishedLoading OnFinishedLoading;

        public dynamic Instance { get; set; }

        private BackgroundWorker bg = new BackgroundWorker();

        private DbSchemaBuilder DbSchemaBuilder { get; set; }

        private readonly Config _config;
        private readonly ILogger _logger;

        public bool IsActive { get; set; }

        public DbAdapter(Config configuration, ILogger logger)
        {
            _config = configuration;
            _logger = logger;
        }

        public void Load()
        {
            if (_config.Asynchronized)
            {
                bg.DoWork += bg_DoWork;
                bg.RunWorkerCompleted += bg_RunWorkerCompleted;
                bg.RunWorkerAsync();
            }
            else
            {
                bg_DoWork(this, null);

                OnFinishedLoading?.Invoke();
            }
        }

        void bg_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            OnFinishedLoading?.Invoke();
        }

        void bg_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                IsActive = RefreshEntity(false);
            }
            catch (Exception exp)
            {
                _logger.LogError(exp, "Error while adapting the dynamic entities.");

                IsActive = false;

                throw;
            }
        }

        public bool RefreshEntity(bool isReload = true)
        {
            _logger.LogTrace("[{IsRefresh}] Start fetching tables and properties with config {@Configuration}.",
                isReload ? "REFRESH" : "INITIAL", _config);

            DbSchemaBuilder = new DbSchemaBuilder(_config, _logger);

            DynamicClassBuilder dynamicClassBuilder = new DynamicClassBuilder(_config, _logger);

            Type contextType = dynamicClassBuilder.CreateContextType(DbSchemaBuilder.Tables, _config.ConnectionString);

            Instance = (DbContextBase)Activator.CreateInstance(contextType);

            _logger.LogTrace("[{IsRefresh}] Dynamic data store is ready. Entity count: {Count}",
                isReload ? "REFRESH" : "INITIAL", DbSchemaBuilder.Tables.Count);

            return true;
        }

        public List<Table> GetDynamicEntities()
        {
            return DbSchemaBuilder.Tables.ToList();
        }

        public dynamic GetAll(string tableName)
        {
            var returnValue = Utils.GetPropertyValue(this.Instance, tableName);

            return returnValue;
        }

        public List<dynamic> GetBy(string tableName, string predicate, params object[] args)
        {
            var entityValue = Utils.GetPropertyValue(this.Instance, tableName) as IQueryable;

            var queryEntity = ((IQueryable)entityValue)!
                .AsQueryable()
                .Where(ParsingConfig.DefaultEFCore21, predicate, args)
                .ToDynamicList();

            return queryEntity;
        }

        public List<dynamic> GetFirstBy(string tableName, string predicate, params object[] args)
        {
            var queryEntity = ((IQueryable)this.Instance[tableName])
                .AsQueryable()
                .FirstOrDefault(ParsingConfig.DefaultEFCore21, predicate, args);

            return queryEntity;
        }

        public dynamic New(dynamic dbSet)
        {
            Type type = Utils.GetListType(dbSet);
            return Activator.CreateInstance(type);
        }

        public dynamic New(string tableName)
        {
            var obj = Utils.GetPropertyValue(Instance, tableName);
            Type type = Utils.GetListType(obj);

            return Activator.CreateInstance(type);
        }

        public void Add(dynamic obj)
        {
            dynamic dbSet = Utils.GetPropertyValue(Instance, obj.GetType().Name);

            dbSet.Add(obj);
        }

        public void Add(dynamic masterObject, dynamic detailObject, string connectorField)
        {
            var obj = Utils.GetPropertyValue(masterObject, connectorField);
            if (obj == null)
            {
                Type type = Utils.GetPropertyType(masterObject, connectorField);
                obj = Activator.CreateInstance(type);
                Utils.SetPropertyValue(masterObject, connectorField + _config.CollectionPostfixName, obj);
            }

            obj.Add(detailObject);
        }

        public void Add(dynamic masterObject, dynamic detailObject)
        {
            Table table = DbSchemaBuilder.Tables.FirstOrDefault(o => o.VariableName == detailObject.GetType().Name);

            if (table != null)
            {
                var query = (from o in table.Columns
                    where o.IsFk && o.ReferencedTable == masterObject.GetType().Name
                    select o).ToList();

                if (query.Count() == 1)
                {
                    Column column = query.FirstOrDefault();

                    if (column != null)
                    {
                        Add(masterObject, detailObject, column.ColumnName);
                    }
                    else
                    {
                        throw new Exception("Cannot find any connection between two objects");
                    }
                }
                else
                {
                    throw new Exception("there is a logical problem connecting two objects");
                }
            }
        }

        public void Delete(dynamic obj)
        {
            dynamic dbSet = Utils.GetPropertyValue(Instance, obj.GetType().Name);
            dbSet.Remove(obj);
        }

        public void Save()
        {
            Instance.SaveChanges();
        }

        public void Cancel()
        {
            Instance.CancelChanges();
        }
    }
}
