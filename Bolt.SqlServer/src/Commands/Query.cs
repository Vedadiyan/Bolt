using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bolt.Core.Interpretation;
using Bolt.Core.Storage;
using Bolt.Core.Abstraction;

namespace Bolt.SqlServer.Commands
{
    public class Query<T> : IQuery where T : new()
    {
        private Dictionary<string, TableInfo> tableInfos;
        private Dictionary<string, PropertyInfo> properties;
        private Dictionary<string, Func<object>> tableObjects;
        private StringBuilder select;
        private StringBuilder where;
        private StringBuilder join;
        private StringBuilder groupBy;
        private StringBuilder orderBy;
        private StringBuilder havingClause;
        private Queue<Action> whereClauses;
        private Queue<Action> selectClauses;
        private Queue<Action> groupByClauses;
        private Queue<Action> orderByClauses;
        private Queue<Action> havingClauses;
        private ExpressionTypes expressionTypeVariant;
        private ExpressionTypes selectExpressionType;
        public Query()
        {
            select = new StringBuilder();
            where = new StringBuilder();
            join = new StringBuilder();
            groupBy = new StringBuilder();
            orderBy = new StringBuilder();
            havingClause = new StringBuilder();
            tableInfos = new Dictionary<string, TableInfo>();
            TableInfo tableInfo = DSS.GetTableInfo<T>();
            tableInfos.Add(tableInfo.TableName, tableInfo);
            properties = new Dictionary<string, PropertyInfo>();
            foreach (var property in tableInfo.type.GetProperties())
            {
                properties.Add(tableInfo.Columns[property.Name].Alias, property);
            }
            tableObjects = new Dictionary<string, Func<object>>();
            tableObjects.Add(tableInfo.type.Name, () => Activator.CreateInstance(tableInfo.type));
            whereClauses = new Queue<Action>();
            groupByClauses = new Queue<Action>();
            orderByClauses = new Queue<Action>();
            havingClauses = new Queue<Action>();
            selectClauses = new Queue<Action>();
        }
        public Query<T> Where<R>(Expression<Predicate<R>> expression)
        {
            whereClauses.Enqueue(() => _where(expression));
            return this;
        }
        private void _where<R>(Expression<Predicate<R>> expression)
        {
            where.Clear();
            StringBuilder sb = new StringBuilder();
            ExpressionReader expressionReader = new ExpressionReader(expression.Body, expressionTypeVariant, new Stack<ExpressionType>(), sb);
            where.Append(sb.ToString());
        }
        private void joinFunction<L, R>(string joinType, Expression<Func<(L CurrentTable, R TargetTable), object>> expression)
        {
            TableInfo left = DSS.GetTableInfo<L>();
            if (!tableInfos.ContainsKey(left.TableName))
            {
                throw new Exception($"{left.TableName} is not present in the query");
            }
            StringBuilder sb = new StringBuilder();
            ExpressionReader expressionReader = new ExpressionReader(expression.Body, ExpressionTypes.FullyEvaluatedWithTypeName, new Stack<ExpressionType>(), sb);
            TableInfo right = DSS.GetTableInfo<R>();
            string tableName = null;
            if (!tableInfos.ContainsKey(right.TableName))
            {
                tableInfos.Add(right.TableName, right);
                foreach (var property in right.type.GetProperties())
                {
                    properties.Add(right.Columns[property.Name].Alias, property);
                }
                tableObjects.Add(right.type.Name, () => Activator.CreateInstance(right.type));
                tableName = right.FullyEvaluatedTableName + " AS [" + right.type.Name + "]";
            }
            else
            {
                tableName = right.type.Name;
            }
            if (!string.IsNullOrEmpty(joinType))
            {
                join.Append("\r\n").Append(joinType);
            }
            join.Append(" JOIN ").Append(tableName).Append(" ON ").Append(sb.ToString());
        }
        public Query<T> Join<L, R>(Expression<Func<(L Left, R Right), object>> expression)
        {
            joinFunction<L, R>(null, expression);
            return this;
        }
        public Query<T> LefJoin<L, R>(Expression<Func<(L Left, R Right), object>> expression)
        {
            joinFunction("LEFT", expression);
            return this;
        }
        public Query<T> RightJoin<L, R>(Expression<Func<(L Left, R Right), object>> expression)
        {
            joinFunction("Right", expression);
            return this;
        }
        public Query<T> FullOuterJoin<L, R>(Expression<Func<(L Left, R Right), object>> expression)
        {
            joinFunction("FULL OUTER", expression);
            return this;
        }
        public Query<T> GroupBy<R>(Expression<Func<R, object>> expression) where R : new()
        {
            groupByClauses.Enqueue(() => _groupBy(expression));
            return this;
        }
        private void _groupBy<R>(Expression<Func<R, object>> expression) where R : new()
        {
            StringBuilder sb = new StringBuilder();
            ExpressionReader expressionReader = new ExpressionReader(expression.Body, expressionTypeVariant, new Stack<ExpressionType>(), sb);
            if (groupBy.Length > 0)
            {
                groupBy.Append(", \r\n");
            }
            groupBy.Append(sb.ToString());
        }
        public Query<T> Having<R>(Expression<Predicate<R>> expression) where R : new()
        {
            havingClauses.Enqueue(() => _having(expression));
            return this;
        }
        private void _having<R>(Expression<Predicate<R>> expression) where R : new()
        {
            StringBuilder sb = new StringBuilder();
            ExpressionReader expressionReader = new ExpressionReader(expression.Body, expressionTypeVariant, new Stack<ExpressionType>(), sb);
            if (havingClause.Length > 0)
            {
                havingClause.Append(", \r\n");
            }
            havingClause.Append(sb.ToString());
        }
        public Query<T> OrderBy<R>(Expression<Func<R, object>> expression) where R : new()
        {
            orderByClauses.Enqueue(() => _orderBy(expression));
            return this;
        }
        private void _orderBy<R>(Expression<Func<R, object>> expression) where R : new()
        {
            StringBuilder sb = new StringBuilder();
            ExpressionReader expressionReader = new ExpressionReader(expression.Body, expressionTypeVariant, new Stack<ExpressionType>(), sb);
            if (orderBy.Length > 0)
            {
                orderBy.Append(", \r\n");
            }
            orderBy.Append(sb.ToString());
        }
        public Query<T> OrderByDescending<R>(Expression<Func<R, object>> expression) where R : new()
        {
            orderByClauses.Enqueue(() => _orderByDescending(expression));
            return this;
        }
        private void _orderByDescending<R>(Expression<Func<R, object>> expression) where R : new()
        {
            StringBuilder sb = new StringBuilder();
            ExpressionReader expressionReader = new ExpressionReader(expression.Body, expressionTypeVariant, new Stack<ExpressionType>(), sb);
            if (orderBy.Length > 0)
            {
                orderBy.Append(", \r\n");
            }
            orderBy.Append(sb.ToString()).Append(" DESC");
        }
        public Query<T> Select<R>(Expression<Func<R, object>> expression) where R : new()
        {
            selectClauses.Enqueue(() => _select(expression));
            return this;
        }
        private void _select<R>(Expression<Func<R, object>> expression) where R : new()
        {
            if (select.Length > 0)
            {
                select.Append(", \r\n");
            }
            StringBuilder sb = new StringBuilder();
            ExpressionReader expressionReader = new ExpressionReader(expression.Body, selectExpressionType, new Stack<ExpressionType>(), sb);
            select.Append($"{sb.ToString()}");
        }
        public Query<T> Select()
        {
            selectClauses.Enqueue(() => _select());
            return this;
        }
        private void _select()
        {
            select.Clear();
            int eCount = 0;
            int iCount = 0;
            foreach (var tableInfo in tableInfos)
            {
                iCount = 0;
                foreach (var columnInfo in tableInfo.Value.Columns)
                {
                    if (selectExpressionType == ExpressionTypes.FullyEvaluatedWithAlias)
                    {
                        select.Append(columnInfo.Value.FullyEvaluatedColumnName + " AS " + columnInfo.Value.Alias);
                    }
                    else if (selectExpressionType == ExpressionTypes.FullyEvaluatedWithTypeNameAndAlias)
                    {
                        select.Append("[" + tableInfo.Value.type.Name + "]." + columnInfo.Value.Name + " AS " + columnInfo.Value.Alias);
                    }
                    if (iCount++ < tableInfo.Value.Columns.Count - 1)
                    {
                        select.Append(", \r\n");
                    }
                }
                if (eCount++ < tableInfos.Count - 1)
                {
                    select.Append(", \r\n");
                }
            }
        }

        public string GetSqlQuery()
        {
            StringBuilder query = new StringBuilder();
            TableInfo tableInfo = DSS.GetTableInfo<T>();
            if (join.Length > 0)
            {
                expressionTypeVariant = ExpressionTypes.FullyEvaluatedWithTypeName;
                selectExpressionType = ExpressionTypes.FullyEvaluatedWithTypeNameAndAlias;
            }
            else
            {
                expressionTypeVariant = ExpressionTypes.FullyEvaluated;
                selectExpressionType = ExpressionTypes.FullyEvaluatedWithAlias;
            }
            while (selectClauses.Count > 0)
            {
                selectClauses.Dequeue()();
            }
            query
            .Append("SELECT ")
            .Append(select)
            .Append(" FROM ");
            if (tableInfos.Count == 1)
            {
                query.Append(tableInfo.FullyEvaluatedTableName);
            }
            else
            {
                query.Append(tableInfo.FullyEvaluatedTableName + " AS [" + tableInfo.type.Name + "]");
            }
            if (join.Length > 0)
            {
                query.Append(join);
            }
            else
            {
                expressionTypeVariant = ExpressionTypes.FullyEvaluated;
            }
            while (whereClauses.Count > 0)
            {
                whereClauses.Dequeue()();
            }
            if (where.Length > 0)
            {
                query.Append(" WHERE ").Append(where);
            }
            while (groupByClauses.Count > 0)
            {
                groupByClauses.Dequeue()();
            }
            if (groupBy.Length > 0)
            {
                query.Append(" GROUP BY ").Append(groupBy);
            }
            while (havingClauses.Count > 0)
            {
                havingClauses.Dequeue()();
            }
            if (havingClause.Length > 0)
            {
                query.Append(" HAVING (").Append(havingClause).Append(")");
            }
            while (orderByClauses.Count > 0)
            {
                orderByClauses.Dequeue()();
            }
            if (orderBy.Length > 0)
            {
                query.Append(" ORDER BY ").Append(orderBy);
            }
            return query.ToString();
        }
        public async IAsyncEnumerable<Dictionary<Type, object>> Execute(string connectionString, int timeout, CancellationToken sqlCancellationToken, [EnumeratorCancellation] CancellationToken enumeratorCancellation)
        {

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = connection.CreateCommand();
                command.CommandText = GetSqlQuery();
                command.CommandTimeout = timeout;
                await connection.OpenAsync(sqlCancellationToken);
                SqlDataReader dataReader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection, sqlCancellationToken);
                if (dataReader.HasRows)
                {
                    while (await dataReader.ReadAsync() && !enumeratorCancellation.IsCancellationRequested)
                    {
                        ExpandoObject expandoObject = null;
                        Dictionary<Type, object> list = new Dictionary<Type, Object>();
                        foreach (var column in dataReader.GetColumnSchema())
                        {
                            var value = dataReader.GetValue(column.ColumnName);
                            if (DSS.TryGetColumnInfo(column.ColumnName, out ColumnInfo columnInfo))
                            {
                                TableInfo tableInfo = DSS.GetTableInfo(columnInfo.TableKey);
                                if (list.ContainsKey(tableInfo.type))
                                {
                                    properties[column.ColumnName].SetValue(list[tableInfo.type], value != DBNull.Value ? value : null);
                                }
                                else
                                {
                                    list.Add(tableInfo.type, tableObjects[tableInfo.type.Name]());
                                    properties[column.ColumnName].SetValue(list[tableInfo.type], value != DBNull.Value ? value : null);

                                }
                            }
                            else
                            {
                                if (expandoObject == null)
                                {
                                    expandoObject = new ExpandoObject();
                                }
                                expandoObject.TryAdd(column.ColumnName, value);
                            }
                        }
                        if (expandoObject != null)
                        {
                            list.Add(expandoObject.GetType(), expandoObject);
                        }
                        yield return list;
                    }
                }
            }
        }
    }
}