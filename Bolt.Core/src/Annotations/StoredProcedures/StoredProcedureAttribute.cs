using System;

namespace Bolt.Core.Annotations
{
    public class StoredProcedureAttribute : Attribute
    {
        public bool StoredProcedureInfoSpecified { get; }
        public string StoredProcedureName { get; }
        public string SchemaName { get; }
        public Type ParametersType { get; }
        public StoredProcedureAttribute()
        {
            StoredProcedureInfoSpecified = false;
        }
        public StoredProcedureAttribute(Type parametersType) : this()
        {
            ParametersType = parametersType;
        }
        public StoredProcedureAttribute(string storedProcedurename)
        {
            StoredProcedureInfoSpecified = true;
            StoredProcedureName = storedProcedurename;
        }
        public StoredProcedureAttribute(string storedProcedurename, Type parametersType) : this(storedProcedurename)
        {
            ParametersType = parametersType;
        }
        public StoredProcedureAttribute(string storedProcedureName, string schemaName) : this(storedProcedureName)
        {
            SchemaName = schemaName;
        }
        public StoredProcedureAttribute(string storedProcedureName, string schemaName, Type parametersType) : this(storedProcedureName, schemaName)
        {
            ParametersType = parametersType;
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