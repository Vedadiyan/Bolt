using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
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
        public IQueryExecutor QueryExecutor; 
        public int Count => Items.Count;

        public ResultSet(IQuery query, IQueryExecutor queryExecutor)
        {
            Query = query;
            QueryExecutor = queryExecutor;
        }

        public async Task<IResultSet> LoadAsync(DbConnection connection)
        {
            return await LoadAsync(connection, 3000, new CancellationToken());
        }
        public async Task<IResultSet> LoadAsync(DbConnection connection, int timeout, CancellationToken sqlCancellationToken)
        {
            Items = new List<IResult>();
            await foreach (var i in QueryExecutor.ExecuteAsync(connection, Query.GetSqlQuery(), timeout, sqlCancellationToken))
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
