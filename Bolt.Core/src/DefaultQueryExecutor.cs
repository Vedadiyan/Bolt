using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Bolt.Core.Abstraction;
using Bolt.Core.Storage;

namespace Bolt.Core
{
    public class DefaultQueryExecutor : IQueryExecutor
    {
        public async IAsyncEnumerable<Dictionary<Type, object>> ExecuteAsync(DbConnection connection, string sqlCommand, CommandType commandType, int timeout, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using (connection)
            {
                DbCommand command = connection.CreateCommand();
                command.CommandText = sqlCommand;
                command.CommandTimeout = timeout;
                command.CommandType = commandType;
                await connection.OpenAsync(cancellationToken);
                DbDataReader dataReader = await command.ExecuteReaderAsync(CommandBehavior.KeyInfo | CommandBehavior.CloseConnection, cancellationToken);
                if (dataReader.HasRows)
                {
                    IReadOnlyDictionary<string, IReadOnlyCollection<SchemaInfo>> queryTableMap = GetQueryTableMap(dataReader.GetSchemaTable(), dataReader.GetColumnSchema());
                    while (await dataReader.ReadAsync() && !cancellationToken.IsCancellationRequested)
                    {
                        ExpandoObject expandoObject = null;
                        Dictionary<Type, object> list = new Dictionary<Type, Object>();
                        foreach (var table in queryTableMap)
                        {
                            if (TableMap.Current.TryGetTableByTableName(table.Key, out Table _table))
                            {
                                IReadOnlyDictionary<string, Column> columns = TableMap.Current.GetColumnsByUniqueId(_table.Type);
                                object instance = _table.Instance();
                                foreach (var schemaInfo in table.Value)
                                {
                                    var value = dataReader[schemaInfo.ColumnName];
                                    if (columns.TryGetValue(schemaInfo.ColumnName, out Column column))
                                    {
                                        if (column.Processors != null)
                                        {
                                            foreach (var processor in column.Processors)
                                            {
                                                value = processor.Process(value);
                                            }
                                        }
                                        column.PropertyInfo.SetValue(instance, value != DBNull.Value ? value : null);
                                    }
                                    else
                                    {
                                        expandoObject ??= new ExpandoObject();

                                    }
                                }
                                list.Add(_table.Type, instance);
                            }
                        }
                        if (expandoObject != null)
                        {
                            list.Add(expandoObject.GetType(), expandoObject);
                        }
                        yield return list;
                    }
                }
            }
        }

        public IReadOnlyDictionary<string, IReadOnlyCollection<SchemaInfo>> GetQueryTableMap(DataTable dataTable, ReadOnlyCollection<DbColumn> columnSchema)
        {
            Dictionary<string, HashSet<SchemaInfo>> queryTableMap = new Dictionary<string, HashSet<SchemaInfo>>();
            foreach (DataRow row in dataTable.Rows)
            {
                string columnName = row["ColumnName"]?.ToString();
                string baseColumnName = row["BaseColumnName"]?.ToString();
                string baseSchemaName = row["BaseSchemaName"]?.ToString();
                string baseTableName = row["BaseTableName"]?.ToString();
                string fullyEvaluatedTableName = $"[{baseSchemaName}].[{baseTableName}]";
                if (!queryTableMap.ContainsKey(fullyEvaluatedTableName))
                {
                    queryTableMap.Add(fullyEvaluatedTableName, new HashSet<SchemaInfo>());
                }
                queryTableMap[fullyEvaluatedTableName].Add(new SchemaInfo(columnName, baseColumnName, baseSchemaName, baseTableName));
            }
            return queryTableMap.ToDictionary(x => x.Key, x => (IReadOnlyCollection<SchemaInfo>)x.Value);
        }
    }
}