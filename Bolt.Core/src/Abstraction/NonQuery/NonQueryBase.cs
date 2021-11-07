using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bolt.Core.Annotations;
using Bolt.Core.Storage;

namespace Bolt.Core.Abstraction
{
    public abstract class NonQueryBase : INonQuery
    {
        private SemaphoreSlim semaphore;
        private TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);
        private List<(DbTransaction transaction, SemaphoreSlim semaphore)> transactions;
        private List<(Type Type, object Row, Command Command, int TransactionNumber)> bufferedCommands;
        private static ConcurrentDictionary<Type, ConcurrentDictionary<CommandTypes, string>> commands = new ConcurrentDictionary<Type, ConcurrentDictionary<CommandTypes, string>>();
        private IQueryFormatter queryFormatter;
        public NonQueryBase(IQueryFormatter queryFormatter, int poolSize = 10)
        {
            this.queryFormatter = queryFormatter;
            bufferedCommands = new List<(Type Type, object Row, Command Command, int TransactionNumber)>();
            semaphore = new SemaphoreSlim(poolSize);
        }
        protected abstract DbConnection GetDbConnection();
        public bool CreateTransaction(int transactionCount = 1)
        {
            if (transactions != null || transactionCount <= 0)
            {
                return false;
            }
            transactions = new List<(DbTransaction transaction, SemaphoreSlim semaphore)>();
            for (int i = 0; i < transactionCount; i++)
            {
                DbConnection connection = GetDbConnection();
                connection.Open();
                transactions.Add((connection.BeginTransaction(), new SemaphoreSlim(1)));
            }
            return true;
        }
        public async Task CommitAsync()
        {
            if (transactions != null)
            {
                foreach (var transaction in transactions)
                {
                    await transaction.transaction.CommitAsync();
                }
                transactions = null;
            }
        }
        public async Task RollbackAsync()
        {
            if (transactions != null)
            {
                foreach (var transaction in transactions)
                {
                    await transaction.transaction.RollbackAsync();
                }
                transactions = null;
            }
        }
        public async Task ApplyAsync()
        {
            Queue<Task> insertQueue = new Queue<Task>();
            foreach (var i in bufferedCommands)
            {
                insertQueue.Enqueue(ExecuteNonQuery(i.Type, i.Row, i.Command, i.TransactionNumber));
            }
            await Task.WhenAll(insertQueue);
        }
        public async Task BatchedApplyAsync()
        {
            Queue<Task> insertQueue = new Queue<Task>();
            foreach (var i in GetBatchedCommand())
            {
                insertQueue.Enqueue(ExecuteNonQuery(i));
            }
            await Task.WhenAll(insertQueue);
        }
        public async Task<TableType> InsertAsync<TableType>(TableType row, int? transactionNumber = null)
        {
            Type tableType = typeof(TableType);
            return (TableType)await ExecuteNonQuery(tableType, row, GetCommand(tableType, CommandTypes.INSERT), transactionNumber ?? 0);
        }
        public void BufferedInsert<TableType>(TableType row, int? transactionNumber = null)
        {
            Type tableType = typeof(TableType);
            bufferedCommands.Add((tableType, row, GetCommand(tableType, CommandTypes.INSERT), transactionNumber ?? 0));
        }
        public async Task<object> UpdateAsync<TableType>(TableType row, Expression<Predicate<TableType>> predicate, int? transactionNumber = null)
        {
            Type tableType = typeof(TableType);
            return await ExecuteNonQuery(tableType, row, Command.GetConditionalCommand<TableType>(GetCommand(tableType, CommandTypes.UPDATE), predicate, queryFormatter), transactionNumber ?? 0);
        }
        public void BufferedUpdate<TableType>(TableType row, Expression<Predicate<TableType>> predicate, int? transactionNumber = null)
        {
            Type tableType = typeof(TableType);
            bufferedCommands.Add((tableType, row, Command.GetConditionalCommand<TableType>(GetCommand(tableType, CommandTypes.UPDATE), predicate, queryFormatter), transactionNumber ?? 0));
        }
        public async Task<object> DeleteAsync<TableType>(Expression<Predicate<TableType>> predicate, int? transactionNumber = null)
        {
            Type tableType = typeof(TableType);
            return await ExecuteNonQuery(tableType, null, Command.GetConditionalCommand<TableType>(GetCommand(tableType, CommandTypes.DELETE), predicate, queryFormatter), transactionNumber ?? 0);
        }
        public void BufferedDelete<TableType>(TableType row, Expression<Predicate<TableType>> predicate, int? transactionNumber = null)
        {
            Type tableType = typeof(TableType);
            bufferedCommands.Add((tableType, row, Command.GetConditionalCommand<TableType>(GetCommand(tableType, CommandTypes.DELETE), predicate, queryFormatter), transactionNumber ?? 0));
        }
        public async Task<object> TruncateAsync<TableType>(int? transactionNumber = null)
        {
            Type tableType = typeof(TableType);
            return await ExecuteNonQuery(tableType, null, GetCommand(tableType, CommandTypes.TRUNCATE), transactionNumber ?? 0);
        }
        public async Task<object> DeleteAllAsync<TableType>(int? transactionNumber = null)
        {
            Type tableType = typeof(TableType);
            return await ExecuteNonQuery(tableType, null, GetCommand(tableType, CommandTypes.DELETE), transactionNumber ?? 0);
        }
        public async Task<TableType[]> InsertManyAsync<TableType>(TableType[] rows, int? transactionNumber = null)
        {
            Command command = GetCommand(typeof(TableType), CommandTypes.INSERT);
            Queue<Task> insertQueue = new Queue<Task>();
            Type tableType = typeof(TableType);
            for (int i = 0, j = 0; i < rows.Length; i++, j++)
            {
                insertQueue.Enqueue(ExecuteNonQuery(tableType, rows[i], command, transactionNumber ?? (j = j >= (transactions?.Count ?? 0) ? 0 : j)));
            }
            await Task.WhenAll(insertQueue);
            return rows;
        }
        public async Task<TableType[]> BulkInsertAsync<TableType>(TableType[] rows, int? transactionNumber = null)
        {
            List<string> batched = GetBatchedCommand(rows, CommandTypes.INSERT).ToList();
            Queue<Task> insertQueue = new Queue<Task>();
            for (int i = 0, j = 0; i < batched.Count; i++, j++)
            {
                insertQueue.Enqueue(ExecuteNonQuery(batched[i], transactionNumber ?? (j = j >= (transactions?.Count ?? 0) ? 0 : j)));
            }
            await Task.WhenAll(insertQueue);
            return rows;
        }
        protected virtual IEnumerable<string> GetBatchedCommand<TableType>(TableType[] rows, CommandTypes commandType)
        {
            StringBuilder buffer = new StringBuilder();
            for (int i = 0, j = 0; i < rows.Length; i++)
            {
                if (i != 0 && i % 1000 == 0)
                {
                    j++;
                    yield return buffer.ToString();
                    buffer.Clear();
                }
                Type type = rows[i].GetType();
                Table tableInfo = TableMap.Current.GetTable(type);
                Command command = GetCommand(type, commandType);
                buffer.Append(command.GetSqlCommand(tableInfo, rows[i])).AppendLine(";");
            }
            if (buffer.Length > 0)
            {
                yield return buffer.ToString();
                buffer.Clear();
            }
        }
        protected virtual IEnumerable<string> GetBatchedCommand()
        {
            StringBuilder buffer = new StringBuilder();
            for (int i = 0, j = 0; i < bufferedCommands.Count; i++)
            {
                if (i != 0 && i % 1000 == 0)
                {
                    j++;
                    yield return buffer.ToString();
                    buffer.Clear();
                }
                var row = bufferedCommands[i];
                buffer.Append(row.Command.GetSqlCommand(TableMap.Current.GetTable(row.Row.GetType()), row.Row)).AppendLine(";");
            }
            if (buffer.Length > 0)
            {
                yield return buffer.ToString();
                buffer.Clear();
            }
            bufferedCommands.Clear();
        }
        protected virtual async Task<object> ExecuteNonQuery(Type type, object row, Command command, int iter)
        {
            SemaphoreSlim semaphore = null;
            DbConnection connection = null;
            DbTransaction transaction = null;
            if (transactions == null)
            {
                connection = GetDbConnection();
                semaphore = this.semaphore;
            }
            else
            {
                transaction = transactions[iter].transaction;
                connection = transaction.Connection;
                semaphore = transactions[iter].semaphore;
            }
            DbCommand cmd = connection.CreateCommand();
            cmd.CommandTimeout = (int)DefaultTimeout.TotalSeconds;
            cmd.CommandText = command.SqlCommand;
            Column surrogateKey = default;
            if (row != null)
            {
                foreach (var column in TableMap.Current.GetTable(type).GetColumns())
                {
                    if (column.ColumnFeatures.IsSurrogateKey)
                    {
                        if (surrogateKey == default)
                        {
                            surrogateKey = column;
                        }
                        else
                        {
                            throw new Exception("Composite Surrogate Keys are not supported");
                        }
                    }
                    var value = column.PropertyInfo.GetValue(row);
                    if (value == null)
                    {
                        var defaultAttribute = column.PropertyInfo.GetCustomAttribute<DefaultValueAttribute>();
                        DbParameter parameter = cmd.CreateParameter();
                        parameter.ParameterName = $"@{column.UniqueId}";
                        parameter.Value = defaultAttribute?.Value ?? DBNull.Value;
                        cmd.Parameters.Add(parameter);
                    }
                    else
                    {
                        DbParameter parameter = cmd.CreateParameter();
                        parameter.ParameterName = $"@{column.UniqueId}";
                        parameter.Value = value;
                        cmd.Parameters.Add(parameter);
                    }
                }
            }
            cmd.Transaction = transaction;
            semaphore.Wait();
            if (connection.State == ConnectionState.Closed)
            {
                await connection.OpenAsync();
            }
            var result = await cmd.ExecuteScalarAsync();
            if (transactions == null)
            {
                await connection.CloseAsync();
            }
            semaphore.Release();
            if (command.CommandType == CommandTypes.INSERT)
            {
                if (surrogateKey != default)
                {
                    surrogateKey.PropertyInfo.SetValue(row, Convert.ChangeType(result, surrogateKey.PropertyInfo.PropertyType));
                }
                return row;
            }
            else
            {
                return result;
            }
        }
        protected virtual async Task<object> ExecuteNonQuery(string command, int iter)
        {
            SemaphoreSlim semaphore = null;
            DbConnection connection = null;
            DbTransaction transaction = null;
            if (transactions == null)
            {
                connection = GetDbConnection();
                semaphore = this.semaphore;
            }
            else
            {
                transaction = transactions[iter].transaction;
                connection = transaction.Connection;
                semaphore = transactions[iter].semaphore;
            }
            DbCommand cmd = connection.CreateCommand();
            cmd.CommandTimeout = (int)DefaultTimeout.TotalSeconds;
            cmd.CommandText = command;
            cmd.Transaction = transaction;
            semaphore.Wait();
            if (connection.State == ConnectionState.Closed)
            {
                await connection.OpenAsync();
            }
            var result = await cmd.ExecuteScalarAsync();
            if (transactions == null)
            {
                await connection.CloseAsync();
            }
            semaphore.Release();
            return result;
        }
        protected virtual async Task<object> ExecuteNonQuery(string command)
        {
            SemaphoreSlim semaphore = null;
            DbConnection connection = null;
            connection = GetDbConnection();
            semaphore = this.semaphore;
            DbCommand cmd = connection.CreateCommand();
            cmd.CommandTimeout = (int)DefaultTimeout.TotalSeconds;
            cmd.CommandText = command;
            semaphore.Wait();
            if (connection.State == ConnectionState.Closed)
            {
                await connection.OpenAsync();
            }
            var result = await cmd.ExecuteScalarAsync();
            if (transactions == null)
            {
                await connection.CloseAsync();
            }
            semaphore.Release();
            return result;
        }
        protected virtual Command GetCommand(Type type, CommandTypes commandType)
        {
            string command = null;
            if (commands.TryGetValue(type, out ConcurrentDictionary<CommandTypes, string> _commands))
            {
                if (!_commands.TryGetValue(commandType, out command))
                {
                    command = CreateCommand(type, commandType);
                    _commands.TryAdd(commandType, command);
                }
            }
            else
            {
                command = CreateCommand(type, commandType);
                commands.TryAdd(type, new ConcurrentDictionary<CommandTypes, string> { [commandType] = command });
            }
            return new Command(command, commandType);
        }
        protected virtual string CreateCommand(Type type, CommandTypes commandType)
        {
            switch (commandType)
            {
                case CommandTypes.INSERT:
                    return CreateInsertCommand(type);
                case CommandTypes.UPDATE:
                    return CreateUpdateCommand(type);
                case CommandTypes.DELETE:
                    return CreateDeleteCommand(type);
                case CommandTypes.TRUNCATE:
                    return CreateTruncateCommand(type);
            }
            return null;
        }
        protected abstract string CreateInsertCommand(Type type);
        protected virtual string CreateUpdateCommand(Type type)
        {
            Table tableinfo = TableMap.Current.GetTable(type);
            StringBuilder cmd = new StringBuilder();
            StringBuilder values = new StringBuilder();
            Column surrogateKey = default;
            foreach (var column in tableinfo.GetColumns())
            {
                if (!column.ColumnFeatures.IsSurrogateKey)
                {
                    if (values.Length > 0)
                    {
                        values.Append(',');
                    }
                    values.Append(column.ColumnName).Append("=@").Append(column.UniqueId);
                }
                else
                {
                    if (surrogateKey == default)
                    {
                        surrogateKey = column;
                    }
                    else
                    {
                        throw new Exception("Composite Surrogate Key Is Not Supported");
                    }
                }
            }
            cmd.Append("UPDATE ").Append(tableinfo.FullyEvaluatedTableName).Append(" SET ").Append(values).Append(" ");
            return cmd.ToString();
        }
        protected virtual string CreateDeleteCommand(Type type)
        {
            Table tableinfo = TableMap.Current.GetTable(type);
            StringBuilder cmd = new StringBuilder();
            cmd.Append("DELETE FROM ").Append(tableinfo.FullyEvaluatedTableName);
            return cmd.ToString();
        }
        protected virtual string CreateTruncateCommand(Type type)
        {
            Table tableinfo = TableMap.Current.GetTable(type);
            StringBuilder cmd = new StringBuilder();
            cmd.Append("TRUNCATE TABLE ").Append(tableinfo.FullyEvaluatedTableName);
            return cmd.ToString();
        }

        public void Dispose()
        {
            semaphore.Dispose();
        }
    }
}