using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Bolt.Core.Abstraction {
    public interface IQuery {
        string GetSqlQuery();
        IAsyncEnumerable<Dictionary<Type, object>> Execute(string connectionString, int timeout, CancellationToken sqlCancellationToken,  CancellationToken enumeratorCancellation);
    }
}