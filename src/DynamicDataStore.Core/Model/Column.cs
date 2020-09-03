using DynamicDataStore.Core.Util;

namespace DynamicDataStore.Core.Model
{
    public class Column
    {
        public Table Table { get; set; }

        //Table
        public string TableSchema { get; set; }

        public string TableName { get; set; }

        //Column
        public string ColumnName { get; set; }

        public string ColumnDescription { get; set; }

        public string ColumnType { get; set; }

        public int ColumnLength { get; set; }

        public bool ColumnIsNullable { get; set; }

        //PK
        public string PkName { get; set; }

        public string PkColumnName { get; set; }

        public int PkPosition { get; set; }

        public bool PkIsIdentity { get; set; }

        //FK
        public string FkName { get; set; }

        public string ReferencedSchema { get; set; }

        public string ReferencedTable { get; set; }

        public string ReferencedColumn { get; set; }

        //XML Extended Properties
        public string ColumnProperties { get; set; }

        //Getters
        public DataType DataType
        {
            get { return Utils.DbTypeToDataType(ColumnType, ColumnIsNullable); }
        }

        public bool IsPk
        {
            get { return !string.IsNullOrEmpty(PkName); }
        }

        public bool IsFk
        {
            get { return !string.IsNullOrEmpty(FkName); }
        }

        public bool Browsable
        {
            get { return !((this.IsFk) || (this.IsPk && !this.PkIsIdentity)); }
        }

        public string ReferencedVariableName
        {
            get
            {
                if (string.IsNullOrEmpty(this.ReferencedSchema) || this.ReferencedSchema == "dbo")
                {
                    return this.ReferencedTable;
                }
                else
                {
                    return $"{this.ReferencedSchema}_{this.ReferencedTable}";
                }
            }
        }

        public override string ToString()
        {
            return ColumnName;
        }
    }
}
