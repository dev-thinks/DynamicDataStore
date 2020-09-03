using System.Collections.Generic;

namespace DynamicDataStore.Core.Model
{
    public class Config
    {
        public bool BrowsePrimaryKeyColumns { get; set; }

        public bool BrowseForeignKeyColumns { get; set; }

        public bool Asynchronized { get; set; }

        public string CollectionPostfixName { get; set; }

        public string ObjectPostfixName { get; set; }

        public List<string> FilterSchemas { get; set; }

        public List<string> IncludedTables { get; set; }

        public string ConnectionString { get; set; }

        public bool ExtendedProperties { get; set; }

        public string PropertyPreFixName { get; set; }

        public bool SaveLibraryRunTime { get; set; }

        public Config()
        {
            BrowsePrimaryKeyColumns = false;
            BrowseForeignKeyColumns = false;
            Asynchronized = false;
            CollectionPostfixName = "List";
            ObjectPostfixName = "Object";
            PropertyPreFixName = "";
            FilterSchemas = new List<string>();
            IncludedTables = new List<string>();
            SaveLibraryRunTime = false;
        }
    }
}
