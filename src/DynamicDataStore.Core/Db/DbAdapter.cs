using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Dynamic.Core;
using DynamicDataStore.Core.Model;
using DynamicDataStore.Core.Runtime;
using DynamicDataStore.Core.Util;

namespace DynamicDataStore.Core.Db
{
    public delegate void FinishedLoading();

    public class DbAdapter
    {
        public event FinishedLoading OnFinishedLoading;

        private BackgroundWorker bg = new BackgroundWorker();

        public DbSchemaBuilder DbSchemaBuilder { get; set; }

        public DynamicClassBuilder DynamicClassBuilder { get; set; }

        public dynamic Instance { get; set; }

        public Config Config { get; set; }

        public bool IsActive { get; set; }

        public Type ContextType { get; set; }

        public DbAdapter(string connectionString, bool extendedProperties = false)
        {
            Config = new Config();
            Config.ConnectionString = connectionString;
            Config.ExtendedProperties = extendedProperties;
            Config.SaveLibraryRunTime = true;
            Config.FilterSchemas = new List<string> {"'T'", "'C'"};
        }

        public void Load()
        {
            if (Config.Asynchronized)
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
                DbSchemaBuilder = new DbSchemaBuilder(Config);
                DynamicClassBuilder = new DynamicClassBuilder(Config);

                ContextType = DynamicClassBuilder.CreateContextType(DbSchemaBuilder.Tables, Config.ConnectionString);

                Instance = (DbContextBase)Activator.CreateInstance(ContextType);

                IsActive = true;
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp);

                IsActive = false;

                throw;
            }
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
                Utils.SetPropertyValue(masterObject, connectorField + Config.CollectionPostfixName, obj);
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
