using System;
using System.Collections.Generic;
using System.Reflection;
using Bolt.Core.Abstraction;
using Bolt.Core.Interpretation;
using Bolt.Core.Storage;

namespace Bolt.MySql
{
    public class QueryFormatter : IQueryFormatter
    {
        public static IQueryFormatter Current { get; } = new QueryFormatter();
        private QueryFormatter() { }
        public object Format(ExpressionTypes expressionType, Type type, MemberInfo member)
        {
            Table tableInfo = TableMap.Current.GetTable(type);
            if (TableMap.Current.TryGetColumns(type, out IReadOnlyDictionary<string, Column> columns))
            {
                switch (expressionType)
                {
                    case ExpressionTypes.FullyEvaluated:
                        {
                            return new Name(tableInfo.GetFullyEvalulatedColumnName(columns[member.Name]));
                        }
                    case ExpressionTypes.FullyEvaluatedWithTypeName:
                        {
                            return new Name("`" + tableInfo.Type.FullName + "`." + columns[member.Name].ColumnName);
                        }
                    case ExpressionTypes.FullyEvaluatedWithTypeNameAndAlias:
                        {
                            return new Name("`" + tableInfo.Type.Name + "`." + columns[member.Name].ColumnName + " AS " + columns[member.Name].UniqueId);
                        }
                    case ExpressionTypes.FullyEvaluatedWithAlias:
                        {
                            return new Name(tableInfo.GetFullyEvalulatedColumnName(columns[member.Name]) + " AS " + columns[member.Name].UniqueId);
                        }
                    case ExpressionTypes.Alias:
                        {
                            return new Name(columns[member.Name].UniqueId);
                        }
                    default:
                        {
                            return null;
                        }
                }
            }
            throw new KeyNotFoundException(type.FullName);
        }

        public string Format(string input)
        {
            return $"`{input}`";
        }
    }
}