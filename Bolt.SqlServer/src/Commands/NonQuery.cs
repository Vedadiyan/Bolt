using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bolt.Core.Storage;

namespace Bolt.SqlServer.Commands
{
    // THIS CLASS IS UNDER DEVELOPMENT 
    internal class NonQuery
    {
        private SemaphoreSlim semaphore;
        private readonly string connectionString;
        private TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);
        private List<(SqlTransaction transaction, SemaphoreSlim semaphore)> transactions;
        private List<(object Row, Command Command, int TransactionNumber)> bufferedCommands;
        private static ConcurrentDictionary<Type, ConcurrentDictionary<CommandTypes, string>> commands = new ConcurrentDictionary<Type, ConcurrentDictionary<CommandTypes, string>>();
        public NonQuery(string connectionString, int poolSize = 10)
        {
            bufferedCommands = new List<(object Row, Command Command, int TransactionNumber)>();
            semaphore = new SemaphoreSlim(poolSize);
            this.connectionString = connectionString;
        }
        public bool CreateTransaction(int transactionCount = 1)
        {
            if (transactions != null || transactionCount <= 0)
            {
                return false;
            }
            transactions = new List<(SqlTransaction transaction, SemaphoreSlim semaphore)>();
            for (int i = 0; i < transactionCount; i++)
            {
                SqlConnection connection = new SqlConnection(connectionString);
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
                insertQueue.Enqueue(executeNonQuery(i.Row, i.Command, i.TransactionNumber));
            }
            await Task.WhenAll(insertQueue);
        }
        public async Task BatchedApplyAsync()
        {
            Queue<Task> insertQueue = new Queue<Task>();
            foreach (var i in getBatchedCommand())
            {
                insertQueue.Enqueue(executeNonQuery(i));
            }
            await Task.WhenAll(insertQueue);
        }
        public async Task<TableType> InsertAsync<TableType>(TableType row, int? transactionNumber = null)
        {
            return (TableType)await executeNonQuery(row, getCommand(typeof(TableType), CommandTypes.INSERT), transactionNumber ?? 0);
        }
        public void BufferedInsert<TableType>(TableType row, int? transactionNumber = null)
        {
            bufferedCommands.Add((row, getCommand(typeof(TableType), CommandTypes.INSERT), transactionNumber ?? 0));
        }
        public async Task<object> UpdateAsync<TableType>(TableType row, Expression<Predicate<TableType>> predicate, int? transactionNumber = null)
        {
            return await executeNonQuery(row, Command.GetConditionalCommand<TableType>(getCommand(typeof(TableType), CommandTypes.UPDATE), predicate), transactionNumber ?? 0);
        }
        public void BufferedUpdate<TableType>(TableType row, Expression<Predicate<TableType>> predicate, int? transactionNumber = null)
        {
            bufferedCommands.Add((row, Command.GetConditionalCommand<TableType>(getCommand(typeof(TableType), CommandTypes.UPDATE), predicate), transactionNumber ?? 0));
        }
        public async Task<object> DeleteAsync<TableType>(Expression<Predicate<TableType>> predicate, int? transactionNumber = null)
        {
            return await executeNonQuery(null, Command.GetConditionalCommand<TableType>(getCommand(typeof(TableType), CommandTypes.DELETE), predicate), transactionNumber ?? 0);
        }
        public void BufferedDelete<TableType>(TableType row, Expression<Predicate<TableType>> predicate, int? transactionNumber = null)
        {
            bufferedCommands.Add((row, Command.GetConditionalCommand<TableType>(getCommand(typeof(TableType), CommandTypes.DELETE), predicate), transactionNumber ?? 0));
        }
        public async Task<object> TruncateAsync<TableType>(int? transactionNumber = null)
        {
            return await executeNonQuery(null, getCommand(typeof(TableType), CommandTypes.TRUNCATE), transactionNumber ?? 0);
        }
        public async Task<object> DeleteAllAsync<TableType>(int? transactionNumber = null)
        {
            return await executeNonQuery(null, getCommand(typeof(TableType), CommandTypes.DELETE), transactionNumber ?? 0);
        }
        public async Task<TableType[]> InsertManyAsync<TableType>(TableType[] rows, int? transactionNumber = null)
        {
            Command command = getCommand(typeof(TableType), CommandTypes.INSERT);
            Queue<Task> insertQueue = new Queue<Task>();
            for (int i = 0, j = 0; i < rows.Length; i++, j++)
            {
                insertQueue.Enqueue(executeNonQuery(rows[i], command, transactionNumber ?? (j = j >= (transactions?.Count ?? 0) ? 0 : j)));
            }
            await Task.WhenAll(insertQueue);
            return rows;
        }
        public async Task<TableType[]> BulkInsertAsync<TableType>(TableType[] rows, int? transactionNumber = null)
        {
            List<string> batched = getBatchedCommand(rows, CommandTypes.INSERT).ToList();
            Queue<Task> insertQueue = new Queue<Task>();
            for (int i = 0, j = 0; i < batched.Count; i++, j++)
            {
                insertQueue.Enqueue(executeNonQuery(batched[i], transactionNumber ?? (j = j >= (transactions?.Count ?? 0) ? 0 : j)));
            }
            await Task.WhenAll(insertQueue);
            return rows;
        }
        private IEnumerable<string> getBatchedCommand<TableType>(TableType[] rows, CommandTypes commandType)
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
                TableInfo tableInfo = DSS.GetTableInfo(type);
                Command command = getCommand(type, commandType);
                buffer.Append(command.GetSqlCommand(tableInfo, rows[i])).AppendLine(";");
            }
            if (buffer.Length > 0)
            {
                yield return buffer.ToString();
                buffer.Clear();
            }
        }
        private IEnumerable<string> getBatchedCommand()
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
                buffer.Append(row.Command.GetSqlCommand(DSS.GetTableInfo(row.Row.GetType()), row.Row)).AppendLine(";");
            }
            if (buffer.Length > 0)
            {
                yield return buffer.ToString();
                buffer.Clear();
            }
            bufferedCommands.Clear();
        }
        private async Task<object> executeNonQuery(object row, Command command, int iter)
        {
            SemaphoreSlim semaphore = null;
            SqlConnection connection = null;
            SqlTransaction transaction = null;
            if (transactions == null)
            {
                connection = new SqlConnection(connectionString);
                semaphore = this.semaphore;
            }
            else
            {
                transaction = transactions[iter].transaction;
                connection = transaction.Connection;
                semaphore = transactions[iter].semaphore;
            }
            SqlCommand cmd = connection.CreateCommand();
            cmd.CommandTimeout = (int)DefaultTimeout.TotalSeconds;
            cmd.CommandText = command.SqlCommand;
            ColumnInfo surrogateKey = null;
            if (row != null)
            {
                foreach (var column in DSS.GetTableInfo(row.GetType()).Columns)
                {
                    if (column.Value.SurrogateKey != null)
                    {
                        if (surrogateKey == null)
                        {
                            surrogateKey = column.Value;
                        }
                        else
                        {
                            throw new Exception("Composite Surrogate Keys are not supported");
                        }
                    }
                    cmd.Parameters.AddWithValue($"@{column.Value.Name}", column.Value.PropertyInfo.GetValue(row));
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
                if (surrogateKey != null)
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
        private async Task<object> executeNonQuery(string command, int iter)
        {
            SemaphoreSlim semaphore = null;
            SqlConnection connection = null;
            SqlTransaction transaction = null;
            if (transactions == null)
            {
                connection = new SqlConnection(connectionString);
                semaphore = this.semaphore;
            }
            else
            {
                transaction = transactions[iter].transaction;
                connection = transaction.Connection;
                semaphore = transactions[iter].semaphore;
            }
            SqlCommand cmd = connection.CreateCommand();
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
        private async Task<object> executeNonQuery(string command)
        {
            SemaphoreSlim semaphore = null;
            SqlConnection connection = null;
            connection = new SqlConnection(connectionString);
            semaphore = this.semaphore;
            SqlCommand cmd = connection.CreateCommand();
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
        private Command getCommand(Type type, CommandTypes commandType)
        {
            string command = null;
            if (commands.TryGetValue(type, out ConcurrentDictionary<CommandTypes, string> _commands))
            {
                if (!_commands.TryGetValue(commandType, out command))
                {
                    command = createCommand(type, commandType);
                    _commands.TryAdd(commandType, command);
                }
            }
            else
            {
                command = createCommand(type, commandType);
                commands.TryAdd(type, new ConcurrentDictionary<CommandTypes, string> { [commandType] = command });
            }
            return new Command(command, commandType);
        }
        private string createCommand(Type type, CommandTypes commandType)
        {
            switch (commandType)
            {
                case CommandTypes.INSERT:
                    return createInsertCommand(type);
                case CommandTypes.UPDATE:
                    return createUpdateCommand(type);
                case CommandTypes.DELETE:
                    return createDeleteCommand(type);
                case CommandTypes.TRUNCATE:
                    return createTruncateCommand(type);
            }
            return null;
        }
        private string createInsertCommand(Type type)
        {
            TableInfo tableinfo = DSS.GetTableInfo(type);
            StringBuilder cmd = new StringBuilder();
            StringBuilder columns = new StringBuilder();
            StringBuilder values = new StringBuilder();
            ColumnInfo surrogateKey = null;
            foreach (var column in tableinfo.Columns)
            {
                if (column.Value.SurrogateKey == null)
                {
                    if (columns.Length > 0)
                    {
                        columns.Append(',');
                        values.Append(',');
                    }
                    columns.Append(column.Value.Name);
                    values.Append("@").Append(column.Value.Name);
                }
                else
                {
                    if (surrogateKey == null)
                    {
                        surrogateKey = column.Value;
                    }
                    else
                    {
                        throw new Exception("Composite Surrogate Key Is Not Supported");
                    }
                }
            }
            cmd.Append("INSERT INTO ").Append(tableinfo.FullyEvaluatedTableName).Append("(").Append(columns).Append(")").Append(" VALUES (").Append(values).Append("); SELECT scope_identity() AS ID");
            return cmd.ToString();
        }
        private string createUpdateCommand(Type type)
        {
            TableInfo tableinfo = DSS.GetTableInfo(type);
            StringBuilder cmd = new StringBuilder();
            StringBuilder values = new StringBuilder();
            ColumnInfo surrogateKey = null;
            foreach (var column in tableinfo.Columns)
            {
                if (column.Value.SurrogateKey == null)
                {
                    if (values.Length > 0)
                    {
                        values.Append(',');
                    }
                    values.Append(column.Value.Name).Append("=@").Append(column.Value.Name);
                }
                else
                {
                    if (surrogateKey == null)
                    {
                        surrogateKey = column.Value;
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
        private string createDeleteCommand(Type type)
        {
            TableInfo tableinfo = DSS.GetTableInfo(type);
            StringBuilder cmd = new StringBuilder();
            cmd.Append("DELETE FROM ").Append(tableinfo.FullyEvaluatedTableName);
            return cmd.ToString();
        }
        private string createTruncateCommand(Type type)
        {
            TableInfo tableinfo = DSS.GetTableInfo(type);
            StringBuilder cmd = new StringBuilder();
            cmd.Append("TRUNCATE TABLE ").Append(tableinfo.FullyEvaluatedTableName);
            return cmd.ToString();
        }
    }

}