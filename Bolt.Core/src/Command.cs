using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Bolt.Core.Abstraction;
using Bolt.Core.Interpretation;
using Bolt.Core.Storage;

namespace Bolt.Core
{
    public enum CommandTypes
    {
        INSERT,
        UPDATE,
        DELETE,
        TRUNCATE
    }
    public readonly struct Command
    {
        public string SqlCommand { get; }
        public CommandTypes CommandType { get; }
        public Command(string sqlCommand, CommandTypes commandType)
        {
            SqlCommand = sqlCommand;
            CommandType = commandType;
        }
        public string GetSqlCommand(Table tableInfo, object row)
        {
            string _command = SqlCommand;
            foreach (var column in tableInfo.GetColumns())
            {
                var value = column.PropertyInfo.GetValue(row);
                switch (Type.GetTypeCode(column.PropertyInfo.PropertyType))
                {
                    case TypeCode.Char:
                    case TypeCode.DateTime:
                    case TypeCode.String:
                        value = $"'{value.ToString().Replace("'", "''")}'";
                        break;
                    case TypeCode.Boolean:
                        value = (bool)value ? 1 : 0;
                        break;
                }
                _command = _command.Replace($"@{column.ColumnName}", value.ToString());
            }
            return _command;
        }
        public static Command GetConditionalCommand<TableType>(Command command, Expression<Predicate<TableType>> predicate, IQueryFormatter queryFormatter)
        {
            StringBuilder expression = new StringBuilder();
            ExpressionReader expressionReader = new ExpressionReader(predicate.Body, ExpressionTypes.FullyEvaluated, new Stack<ExpressionType>(), expression, queryFormatter);
            return new Command($"{command.SqlCommand} WHERE {expression.ToString()}", command.CommandType);
        }
    }
}