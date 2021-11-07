using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using Bolt.Core.Storage;

namespace Bolt.Core.Abstraction
{
    public abstract class StoredProcedureBase<TResult, TArguments> : IQuery where TArguments : class, new() where TResult : class, new()
    {
        protected StringBuilder parameters;
        protected StoredProcedure StoredProcedure { get; }

        public StoredProcedureBase()
        {
            if (!StoredProcedureMap.Current.TryGetStoredProcedure(typeof(TResult), out StoredProcedure storedProcedure))
            {
                throw new ArgumentException("Stored Procedure not registered");
            }
            StoredProcedure = storedProcedure;
            parameters = new StringBuilder();
        }
        public StoredProcedureBase<TResult, TArguments> SetParameters(TArguments arguments)
        {
            if (parameters.Length > 0)
            {
                parameters.Clear();
            }
            foreach (var i in StoredProcedure.Parameters)
            {
                AddParameter(i.Key, i.Value.GetValue(arguments) ?? DBNull.Value);
            }
            return this;
        }
        public abstract string GetSqlQuery();
        public abstract void AddParameter(string name, object value);
    }
}