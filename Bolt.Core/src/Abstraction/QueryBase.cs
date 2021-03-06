using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Bolt.Core.Annotations;
using Bolt.Core.Interpretation;
using Bolt.Core.Storage;

namespace Bolt.Core.Abstraction
{
    public abstract class QueryBase<T> : IQuery where T : new()
    {
        protected Dictionary<string, TableInfo> TableInfo { get; set; }
        protected Dictionary<string, Func<object>> TableObjects { get; set; }
        protected StringBuilder SelectClause { get; set; }
        protected StringBuilder WhereCluase { get; set; }
        protected StringBuilder JoinCluase { get; set; }
        protected StringBuilder GroupByClause { get; set; }
        protected StringBuilder OrderByClause { get; set; }
        protected StringBuilder HavingClause { get; set; }
        protected Queue<Action> WhereClauses { get; set; }
        protected Queue<Action> SelectClauses { get; set; }
        protected Queue<Action> GroupByClauses { get; set; }
        protected Queue<Action> OrderByClauses { get; set; }
        protected Queue<Action> HavingClauses { get; set; }
        protected ExpressionTypes ExpressionTypeVariant { get; set; }
        protected ExpressionTypes SelectExpressionType { get; set; }
        protected IQueryFormatter QueryFormatter;
        protected int _Top { get; set; } = -1;
        protected bool _Distinct { get; set; }
        private bool clearGenericSelect = false;
        public QueryBase(IQueryFormatter queryFormatter)
        {
            QueryFormatter = queryFormatter;
            SelectClause = new StringBuilder();
            WhereCluase = new StringBuilder();
            JoinCluase = new StringBuilder();
            GroupByClause = new StringBuilder();
            OrderByClause = new StringBuilder();
            HavingClause = new StringBuilder();
            TableInfo = new Dictionary<string, TableInfo>();
            TableInfo tableInfo = DSS.GetTableInfo<T>();
            TableInfo.Add(tableInfo.TableName, tableInfo);
            TableObjects = new Dictionary<string, Func<object>>();
            TableObjects.Add(tableInfo.type.Name, () => Activator.CreateInstance(tableInfo.type));
            WhereClauses = new Queue<Action>();
            GroupByClauses = new Queue<Action>();
            OrderByClauses = new Queue<Action>();
            HavingClauses = new Queue<Action>();
            SelectClauses = new Queue<Action>();
        }
        public virtual QueryBase<T> Where<R>(Expression<Predicate<R>> expression)
        {
            WhereClauses.Enqueue(() => where(expression));
            return this;
        }
        public virtual QueryBase<T> Join<L, R>(Expression<Func<(L Left, R Right), object>> expression)
        {
            join<L, R>(null, expression);
            return this;
        }
        public virtual QueryBase<T> LefJoin<L, R>(Expression<Func<(L Left, R Right), object>> expression)
        {
            join("LEFT", expression);
            return this;
        }
        public virtual QueryBase<T> RightJoin<L, R>(Expression<Func<(L Left, R Right), object>> expression)
        {
            join("Right", expression);
            return this;
        }
        public virtual QueryBase<T> FullOuterJoin<L, R>(Expression<Func<(L Left, R Right), object>> expression)
        {
            join("FULL OUTER", expression);
            return this;
        }
        public virtual QueryBase<T> GroupBy<R>(Expression<Func<R, object>> expression) where R : new()
        {
            GroupByClauses.Enqueue(() => groupBy(expression));
            return this;
        }
        public virtual QueryBase<T> Having<R>(Expression<Predicate<R>> expression) where R : new()
        {
            HavingClauses.Enqueue(() => having(expression));
            return this;
        }
        public virtual QueryBase<T> OrderBy<R>(Expression<Func<R, object>> expression) where R : new()
        {
            OrderByClauses.Enqueue(() => orderBy(expression));
            return this;
        }
        public virtual QueryBase<T> OrderByDescending<R>(Expression<Func<R, object>> expression) where R : new()
        {
            OrderByClauses.Enqueue(() => orderByDescending(expression));
            return this;
        }
        public virtual QueryBase<T> Select<R>(Expression<Func<R, object>> expression) where R : new()
        {
            SelectClauses.Enqueue(() => select(expression));
            return this;
        }
        public virtual QueryBase<T> Select(bool clearGenericSelect = true)
        {
            SelectClauses.Enqueue(() => select());
            this.clearGenericSelect = clearGenericSelect;
            return this;
        }
        public virtual QueryBase<T> Top(int i)
        {
            _Top = i;
            return this;
        }
        public virtual QueryBase<T> Distinct(bool distinct = true)
        {
            _Distinct = distinct;
            return this;
        }
        private void where<R>(Expression<Predicate<R>> expression)
        {
            WhereCluase.Clear();
            StringBuilder sb = new StringBuilder();
            ExpressionReader expressionReader = new ExpressionReader(expression.Body, ExpressionTypeVariant, new Stack<ExpressionType>(), sb, QueryFormatter);
            WhereCluase.Append(sb.ToString());
        }
        private void join<L, R>(string joinType, Expression<Func<(L CurrentTable, R TargetTable), object>> expression)
        {
            TableInfo left = DSS.GetTableInfo<L>();
            if (!TableInfo.ContainsKey(left.TableName))
            {
                throw new Exception($"{left.TableName} is not present in the query");
            }
            StringBuilder sb = new StringBuilder();
            ExpressionReader expressionReader = new ExpressionReader(expression.Body, ExpressionTypes.FullyEvaluatedWithTypeName, new Stack<ExpressionType>(), sb, QueryFormatter);
            TableInfo right = DSS.GetTableInfo<R>();
            string tableName = null;
            if (!TableInfo.ContainsKey(right.TableName))
            {
                TableInfo.Add(right.TableName, right);
                TableObjects.Add(right.type.Name, () => Activator.CreateInstance(right.type));
                tableName = right.FullyEvaluatedTableName + " AS " + QueryFormatter.Format(right.type.Name);
            }
            else
            {
                tableName = right.type.Name;
            }
            if (!string.IsNullOrEmpty(joinType))
            {
                JoinCluase.Append("\r\n").Append(joinType);
            }
            JoinCluase.Append(" JOIN ").Append(tableName).Append(" ON ").Append(sb.ToString());
        }
        private void groupBy<R>(Expression<Func<R, object>> expression) where R : new()
        {
            StringBuilder sb = new StringBuilder();
            ExpressionReader expressionReader = new ExpressionReader(expression.Body, ExpressionTypeVariant, new Stack<ExpressionType>(), sb, QueryFormatter);
            if (GroupByClause.Length > 0)
            {
                GroupByClause.Append(", \r\n");
            }
            GroupByClause.Append(sb.ToString());
        }
        private void having<R>(Expression<Predicate<R>> expression) where R : new()
        {
            StringBuilder sb = new StringBuilder();
            ExpressionReader expressionReader = new ExpressionReader(expression.Body, ExpressionTypeVariant, new Stack<ExpressionType>(), sb, QueryFormatter);
            if (HavingClause.Length > 0)
            {
                HavingClause.Append(", \r\n");
            }
            HavingClause.Append(sb.ToString());
        }
        private void orderBy<R>(Expression<Func<R, object>> expression) where R : new()
        {
            StringBuilder sb = new StringBuilder();
            ExpressionReader expressionReader = new ExpressionReader(expression.Body, ExpressionTypeVariant, new Stack<ExpressionType>(), sb, QueryFormatter);
            if (OrderByClause.Length > 0)
            {
                OrderByClause.Append(", \r\n");
            }
            OrderByClause.Append(sb.ToString());
        }
        private void orderByDescending<R>(Expression<Func<R, object>> expression) where R : new()
        {
            StringBuilder sb = new StringBuilder();
            ExpressionReader expressionReader = new ExpressionReader(expression.Body, ExpressionTypeVariant, new Stack<ExpressionType>(), sb, QueryFormatter);
            if (OrderByClause.Length > 0)
            {
                OrderByClause.Append(", \r\n");
            }
            OrderByClause.Append(sb.ToString()).Append(" DESC");
        }
        private void select<R>(Expression<Func<R, object>> expression) where R : new()
        {
            if (SelectClause.Length > 0)
            {
                SelectClause.Append(", \r\n");
            }
            StringBuilder sb = new StringBuilder();
            ExpressionReader expressionReader = new ExpressionReader(expression.Body, SelectExpressionType, new Stack<ExpressionType>(), sb, QueryFormatter);
            SelectClause.Append($"{sb.ToString()}");
        }
        private void select()
        {
            int eCount = 0;
            int iCount = 0;
            StringBuilder sb = new StringBuilder();
            foreach (var tableInfo in TableInfo)
            {
                iCount = 0;
                foreach (var columnInfo in tableInfo.Value.Columns)
                {
                    if (SelectExpressionType == ExpressionTypes.FullyEvaluatedWithAlias)
                    {
                        sb.Append(columnInfo.Value.FullyEvaluatedColumnName + " AS " + columnInfo.Value.Alias);
                    }
                    else if (SelectExpressionType == ExpressionTypes.FullyEvaluatedWithTypeNameAndAlias)
                    {
                        sb.Append(QueryFormatter.Format(tableInfo.Value.type.Name) + "." + columnInfo.Value.Name + " AS " + columnInfo.Value.Alias);
                    }
                    if (iCount++ < tableInfo.Value.Columns.Count - 1)
                    {
                        sb.Append(", \r\n");
                    }
                }
                if (eCount++ < TableInfo.Count - 1)
                {
                    sb.Append(", \r\n");
                }
            }
            if (clearGenericSelect)
            {
                SelectClause.Clear();
                SelectClause.Append(sb);
            }
            else
            {
                var _sb = sb.ToString();
                if (!string.IsNullOrWhiteSpace(_sb))
                {
                    SelectClause.Append(", ");
                }
                SelectClause.Append(sb);
            }
        }
        public abstract string GetSqlQuery();
        protected async IAsyncEnumerable<Dictionary<Type, object>> Execute(DbConnection connection, int timeout, CancellationToken sqlCancellationToken, [EnumeratorCancellation] CancellationToken enumeratorCancellation)
        {
            using (connection)
            {
                DbCommand command = connection.CreateCommand();
                command.CommandText = GetSqlQuery();
                command.CommandTimeout = timeout;
                await connection.OpenAsync(sqlCancellationToken);
                DbDataReader dataReader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection, sqlCancellationToken);
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
                                foreach (var i in tableInfo.Columns[columnInfo.PropertyInfo.Name].Proccessors)
                                {
                                    value = i.Process(value);
                                }
                                if (list.ContainsKey(tableInfo.type))
                                {
                                    columnInfo.PropertyInfo.SetValue(list[tableInfo.type], value != DBNull.Value ? value : null);
                                }
                                else
                                {
                                    list.Add(tableInfo.type, TableObjects[tableInfo.type.Name]());
                                    columnInfo.PropertyInfo.SetValue(list[tableInfo.type], value != DBNull.Value ? value : null);
                                }
                            }
                            else
                            {
                                if (expandoObject == null)
                                {
                                    expandoObject = new ExpandoObject();
                                }
                                expandoObject.TryAdd(column.ColumnName, value != DBNull.Value ? value : null);
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

        public abstract IAsyncEnumerable<Dictionary<Type, object>> Execute(string connectionString, int timeout, CancellationToken sqlCancellationToken, CancellationToken enumeratorCancellation);
    }

}