using Bolt.Core;
using Bolt.Core.Abstraction;
using Bolt.SqlServer.Commands;

namespace Bolt.SqlServer
{
    public abstract class DbContext : DbContextBase
    {
        public override INonQuery GetNonQueryScope(int poolSize = 10)
        {
            return new NonQuery(ConnectionString, poolSize);
        }
    }
}