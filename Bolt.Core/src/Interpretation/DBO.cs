using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Bolt.Core.Abstraction;

namespace Bolt.Core.Interpretation
{
    public class DBO
    {
        public Func<ExpressionTypes, Convertible> EExpression { get; }
        private DBO(Func<ExpressionTypes, Convertible> expression)
        {
            EExpression = expression;
        }
        public static DBO Function<T>(String functionName)
        {
            return new DBO((x) =>
            {
                return new Convertible(functionName);
            });
        }
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
                    arguments.Add(ExpressionReader.getValue(expressionType, member.Expression.Type, member.Member).ToString());
                }
                else if (exp.Body is NewExpression newExpression)
                {
                    for (int index = 0; index < newExpression.Arguments.Count; index++)
                    {
                        var arg = newExpression.Arguments[index];
                        if (arg is MemberExpression member)
                        {
                            arguments.Add(ExpressionReader.getValue(expressionType, member.Expression.Type, member.Member).ToString());
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
                    argument = ExpressionReader.getValue(expressionType, member.Expression.Type, member.Member).ToString();
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
        // public static Convertible<T> Function<T, R>(SqlFunctions sqlFunction, Expression<Func<R, object>> exp)
        // {
        //     return default;
        // }
        public static implicit operator int(DBO convertible)
        {
            return default;
        }
        public static implicit operator byte(DBO convertible)
        {
            return default;
        }
        public static implicit operator long(DBO convertible)
        {
            return default;
        }
        public static implicit operator double(DBO convertible)
        {
            return default;
        }
        public static implicit operator decimal(DBO convertible)
        {
            return default;
        }
        public static implicit operator float(DBO convertible)
        {
            return default;
        }
        public static implicit operator short(DBO convertible)
        {
            return default;
        }
        public static implicit operator uint(DBO convertible)
        {
            return default;
        }
        public static implicit operator sbyte(DBO convertible)
        {
            return default;
        }
        public static implicit operator ulong(DBO convertible)
        {
            return default;
        }
        public static implicit operator ushort(DBO convertible)
        {
            return default;
        }
        public static implicit operator DateTime(DBO convertible)
        {
            return default;
        }
        public static implicit operator string(DBO convertible)
        {
            return default;
        }
        public static implicit operator bool(DBO convertible)
        {
            return default;
        }
        public static implicit operator char(DBO convertible)
        {
            return default;
        }
    }
}