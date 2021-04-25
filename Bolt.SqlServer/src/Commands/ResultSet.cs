using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Bolt.Core.Abstraction;

namespace Bolt.SqlServer.Commands
{
    public class ResultSet 
    {
        private IQuery query;
        private List<Result> resultSet;
        public int Count { get => resultSet?.Count ?? 0; }
        public List<Result> Items => resultSet;
        public ResultSet(IQuery query)
        {
            this.query = query;
        }
        public async Task<ResultSet> LoadAsync(string connectionString)
        {
            return await LoadAsync(connectionString, new CancellationToken(), new CancellationToken());
        }
        public async Task<ResultSet> LoadAsync(string connectionString, CancellationToken sqlCancellationToken, CancellationToken enumeratorCancellationToken)
        {
            return await LoadAsync(connectionString, 30000, sqlCancellationToken, enumeratorCancellationToken);
        }
        public async Task<ResultSet> LoadAsync(string connectionString, int timeout, CancellationToken sqlCancellationToken, CancellationToken enumeratorCancellationToken)
        {
            resultSet = new List<Result>();
            await foreach (var i in query.Execute(connectionString, timeout, sqlCancellationToken, enumeratorCancellationToken))
            {
                resultSet.Add(new Result(i));
            }
            return this;
        }
        [Obsolete("Use Item property instead", true)]
        public List<Result> ToList() {
            return resultSet;
        }
    }
    public class Result
    {
        private Dictionary<Type, Object> result;
        public Result(Dictionary<Type, Object> result)
        {
            this.result = result;
        }
        public T GetEntity<T>()
        {
            if (result.TryGetValue(typeof(T), out object value))
            {
                return (T)value;
            }
            else
            {
                return default;
            }
        }
        public dynamic GetUnbindValues()
        {
            if (result.TryGetValue(typeof(ExpandoObject), out object value))
            {
                return value;
            }
            else
            {
                return default;
            }
        }
    }
}
