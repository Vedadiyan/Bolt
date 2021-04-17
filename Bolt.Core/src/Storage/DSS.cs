using System;
using System.Collections.Generic;
using System.Reflection;
using Bolt.Core.Annotations;

namespace Bolt.Core.Storage
{
    public record ColumnInfo(string Name, string FullyEvaluatedColumnName, string Alias, string TableKey, PropertyInfo PropertyInfo, SurrogateKeyAttribute SurrogateKey);
    public record TableInfo(Type type, string TableName, string FullyEvaluatedTableName, Dictionary<string, ColumnInfo> Columns);
    public class DSS
    {
        private static Dictionary<string, TableInfo> tableStructureStorage;
        private static Dictionary<string, ColumnInfo> columnMap;
        static DSS()
        {
            tableStructureStorage = new Dictionary<string, TableInfo>();
            columnMap = new Dictionary<string, ColumnInfo>();
        }
        public static TableInfo Randomize(TableInfo tableInfo)
        {
            Dictionary<string, ColumnInfo> columns = new Dictionary<string, ColumnInfo>();
            foreach (var i in tableInfo.Columns)
            {
                columns.Add(i.Key, i.Value with { Alias = "Value_" + DateTime.Now.Ticks.ToString() });
            }
            TableInfo _tableInfo = tableInfo with
            {
                Columns = columns
            };
            return _tableInfo;
        }
        public static void RegisterTableStructure<T>()
        {
            Type type = typeof(T);
            TableAttribute tableAttribute = type.GetCustomAttribute<TableAttribute>();
            string fullyEvaluatedTableName = tableAttribute?.FullTableName() ?? type.Name;
            Dictionary<string, ColumnInfo> columnInfos = new Dictionary<string, ColumnInfo>();
            foreach (PropertyInfo property in type.GetProperties())
            {
                ColumnAttribute columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
                if (columnAttribute != null)
                {
                    string columnName = columnAttribute.ColumnName ?? property.Name;
                    string fullyEvaluatedColumnName = fullyEvaluatedTableName + "." + columnName;
                    string columnHash = getHash(fullyEvaluatedColumnName);
                    ColumnInfo columnInfo = new ColumnInfo(columnName, fullyEvaluatedColumnName, columnHash, type.Name, property, property.GetCustomAttribute<SurrogateKeyAttribute>());
                    columnInfos.Add(property.Name, columnInfo);
                    columnMap.Add(columnHash, columnInfo);
                }
            }
            tableStructureStorage.Add(type.Name, new TableInfo(type, tableAttribute.TableName ?? type.Name, fullyEvaluatedTableName, columnInfos));
        }
        public static void RegisterTableStructure(Type type)
        {
            TableAttribute tableAttribute = type.GetCustomAttribute<TableAttribute>();
            string fullyEvaluatedTableName = tableAttribute?.FullTableName() ?? type.Name;
            Dictionary<string, ColumnInfo> columnInfos = new Dictionary<string, ColumnInfo>();
            foreach (PropertyInfo property in type.GetProperties())
            {
                ColumnAttribute columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
                if (columnAttribute != null)
                {
                    string columnName = columnAttribute.ColumnName ?? property.Name;
                    string fullyEvaluatedColumnName = fullyEvaluatedTableName + "." + columnName;
                    string columnHash = getHash(fullyEvaluatedColumnName);
                    ColumnInfo columnInfo = new ColumnInfo(columnName, fullyEvaluatedColumnName, columnHash, type.Name, property, property.GetCustomAttribute<SurrogateKeyAttribute>());
                    columnInfos.Add(property.Name, columnInfo);
                    columnMap.Add(columnHash, columnInfo);
                }
            }
            tableStructureStorage.Add(type.Name, new TableInfo(type, tableAttribute.TableName ?? type.Name, fullyEvaluatedTableName, columnInfos));
        }
        public static TableInfo GetTableInfo<T>()
        {
            return tableStructureStorage[typeof(T).Name];
        }
        public static TableInfo GetTableInfo(Type type)
        {
            return tableStructureStorage[type.Name];
        }
        public static Boolean TryGetTableInfo(Type type, out TableInfo tableInfo)
        {
            return tableStructureStorage.TryGetValue(type.Name, out tableInfo);
        }
        public static TableInfo GetTableInfo(string name)
        {
            return tableStructureStorage[name];
        }
        public static Boolean TryGetColumnInfo(string name, out ColumnInfo columnInfo)
        {
            return columnMap.TryGetValue(name, out columnInfo);
        }
        private static string getHash(string str)
        {
            int value = 0;
            for (int iter = 0; iter < str.Length; iter++)
            {
                value += ((iter + 1) * (int)str[iter]);
            }
            return "C" + value.ToString();
        }
    }
}