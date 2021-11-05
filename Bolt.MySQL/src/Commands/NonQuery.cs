using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;
using Bolt.Core.Abstraction;
using Bolt.Core.Storage;
using MySqlConnector;

namespace Bolt.MySql.Commands
{
    public class NonQuery : NonQueryBase
    {
        private readonly string connectionString;
        public NonQuery(string connectionString, int poolSize = 10) : base(QueryFormatter.Current, poolSize)
        {
            this.connectionString = connectionString;
        }

        protected override string CreateInsertCommand(Type type)
        {
            Table tableinfo = TableMap.Current.GetTable(type);
            StringBuilder cmd = new StringBuilder();
            StringBuilder columns = new StringBuilder();
            StringBuilder values = new StringBuilder();
            Column surrogateKey = default;
            foreach (var column in tableinfo.GetColumns())
            {
                if (!column.ColumnFeatures.IsSurrogateKey)
                {
                    if (columns.Length > 0)
                    {
                        columns.Append(',');
                        values.Append(',');
                    }
                    columns.Append(column.ColumnName);
                    values.Append("@").Append(column.UniqueId);
                }
                else
                {
                    if (surrogateKey == null)
                    {
                        surrogateKey = column;
                    }
                    else
                    {
                        throw new Exception("Composite Surrogate Key Is Not Supported");
                    }
                }
            }
            cmd.Append("INSERT INTO ").Append(tableinfo.FullyEvaluatedTableName).Append("(").Append(columns).Append(")").Append(" VALUES (").Append(values).Append("); SELECT LAST_INSERT_ID() AS ID");
            return cmd.ToString();
        }

        protected override DbConnection GetDbConnection()
        {
            return new MySqlConnection(connectionString);
        }
    }

}