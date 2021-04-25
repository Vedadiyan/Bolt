using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Bolt.Core.Abstraction
{
    public interface IResultSet
    {
        IQuery Query { get; }
        List<IResult> Items { get; }
        int Count { get; }
        Task<IResultSet> LoadAsync(string connectionString, int timeout, CancellationToken sqlCancellationToken, CancellationToken enumeratorCancellationToken);
    }
}