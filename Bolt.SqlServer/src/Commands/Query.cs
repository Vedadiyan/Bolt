using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Threading;
using Bolt.Core.Storage;
using Bolt.Core.Abstraction;
using Bolt.Core.Interpretation;
using System.Text;

namespace Bolt.SqlServer.Commands
{
    public class Query<T> : QueryBase<T> where T : class, new()
    {
        public Query() : base(SqlServer.QueryFormatter.Current)
        {

        }
        public override IAsyncEnumerable<Dictionary<Type, object>> Execute(string connectionString, int timeout, CancellationToken sqlCancellationToken, CancellationToken enumeratorCancellation)
        {
            return Execute(new SqlConnection(connectionString), timeout, sqlCancellationToken, enumeratorCancellation);
        }

        public override string GetSqlQuery()
        {
            TableInfo tableInfo = DSS.GetTableInfo<T>();
            if (JoinCluase.Length > 0)
            {
                ExpressionTypeVariant = ExpressionTypes.FullyEvaluatedWithTypeName;
                SelectExpressionType = ExpressionTypes.FullyEvaluatedWithTypeNameAndAlias;
            }
            else
            {
                ExpressionTypeVariant = ExpressionTypes.FullyEvaluated;
                SelectExpressionType = ExpressionTypes.FullyEvaluatedWithAlias;
            }
            StringBuilder query = new StringBuilder();

            while (SelectClauses.Count > 0)
            {
                SelectClauses.Dequeue()();
            }
            query
            .Append("SELECT ");
            if (_Distinct)
            {
                query.Append(" DISTINCT ");
            }
            if (_Top > 0)
            {
                query.Append(" TOP ").Append(_Top).Append(" ");
            }
            query.Append(SelectClause)
            .Append(" FROM ");
            if (TableInfo.Count == 1)
            {
                query.Append(tableInfo.FullyEvaluatedTableName);
            }
            else
            {
                query.Append(tableInfo.FullyEvaluatedTableName + " AS " + QueryFormatter.Format(tableInfo.type.Name) + "");
            }
            if (JoinCluase.Length > 0)
            {
                query.Append(JoinCluase);
            }
            else
            {
                ExpressionTypeVariant = ExpressionTypes.FullyEvaluated;
            }
            while (WhereClauses.Count > 0)
            {
                WhereClauses.Dequeue()();
            }
            if (WhereCluase.Length > 0)
            {
                query.Append(" WHERE ").Append(WhereCluase);
            }
            while (GroupByClauses.Count > 0)
            {
                GroupByClauses.Dequeue()();
            }
            if (GroupByClause.Length > 0)
            {
                query.Append(" GROUP BY ").Append(GroupByClause);
            }
            while (HavingClauses.Count > 0)
            {
                HavingClauses.Dequeue()();
            }
            if (HavingClause.Length > 0)
            {
                query.Append(" HAVING (").Append(HavingClause).Append(")");
            }
            while (OrderByClauses.Count > 0)
            {
                OrderByClauses.Dequeue()();
            }
            if (OrderByClause.Length > 0)
            {
                query.Append(" ORDER BY ").Append(OrderByClause);
            }
            return query.ToString();
        }
    }
}