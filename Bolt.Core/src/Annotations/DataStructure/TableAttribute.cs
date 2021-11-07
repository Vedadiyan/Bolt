using System;

namespace Bolt.Core.Annotations
{
    public class TableAttribute : Attribute
    {
        public bool TableInfoSpecified { get; }
        public string TableName { get; }
        public string SchemaName { get; }
        public TableAttribute()
        {
            TableInfoSpecified = false;
        }
        public TableAttribute(string tableName)
        {
            TableInfoSpecified = true;
            TableName = tableName;
        }
        public TableAttribute(string tableName, string schemaName) : this(tableName)
        {
            SchemaName = schemaName;
        }
        public string FullTableName()
        {
            if (!string.IsNullOrEmpty(SchemaName))
            {
                return $"[{SchemaName}].[{TableName}]";
            }
            else
            {
                return TableName;
            }
        }
    }
}