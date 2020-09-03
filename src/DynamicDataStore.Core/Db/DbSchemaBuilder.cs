using System;
using System.Collections.Generic;
using System.Data;
using DynamicDataStore.Core.Model;
using Microsoft.Data.SqlClient;

namespace DynamicDataStore.Core.Db
{
    public class DbSchemaBuilder
    {
        private readonly Config _config;

        public List<Table> Tables = new List<Table>();

        //private List<Column> Columns = new List<Column>();

        public DbSchemaBuilder(Config config)
        {
            try
            {
                _config = config;

                GetColumns();
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp);

                throw;
            }
        }

        private void GetColumns()
        {
            try
            {
                Table table = null;
                Column column = null;

                string tmpTableName = "";
                string tmpSchemaName = "";
                string filter = "";

                string extendedSql = (_config.ExtendedProperties == true) ?
    @"	,Convert(xml,(SELECT name,value FROM fn_listextendedproperty (NULL, 'schema', schema_name(t.schema_id), 'table', t.name, default, default) for xml raw)) as TableProperties
    ,Convert(xml,(SELECT name,value FROM fn_listextendedproperty (NULL, 'schema', schema_name(t.schema_id), 'table', t.name, 'Column', c.name) for xml raw)) as ColumnProperties
" : "";
                if (_config.FilterSchemas.Count > 0)
                {
                    filter = $"and schema_name(t.schema_id) in ({string.Join(",", _config.FilterSchemas.ToArray())})";

                    if (_config.IncludedTables.Count > 0)
                    {
                        filter = $"{filter} or t.name in ({string.Join(",", _config.IncludedTables.ToArray())})";
                    }
                }

                string sqlQuery =
    @"select
    schema_name(t.schema_id) TableSchema, t.name TableName, 
    c.name ColumnName, ex.value ColumnDescription, type_name(c.user_type_id) as ColumnType,	c.max_length as ColumnLength, c.is_nullable as ColumnIsNullable,
    pk.constraint_name PKName, pk_column_name PKColumnName, isnull(ordinal_position,0) PKPosition, isnull(pk.is_identity,0) PKIsIdentity,
    fk.fk_name FKName, fk.reference_schema ReferencedSchema, fk.referenced_object ReferencedTable, fk.referenced_column ReferencedColumn
    {0}
from sys.tables t      
left outer join 
sys.columns c on t.object_id = c.object_id
left outer join
sys.extended_properties ex on ex.minor_id = c.column_id
left outer join 
(select 
    object_name(constraint_object_id) fk_name	
    ,fkc.parent_object_id
    ,fkc.parent_column_id
    , schema_name(t.schema_id) reference_schema
    ,object_name(referenced_object_id) referenced_object
    ,(select name from sys.columns c where c.object_id = fkc.referenced_object_id and c.column_id = fkc.referenced_column_id) as referenced_column
 from sys.foreign_key_columns fkc inner join sys.tables t on t.object_id = fkc.referenced_object_id
) fk 
on fk.parent_object_id = t.object_id and c.column_id = fk.parent_column_id
left outer join 
(select 
    c.is_identity as is_identity,
    c.is_rowguidcol as is_rowguidcol,
    t.object_id as table_object_id, s.name as table_schema, t.name as table_name
    , k.name as constraint_name, k.type_desc as constraint_type
    , c.name as pk_column_name, ic.key_ordinal AS ordinal_position          
 from sys.key_constraints as k
 join sys.tables as t
 on t.object_id = k.parent_object_id
 join sys.schemas as s
 on s.schema_id = t.schema_id
 join sys.index_columns as ic
 on ic.object_id = t.object_id
 and ic.index_id = k.unique_index_id
 join sys.columns as c
 on c.object_id = t.object_id
 and c.column_id = ic.column_id
 where k.type_desc = 'PRIMARY_KEY_CONSTRAINT'
) pk on pk.table_object_id = t.object_id and pk.pk_column_name = c.name
where t.name <> 'sysdiagrams' {1}
order by TableSchema, TableName";

                SqlConnection connection = new SqlConnection(_config.ConnectionString);
                SqlCommand command = new SqlCommand();

                command.CommandType = CommandType.Text;
                command.Connection = connection;
                command.CommandText = string.Format(sqlQuery, extendedSql, filter);

                try
                {
                    connection.Open();
                    var reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        if (reader["TableSchema"].ToString() == "Audit" || reader["TableSchema"].ToString() == "dbo")
                        {
                            continue;
                        }

                        if ($"{tmpSchemaName}.Tables{tmpTableName}" !=
                            $"{reader["TableSchema"].ToString()}.Tables{reader["TableName"].ToString()}")
                        {
                            tmpSchemaName = reader["TableSchema"].ToString();
                            tmpTableName = reader["TableName"].ToString();

                            table = new Table();
                            table.Schema = tmpSchemaName;
                            table.Name = tmpTableName;
                            Tables.Add(table);
                        }

                        if (table != null)
                        {
                            column = new Column();
                            column.Table = table;
                            column.TableSchema = reader["TableSchema"].ToString();
                            column.TableName = reader["TableName"].ToString();
                            column.ColumnName = _config.PropertyPreFixName + reader["ColumnName"].ToString();
                            column.ColumnDescription = reader["ColumnDescription"].ToString();
                            column.ColumnType = reader["ColumnType"].ToString();
                            column.ColumnLength = Convert.ToInt32(reader["ColumnLength"]);
                            column.ColumnIsNullable = Convert.ToBoolean(reader["ColumnIsNullable"]);

                            column.PkName = reader["PKName"].ToString();
                            column.PkColumnName = reader["PKColumnName"].ToString();
                            column.PkPosition = Convert.ToInt32(reader["PKPosition"]);
                            column.PkIsIdentity = Convert.ToBoolean(reader["PKIsIdentity"]);

                            column.FkName = reader["FKName"].ToString();
                            column.ReferencedSchema = reader["ReferencedSchema"].ToString();
                            column.ReferencedTable = reader["ReferencedTable"].ToString();
                            column.ReferencedColumn = reader["ReferencedColumn"].ToString();

                            if (_config.ExtendedProperties)
                            {
                                table.TableProperties = reader["TableProperties"].ToString();
                                column.ColumnProperties = reader["ColumnProperties"].ToString();
                            }

                            //Columns.Add(column);
                            table.Columns.Add(column);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);

                    throw;
                }
                finally
                {
                    connection.Close();
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp);

                throw;
            }
        }
    }
}
