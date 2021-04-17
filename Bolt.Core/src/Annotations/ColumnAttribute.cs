using System;

namespace Bolt.Core.Annotations
{
    public class ColumnAttribute : Attribute
    {
        public bool IsColumnInfoSpecified { get; }
        public string ColumnName { get; }
        public ColumnAttribute()
        {
            IsColumnInfoSpecified = false;
        }
        public ColumnAttribute(string columnName)
        {
            IsColumnInfoSpecified = true;
            ColumnName = columnName;
        }
    }
}