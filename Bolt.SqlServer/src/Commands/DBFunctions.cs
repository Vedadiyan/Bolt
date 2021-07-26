using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Bolt.Core.Interpretation;

namespace Bolt.SqlServer
{
    public class DBFunctions
    {
        public static DBO CaseSensitive<T>(String value)
        {
            return new DBO((x) =>
            {
                return new Convertible($"'{value}' COLLATE Latin1_General_CS_AS ");
            });
        }
        public static DBO Function<T, R>(String functionName, Expression<Func<R, object>> exp)
        {
            return new DBO((expressionType) =>
            {
                List<string> arguments = new List<string>();
                if (exp.Body is UnaryExpression convertExpression)
                {
                    MemberExpression member = (MemberExpression)convertExpression.Operand;
                    arguments.Add(QueryFormatter.Current.Format(expressionType, member.Expression.Type, member.Member).ToString());
                }
                else if (exp.Body is NewExpression newExpression)
                {
                    for (int index = 0; index < newExpression.Arguments.Count; index++)
                    {
                        var arg = newExpression.Arguments[index];
                        if (arg is MemberExpression member)
                        {
                            arguments.Add(QueryFormatter.Current.Format(expressionType, member.Expression.Type, member.Member).ToString());
                        }
                        else if (arg is MethodCallExpression callExpression)
                        {
                            LambdaExpression lambdaExpression = Expression.Lambda(callExpression);
                            StringBuilder sb = new StringBuilder();
                            Stack<ExpressionType> stack = new Stack<ExpressionType>();
                            arguments.Add(ExpressionReader.FormatType(lambdaExpression.Compile().DynamicInvoke() ?? "NULL", ref stack, ref sb).ToString());
                        }
                    }
                }
                else if (exp.Body is MethodCallExpression methodCallExpression)
                {
                    LambdaExpression lambdaExpression = Expression.Lambda(methodCallExpression);
                    StringBuilder sb = new StringBuilder();
                    Stack<ExpressionType> stack = new Stack<ExpressionType>();
                    arguments.Add(ExpressionReader.FormatType(lambdaExpression.Compile().DynamicInvoke() ?? "NULL", ref stack, ref sb));
                }
                return new Convertible($"{functionName}({string.Join(',', arguments)})");
            });
        }
        public static DBO AS<T>(Expression<Func<T, object>> exp)
        {
            return new DBO((expressionType) =>
            {
                string argument = null;
                if (exp.Body is UnaryExpression convertExpression)
                {
                    MemberExpression member = (MemberExpression)convertExpression.Operand;
                    argument = QueryFormatter.Current.Format(expressionType, member.Expression.Type, member.Member).ToString();
                }
                else if (exp.Body is NewExpression newExpression)
                {
                    throw new Exception("Invalid Expression");
                }
                else if (exp.Body is MethodCallExpression methodCallExpression)
                {
                    throw new Exception("Invalid Expression");
                }
                return new Convertible(argument);
            });
        }
    }
}