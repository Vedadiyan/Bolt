using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Bolt.Core.Abstraction
{
    public interface IResultSet
    {
        IQuery Query { get; }
        List<IResult> Items { get; }
        int Count { get; }
        Task<IResultSet> LoadAsync(DbConnection connection, int timeout, CancellationToken sqlCancellationToken);
    }
}