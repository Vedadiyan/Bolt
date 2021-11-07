using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bolt.Core.Abstraction;
using Bolt.Core.Annotations;
using Bolt.Core.Processors;

namespace Bolt.Core.Storage
{

    public class ColumnFeatures
    {
        public bool IsSurrogateKey { get; init; }
    }
    public readonly struct Column
    {
        public string ColumnName { get; }
        public string UniqueId { get; }
        public PropertyInfo PropertyInfo { get; }
        public ColumnFeatures ColumnFeatures { get; }
        public IReadOnlyCollection<IProcessor> Processors => processors.AsReadOnly();
        private readonly List<IProcessor> processors;
        public Column(string columnName, PropertyInfo propertyInfo, ColumnFeatures columnFeatures)
        {
            ColumnName = columnName;
            PropertyInfo = propertyInfo;
            ColumnFeatures = columnFeatures;
            processors = new List<IProcessor>();
            UniqueId = $"{propertyInfo.DeclaringType.FullName}_{propertyInfo.Name}".Replace(".", "_");
        }
        public void AddProcessor(IProcessor processor)
        {
            processors.Add(processor);
        }

        public override bool Equals(object obj)
        {
            return obj is Column column &&
                   ColumnName == column.ColumnName &&
                   UniqueId == column.UniqueId &&
                   EqualityComparer<PropertyInfo>.Default.Equals(PropertyInfo, column.PropertyInfo) &&
                   EqualityComparer<ColumnFeatures>.Default.Equals(ColumnFeatures, column.ColumnFeatures) &&
                   EqualityComparer<IReadOnlyCollection<IProcessor>>.Default.Equals(Processors, column.Processors) &&
                   EqualityComparer<List<IProcessor>>.Default.Equals(processors, column.processors);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ColumnName, UniqueId, PropertyInfo, ColumnFeatures, Processors, processors);
        }
        public static bool operator ==(Column a, Column b)
        {
            return a.Equals(b);
        }
        public static bool operator !=(Column a, Column b)
        {
            return !a.Equals(b);
        }
    }

    public readonly struct Table
    {
        public Type Type { get; }
        public string Schema { get; }
        public string TableName { get; }
        public string FullyEvaluatedTableName { get; }
        public Table(Type type, string shcema, string tableName)
        {
            Type = type;
            Schema = shcema;
            TableName = tableName;
            FullyEvaluatedTableName = $"[{Schema}].[{TableName}]";
        }
        public Column[] GetColumns()
        {
            if (TableMap.Current.TryGetColumns(Type, out IReadOnlyDictionary<string, Column> columns))
            {
                return columns.Values.ToArray();
            }
            return null;
        }
        public string GetFullyEvalulatedColumnName(Column column)
        {
            return $"{FullyEvaluatedTableName}.{column.ColumnName}";
        }
        public object Instance()
        {
            return Activator.CreateInstance(Type);
        }

    }
    public class TableMap
    {
        public static TableMap Current { get; } = new TableMap();
        private Dictionary<Type, Table> setA;
        private Dictionary<string, Type> setB;
        private Dictionary<string, Type> setC;
        private Dictionary<string, Dictionary<string, Column>> setD;
        private Dictionary<string, Dictionary<string, Column>> setE;
        private TableMap()
        {
            setA = new Dictionary<Type, Table>();
            setB = new Dictionary<string, Type>();
            setC = new Dictionary<string, Type>();
            setD = new Dictionary<string, Dictionary<string, Column>>();
            setE = new Dictionary<string, Dictionary<string, Column>>();
        }
        public void Add(Table table, Column[] columns)
        {
            setA.Add(table.Type, table);
            setB.Add(table.Type.FullName, table.Type);
            setC.Add(table.FullyEvaluatedTableName, table.Type);
            setD.Add(table.Type.FullName, columns.ToDictionary(x => x.PropertyInfo.Name, x => x));
            setE.Add(table.Type.FullName, columns.ToDictionary(x => x.UniqueId, x => x));
        }
        public Table GetTable<T>()
        {
            return setA[typeof(T)];
        }
        public Table GetTable(Type type)
        {
            return setA[type];
        }
        public Table GetTableByTypeName(string typeName)
        {
            return setA[setB[typeName]];
        }
        public IReadOnlyDictionary<string, Column> GetColumnsByTypeName(Type type)
        {
            return setD[type.FullName];
        }
        public IReadOnlyDictionary<string, Column> GetColumnsByUniqueId(Type type)
        {
            return setE[type.FullName];
        }
        public bool TryGetTable<T>(out Table table)
        {
            return setA.TryGetValue(typeof(T), out table);
        }
        public bool TryGetTable(Type type, out Table table)
        {
            return setA.TryGetValue(type, out table);
        }
        public bool TryGetTableByTableName(string tableName, out Table table)
        {
            if (setC.TryGetValue(tableName, out Type type))
            {
                return setA.TryGetValue(type, out table);
            }
            table = default;
            return false;
        }
        public bool TryGetColumn(string typeName, string columnName, out Column column)
        {
            if (setB.TryGetValue(typeName, out Type type))
            {
                if (setD.TryGetValue(type.FullName, out Dictionary<string, Column> columns))
                {
                    return columns.TryGetValue(columnName, out column);
                }
            }
            column = default;
            return false;
        }
        public bool TryGetColumns(Type type, out IReadOnlyDictionary<string, Column> columns)
        {
            if (setD.TryGetValue(type.FullName, out Dictionary<string, Column> _columns))
            {
                columns = _columns;
                return true;
            }
            columns = null;
            return false;
        }
    }
    public readonly struct StoredProcedure
    {
        public string StoredProcedureName { get; }
        public string SchemaName { get; }
        public string StoredProcedureFullName { get; }
        public IReadOnlyDictionary<string, PropertyInfo> Parameters { get; }
        public StoredProcedure(string storedProcedureName, string schemaName, string storedProcedureFullName, IReadOnlyDictionary<string, PropertyInfo> parameters)
        {
            StoredProcedureName = storedProcedureName;
            SchemaName = schemaName;
            StoredProcedureFullName = storedProcedureFullName;
            Parameters = parameters;
        }

    }
    public class StoredProcedureMap
    {
        public static StoredProcedureMap Current { get; } = new StoredProcedureMap();
        private Dictionary<Type, StoredProcedure> storedProcedures;
        private StoredProcedureMap() {
            storedProcedures = new Dictionary<Type, StoredProcedure>();
        }
        public void AddStoredProcedure(Type type, StoredProcedure storedProcedure) {
            storedProcedures.Add(type, storedProcedure);
         }
        public bool TryGetStoredProcedure(Type type, out StoredProcedure storedProcedure)
        {
            return storedProcedures.TryGetValue(type, out storedProcedure);
        }
    }
    public class DSS
    {
        public static void RegisterTableStructure<T>()
        {
            RegisterTableStructure(typeof(T));
        }
        public static void RegisterTableStructure(Type type)
        {
            TableAttribute tableAttribute = type.GetCustomAttribute<TableAttribute>();
            string fullyEvaluatedTableName = tableAttribute?.FullTableName() ?? type.Name;
            Table table = new Table(type, tableAttribute.SchemaName, tableAttribute.TableName);
            var properties = type.GetProperties().Select(x => (Attribute: x.GetCustomAttribute<ColumnAttribute>(), Self: x)).Where(x => x.Attribute != null).ToList();
            Column[] columns = new Column[properties.Count];
            for (int i = 0; i < properties.Count; i++)
            {
                var property = properties[i];
                Column column = new Column(property.Attribute.ColumnName, property.Self, new ColumnFeatures { IsSurrogateKey = property.Self.GetCustomAttribute<SurrogateKeyAttribute>() != null });
                JsonAttribute jsonAttribute = property.Self.GetCustomAttribute<JsonAttribute>();
                if (jsonAttribute != null)
                {
                    column.AddProcessor(new JsonProcessor(property.Self.PropertyType));
                }
                columns[i] = column;
            }
            TableMap.Current.Add(table, columns);
        }
        public static void RegisterStoredProcedure(Type type)
        {
            StoredProcedureAttribute storedProcedureAttribute = type.GetCustomAttribute<StoredProcedureAttribute>();
            Dictionary<string, PropertyInfo> storedProcedureParameters = null;
            if (storedProcedureAttribute.ParametersType != null)
            {
                storedProcedureParameters = new Dictionary<string, PropertyInfo>();
                foreach (var i in storedProcedureAttribute.ParametersType.GetProperties())
                {
                    ParameterAttribute parameterAttribute = i.GetCustomAttribute<ParameterAttribute>();
                    if (parameterAttribute != null)
                    {
                        storedProcedureParameters.Add(parameterAttribute.Name ?? i.Name, i);
                    }
                }
            }
            StoredProcedure storedProcedure = new StoredProcedure(storedProcedureAttribute.StoredProcedureName, storedProcedureAttribute.SchemaName, storedProcedureAttribute.FullStoredProcedureName(), storedProcedureParameters);
            StoredProcedureMap.Current.AddStoredProcedure(type, storedProcedure);
            string fullyEvaluatedTableName = storedProcedureAttribute?.FullStoredProcedureName() ?? type.Name;
            Table table = new Table(type, storedProcedureAttribute.SchemaName, storedProcedureAttribute.StoredProcedureName);
            var properties = type.GetProperties().Select(x => (Attribute: x.GetCustomAttribute<ColumnAttribute>(), Self: x)).Where(x => x.Attribute != null).ToList();
            Column[] columns = new Column[properties.Count];
            for (int i = 0; i < properties.Count; i++)
            {
                var property = properties[i];
                Column column = new Column(property.Attribute.ColumnName, property.Self, new ColumnFeatures { IsSurrogateKey = property.Self.GetCustomAttribute<SurrogateKeyAttribute>() != null });
                JsonAttribute jsonAttribute = property.Self.GetCustomAttribute<JsonAttribute>();
                if (jsonAttribute != null)
                {
                    column.AddProcessor(new JsonProcessor(property.Self.PropertyType));
                }
                columns[i] = column;
            }
            TableMap.Current.Add(table, columns);
        }
    }

}