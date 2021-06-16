using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Threading;
using Bolt.Core.Storage;
using Bolt.Core.Abstraction;

namespace Bolt.SqlServer.Commands
{
    public class Query<T> : QueryBase<T> where T : class, new()
    {
        public Query() : base(QueryFormatter.Current)
        {

        }
        public override IAsyncEnumerable<Dictionary<Type, object>> Execute(string connectionString, int timeout, CancellationToken sqlCancellationToken, CancellationToken enumeratorCancellation)
        {
            return Execute(new SqlConnection(connectionString), timeout, sqlCancellationToken, enumeratorCancellation);
        }
    }
}