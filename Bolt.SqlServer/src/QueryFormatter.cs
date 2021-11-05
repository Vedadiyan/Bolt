using System;
using System.Reflection;
using Bolt.Core.Abstraction;
using Bolt.Core.Interpretation;
using Bolt.Core.Storage;

namespace Bolt.SqlServer
{
    public class QueryFormatter : IQueryFormatter
    {
        public static IQueryFormatter Current { get; } = new QueryFormatter();
        private QueryFormatter() {}
        public object Format(ExpressionTypes expressionType, Type type, MemberInfo member)
        {
            TableInfo tableInfo = DSS.GetTableInfo(type);
            switch (expressionType)
            {
                case ExpressionTypes.FullyEvaluated:
                    {
                        return new Name(tableInfo.Columns[member.Name].FullyEvaluatedColumnName);
                    }
                case ExpressionTypes.FullyEvaluatedWithTypeName:
                    {
                        return new Name("[" + tableInfo.type.Name + "]." + tableInfo.Columns[member.Name].Name);
                    }
                case ExpressionTypes.FullyEvaluatedWithTypeNameAndAlias:
                    {
                        return new Name("[" + tableInfo.type.Name + "]." + tableInfo.Columns[member.Name].Name + " AS " + tableInfo.Columns[member.Name].Alias);
                    }
                case ExpressionTypes.FullyEvaluatedWithAlias:
                    {
                        return new Name(tableInfo.Columns[member.Name].FullyEvaluatedColumnName + " AS " + tableInfo.Columns[member.Name].Alias);
                    }
                case ExpressionTypes.Alias:
                    {
                        return new Name(tableInfo.Columns[member.Name].Alias);
                    }
                default:
                    {
                        return null;
                    }
            }
        }

        public string Format(string input)
        {
            return $"[{input}]";
        }
    }
}