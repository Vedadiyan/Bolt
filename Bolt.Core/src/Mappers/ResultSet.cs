using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Bolt.Core.Abstraction;

namespace Bolt.Core.Mappers
{
    public class ResultSet : IResultSet
    {
        public IQuery Query { get; }
        public List<IResult> Items { get; private set; }

        public int Count => Items.Count;

        public ResultSet(IQuery query)
        {
            Query = query;
        }

        public async Task<IResultSet> LoadAsync(string connectionString)
        {
            return await LoadAsync(connectionString, new CancellationToken(), new CancellationToken());
        }
        public async Task<IResultSet> LoadAsync(string connectionString, CancellationToken sqlCancellationToken, CancellationToken enumeratorCancellationToken)
        {
            return await LoadAsync(connectionString, 30000, sqlCancellationToken, enumeratorCancellationToken);
        }
        public async Task<IResultSet> LoadAsync(string connectionString, int timeout, CancellationToken sqlCancellationToken, CancellationToken enumeratorCancellationToken)
        {
            Items = new List<IResult>();
            await foreach (var i in Query.Execute(connectionString, timeout, sqlCancellationToken, enumeratorCancellationToken))
            {
                Items.Add(new Result(i));
            }
            return this;
        }
        [Obsolete("Use Item property instead", true)]
        public List<IResult> ToList()
        {
            return Items;
        }

    }

}
