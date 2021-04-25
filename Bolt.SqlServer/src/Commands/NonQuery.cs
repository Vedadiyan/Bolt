using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Bolt.Core.Interpretation;
using Bolt.Core.Storage;

namespace Bolt.SqlServer.Commands
{

    /*
        This class is under development 
        NOT TESTED
    */
    
    public class NonQuery
    {
        private List<List<string>> buffer;
        private readonly string connectionString;
        public NonQuery(string connectionString)
        {
            buffer = new List<List<string>>();
            buffer.Add(new List<string>());
            this.connectionString = connectionString;
        }
        public static async Task<T> Insert<T>(T entity)
        {
            TableInfo tableinfo = DSS.GetTableInfo<T>();
            StringBuilder cmd = new StringBuilder();
            StringBuilder columns = new StringBuilder();
            StringBuilder values = new StringBuilder();
            ColumnInfo surrogateKey = null;
            using (SqlConnection connection = new SqlConnection())
            {
                SqlCommand command = connection.CreateCommand();
                foreach (var column in tableinfo.Columns)
                {
                    if (column.Value.SurrogateKey == null)
                    {
                        if (columns.Length > 0)
                        {
                            columns.Append(',');
                            values.Append(',');
                        }
                        columns.Append(column.Value.FullyEvaluatedColumnName);
                        values
                        .Append(column.Value.FullyEvaluatedColumnName)
                        .Append("=@")
                        .Append(column.Value.Alias);
                        command.Parameters.Add(new SqlParameter("@" + column.Value.Alias, column.Value.PropertyInfo.GetValue(entity)));
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
                cmd.Append("INSERT INTO ").Append(tableinfo.FullyEvaluatedTableName).Append("(").Append(columns).Append(")").Append(" VALUES(").Append(values).Append("; SELECT scope_identity() AS ID");
                command.CommandText = cmd.ToString();
                await connection.OpenAsync();
                object id = await command.ExecuteScalarAsync();
                if (surrogateKey != null)
                {
                    surrogateKey.PropertyInfo.SetValue(entity, Convert.ChangeType(id, surrogateKey.PropertyInfo.PropertyType));
                }
                return entity;
            }
        }
        public static async Task BulkInsert<T>(T[] entities)
        {
            TableInfo tableinfo = DSS.GetTableInfo<T>();
            StringBuilder values = new StringBuilder();
            DataTable dataTable = new DataTable();
            using (SqlConnection connection = new SqlConnection())
            {
                SqlCommand command = connection.CreateCommand();
                foreach (var column in tableinfo.Columns)
                {
                    dataTable.Columns.Add(column.Value.FullyEvaluatedColumnName);
                }
                foreach (var i in entities)
                {
                    List<object> row = new List<object>();
                    foreach (var column in tableinfo.Columns)
                    {
                        row.Add(column.Value.PropertyInfo.GetValue(i));
                    }

                    dataTable.Rows.Add(row);
                }
                await connection.OpenAsync();
                using (SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(connection))
                {
                    sqlBulkCopy.DestinationTableName = tableinfo.FullyEvaluatedTableName;
                    await sqlBulkCopy.WriteToServerAsync(dataTable);
                }
            }
        }
        public static async Task<int> Update<T>(T entity, Expression<Predicate<T>> condition, String con)
        {
            try
            {
                TableInfo tableinfo = DSS.GetTableInfo<T>();
                StringBuilder cmd = new StringBuilder();
                StringBuilder values = new StringBuilder();
                StringBuilder expression = new StringBuilder();
                ExpressionReader reader = new ExpressionReader(condition.Body, ExpressionTypes.FullyEvaluated, new Stack<ExpressionType>(), expression);
                ColumnInfo surrogateKey = null;
                using (SqlConnection connection = new SqlConnection(con))
                {
                    SqlCommand command = connection.CreateCommand();
                    foreach (var column in tableinfo.Columns)
                    {
                        if (column.Value.SurrogateKey == null)
                        {
                            if (values.Length > 0)
                            {
                                values.Append(',');
                            }
                            values
                            .Append(column.Value.FullyEvaluatedColumnName)
                            .Append("=@")
                            .Append(column.Value.Alias);
                            var val = column.Value.PropertyInfo.GetValue(entity);
                            command.Parameters.Add(new SqlParameter("@" + column.Value.Alias, val));
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
                    cmd.Append("UPDATE ").Append(tableinfo.FullyEvaluatedTableName).Append(" SET ").Append(values).Append(" WHERE ").Append(expression.ToString());
                    command.CommandText = cmd.ToString();
                    await connection.OpenAsync();
                    int result = await command.ExecuteNonQueryAsync();
                    return result;
                }
            }
            catch //(Exception ex)
            {
                return -1;
            }
        }
        public void Update<T>(T entity, Expression<Predicate<T>> condition)
        {
            TableInfo tableinfo = DSS.GetTableInfo<T>();
            StringBuilder cmd = new StringBuilder();
            StringBuilder values = new StringBuilder();
            StringBuilder expression = new StringBuilder();
            ExpressionReader reader = new ExpressionReader(condition.Body, ExpressionTypes.FullyEvaluated, new Stack<ExpressionType>(), expression);
            ColumnInfo surrogateKey = null;
            foreach (var column in tableinfo.Columns)
            {
                if (column.Value.SurrogateKey == null)
                {
                    if (values.Length > 0)
                    {
                        values.Append(',');
                    }
                    var val = column.Value.PropertyInfo.GetValue(entity);
                    values
                    .Append(column.Value.FullyEvaluatedColumnName)
                    .Append("=");
                    if (val is string || val is DateTime)
                    {
                        values.Append("'").Append(val).Append("'");
                    }
                    else
                    {
                        values.Append(val);
                    }
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
            cmd.Append("UPDATE ").Append(tableinfo.FullyEvaluatedTableName).Append(" SET ").Append(values).Append(" WHERE ").Append(expression.ToString());
            if (buffer[buffer.Count - 1].Count != 1000)
            {
                buffer[buffer.Count - 1].Add(cmd.ToString());
            }
            else
            {
                buffer.Add(new List<string> { cmd.ToString() });
            }
        }
        public async Task Apply() {
            List<string> commands = new List<string>();
            foreach(var i in buffer) {
                StringBuilder sb = new StringBuilder();
                foreach(var x in i){
                    sb.AppendLine(x).Append(";");
                }
            }
            using(SqlConnection connection = new SqlConnection(connectionString)) {
                connection.Open();
                foreach(var i in commands) {
                    SqlCommand cmd = connection.CreateCommand();
                    cmd.CommandText = i;
                    var result = await cmd.ExecuteNonQueryAsync();    
                    Console.WriteLine(result);
                }
            }
        }
        public static async Task<int> BulkUpdate<T>(T entity, Expression<Predicate<T>> condition)
        {
            TableInfo tableinfo = DSS.GetTableInfo<T>();
            StringBuilder cmd = new StringBuilder();
            StringBuilder values = new StringBuilder();
            StringBuilder expression = new StringBuilder();
            ExpressionReader reader = new ExpressionReader(condition.Body, ExpressionTypes.FullyEvaluated, new Stack<ExpressionType>(), expression);
            ColumnInfo surrogateKey = null;
            using (SqlConnection connection = new SqlConnection())
            {
                SqlCommand command = connection.CreateCommand();
                foreach (var column in tableinfo.Columns)
                {
                    if (column.Value.SurrogateKey == null)
                    {
                        if (values.Length > 0)
                        {
                            values.Append(',');
                        }
                        values
                        .Append(column.Value.FullyEvaluatedColumnName)
                        .Append("=@")
                        .Append(column.Value.Alias);
                        command.Parameters.Add(new SqlParameter("@" + column.Value.Alias, column.Value.PropertyInfo.GetValue(entity)));
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
                cmd.Append("UPDATE ").Append(tableinfo.FullyEvaluatedTableName).Append(" SET ").Append(values).Append(" WHERE ").Append(expression.ToString());
                command.CommandText = cmd.ToString();
                await connection.OpenAsync();
                int result = await command.ExecuteNonQueryAsync();
                Console.WriteLine(result);
                return result;
            }
        }
        public static async Task<int> Delete<T>(Expression<Predicate<T>> condition)
        {
            TableInfo tableinfo = DSS.GetTableInfo<T>();
            StringBuilder cmd = new StringBuilder();
            StringBuilder expression = new StringBuilder();
            ExpressionReader reader = new ExpressionReader(condition.Body, ExpressionTypes.FullyEvaluated, new Stack<ExpressionType>(), expression);
            using (SqlConnection connection = new SqlConnection())
            {
                SqlCommand command = connection.CreateCommand();
                cmd.Append("DELETE FROM ").Append(tableinfo.FullyEvaluatedTableName).Append(" WHERE ").Append(expression.ToString());
                command.CommandText = cmd.ToString();
                await connection.OpenAsync();
                int result = await command.ExecuteNonQueryAsync();
                return result;
            }
        }

    }
}
