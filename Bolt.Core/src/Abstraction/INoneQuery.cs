using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bolt.Core.Annotations;
using Bolt.Core.Storage;

namespace Bolt.Core.Abstraction
{
    public interface INonQuery: IDisposable
    {
        bool CreateTransaction(int transactionCount);
        Task CommitAsync();
        
        Task RollbackAsync();
        Task ApplyAsync();
        Task BatchedApplyAsync();
        Task<TableType> InsertAsync<TableType>(TableType row, int? transactionNumber);
        void BufferedInsert<TableType>(TableType row, int? transactionNumber);
        Task<object> UpdateAsync<TableType>(TableType row, Expression<Predicate<TableType>> predicate, int? transactionNumber);
        void BufferedUpdate<TableType>(TableType row, Expression<Predicate<TableType>> predicate, int? transactionNumber);
        Task<object> DeleteAsync<TableType>(Expression<Predicate<TableType>> predicate, int? transactionNumber);
        void BufferedDelete<TableType>(TableType row, Expression<Predicate<TableType>> predicate, int? transactionNumber);
        Task<object> TruncateAsync<TableType>(int? transactionNumber);
        Task<object> DeleteAllAsync<TableType>(int? transactionNumber);
        Task<TableType[]> InsertManyAsync<TableType>(TableType[] rows, int? transactionNumber);
        Task<TableType[]> BulkInsertAsync<TableType>(TableType[] rows, int? transactionNumber);
    }
}