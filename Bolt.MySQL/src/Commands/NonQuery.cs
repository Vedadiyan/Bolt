using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;
using Bolt.Core.Abstraction;
using Bolt.Core.Storage;
using MySqlConnector;

namespace Bolt.MySql
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
                    values.Append("@").Append(column.Value.UniqueId);
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
            cmd.Append("INSERT INTO ").Append(tableinfo.FullyEvaluatedTableName).Append("(").Append(columns).Append(")").Append(" VALUES (").Append(values).Append("); SELECT LAST_INSERT_ID() AS ID");
            return cmd.ToString();
        }

        protected override DbConnection GetDbConnection()
        {
            return new MySqlConnection(connectionString);
        }
    }

}