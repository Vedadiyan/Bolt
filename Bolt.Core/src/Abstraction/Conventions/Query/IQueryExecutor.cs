using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Threading;

namespace Bolt.Core.Abstraction
{
    public interface IQueryExecutor
    {
        IAsyncEnumerable<Dictionary<Type, object>> ExecuteAsync(DbConnection connection, string sqlCommand, int timeout, CancellationToken cancellationToken);
        IReadOnlyDictionary<string, IReadOnlyCollection<SchemaInfo>> GetQueryTableMap(DataTable dataTable, ReadOnlyCollection<DbColumn> columnSchema);
    }
}