using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Bolt.Core.Interpretation;
using Bolt.Core.Storage;

namespace Bolt.SqlServer.Commands
{
    internal enum CommandTypes
    {
        INSERT,
        UPDATE,
        DELETE,
        TRUNCATE
    }
    internal readonly struct Command
    {
        public string SqlCommand { get; }
        public CommandTypes CommandType { get; }
        public Command(string sqlCommand, CommandTypes commandType)
        {
            SqlCommand = sqlCommand;
            CommandType = commandType;
        }
        public string GetSqlCommand(TableInfo tableInfo, object row)
        {
            string _command = SqlCommand;
            foreach (var column in tableInfo.Columns)
            {
                var value = column.Value.PropertyInfo.GetValue(row);
                switch (Type.GetTypeCode(column.Value.PropertyInfo.PropertyType))
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
                _command = _command.Replace($"@{column.Value.Name}", value.ToString());
            }
            return _command;
        }
        public static Command GetConditionalCommand<TableType>(Command command, Expression<Predicate<TableType>> predicate)
        {
            StringBuilder expression = new StringBuilder();
            ExpressionReader expressionReader = new ExpressionReader(predicate.Body, ExpressionTypes.FullyEvaluated, new Stack<ExpressionType>(), expression);
            return new Command($"{command.SqlCommand} WHERE {expression.ToString()}", command.CommandType);
        }
    }
}