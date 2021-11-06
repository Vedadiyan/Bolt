using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Bolt.Core.Abstraction;
using Bolt.Core.Annotations;
using Bolt.Core.Mappers;
using Bolt.Core.Storage;

namespace Bolt.Core
{
    public abstract class DbContextBase
    {
        private static bool isInitialized = false;
        private IQueryExecutor queryExecutor;
        public DbContextBase(IQueryExecutor queryExecutor)
        {
            if (!isInitialized)
            {
                Assembly entryAssembly = Assembly.GetEntryAssembly();
                loadAssembly(entryAssembly);

                AssemblyName[] referencedAssemblies = entryAssembly.GetReferencedAssemblies();
                foreach (var referencedAssembly in referencedAssemblies)
                {
                    loadAssembly(Assembly.Load(referencedAssembly));
                }
                isInitialized = true;
            }
            this.queryExecutor = queryExecutor;
        }
        protected abstract DbConnection GetConnection();
        protected abstract int Timeout { get; }
        private void loadAssembly(Assembly assembly)
        {
            Type[] types = assembly.GetExportedTypes();
            foreach (var type in types)
            {
                TableAttribute tableAttribute = type.GetCustomAttribute<TableAttribute>();
                if (tableAttribute != null)
                {
                    DSS.RegisterTableStructure(type);
                }
            }
        }
        public Task<List<IResult>> ExecuteQueryAsync(IQuery query)
        {
            return ExecuteQueryAsync(query, new CancellationToken());
        }
        public async Task<List<IResult>> ExecuteQueryAsync(IQuery query, CancellationToken cancellationToken)
        {
            IResultSet resultSet = new ResultSet(query, queryExecutor);
            await resultSet.LoadAsync(GetConnection(), Timeout, cancellationToken);
            return resultSet.Items;
        }
        public abstract INonQuery GetNonQueryScope(int poolSize = 10);
    }
}