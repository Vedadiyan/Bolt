using System.Data.Common;
using System.Data.SqlClient;
using Bolt.Core.Abstraction;
using MySqlConnector;

namespace Bolt.MySql
{
    public class NonQuery : NonQueryBase
    {
        private readonly string connectionString;
        public NonQuery(string connectionString, int poolSize = 10): base(QueryFormatter.Current, poolSize)
        {
            this.connectionString = connectionString;
        }
        protected override DbConnection GetDbConnection()
        {
            return new MySqlConnection(connectionString);
        }
    }

}