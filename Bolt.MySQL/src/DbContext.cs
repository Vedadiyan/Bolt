using Bolt.Core;
using Bolt.Core.Abstraction;
using Bolt.MySql.Commands;

namespace Bolt.MySql
{
    public abstract class DbContext : DbContextBase
    {
        public override INonQuery GetNonQueryScope(int poolSize = 10)
        {
            return new NonQuery(ConnectionString, poolSize);
        }
    }
}