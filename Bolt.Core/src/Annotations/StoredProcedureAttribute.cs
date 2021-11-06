using System;

namespace Bolt.Core.Annotations
{
    public class StoredProcedureAttribute : Attribute
    {
        public bool StoredProcedureInfoSpecified { get; }
        public string StoredProcedureName { get; }
        public string SchemaName { get; }
        public StoredProcedureAttribute()
        {
            StoredProcedureInfoSpecified = false;
        }
        public StoredProcedureAttribute(string storedProcedurename)
        {
            StoredProcedureInfoSpecified = true;
            StoredProcedureName = storedProcedurename;
        }
        public StoredProcedureAttribute(string storedProcedureName, string schemaName) : this(storedProcedureName)
        {
            SchemaName = schemaName;
        }
        public string FullStoredProcedureName()
        {
            if (!string.IsNullOrEmpty(SchemaName))
            {
                return $"[{SchemaName}].[{StoredProcedureName}]";
            }
            else
            {
                return StoredProcedureName;
            }
        }
    }
}