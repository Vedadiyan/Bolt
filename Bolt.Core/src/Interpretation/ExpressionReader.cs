using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Bolt.Core.Abstraction;
using Bolt.Core.Storage;

namespace Bolt.Core.Interpretation
{
    public class ExpressionReader
    {
        public Expression Expression { get; }
        public object Value { get; private set; }
        private StringBuilder sb;
        private Stack<ExpressionType> stack;
        private ExpressionTypes expressionType;
        private IQueryFormatter queryFormatter;
        public ExpressionReader(Expression expression, ExpressionTypes expressionType, Stack<ExpressionType> stack, StringBuilder sb, IQueryFormatter queryFormatter)
        {
            this.expressionType = expressionType;
            this.stack = stack;
            this.sb = sb;
            this.queryFormatter = queryFormatter;
            Value = "";
            if (expression is BinaryExpression binaryExpression)
            {
                sb.Append("(");
                read(binaryExpression.Left);
                switch (binaryExpression.NodeType)
                {
                    case ExpressionType.Equal:
                    case ExpressionType.NotEqual:
                    case ExpressionType.GreaterThan:
                    case ExpressionType.LessThan:
                    case ExpressionType.GreaterThanOrEqual:
                    case ExpressionType.LessThanOrEqual:
                        {
                            stack.Push(binaryExpression.NodeType);
                            break;
                        }
                    default:
                        {
                            sb.Append(convertNodeType(binaryExpression.NodeType));
                            break;
                        }
                }
                read(binaryExpression.Right);
                sb.Append(")");
            }
            else if (expression is MemberExpression memberExpression)
            {
                if (memberExpression.Expression is MemberExpression innerMemberExpression)
                {
                    if (stack.Count > 0)
                    {
                        sb.Append(convertNodeType(stack.Pop()));
                        if (innerMemberExpression.Expression is ParameterExpression parameterExpression)
                        {
                            Value = queryFormatter.Format(expressionType, innerMemberExpression.Type, memberExpression.Member);
                        }
                        else
                        {
                            LambdaExpression lamdaExpression = LambdaExpression.Lambda(memberExpression);
                            Value = lamdaExpression.Compile().DynamicInvoke() ?? DBNull.Value;
                        }
                    }
                    else
                    {
                        // Get FullTableName Here and Make Sure You Get SQL Table Name
                        string parentClass = memberExpression.Member.ReflectedType.Name;
                        Value = queryFormatter.Format(expressionType, memberExpression.Member.ReflectedType, memberExpression.Member);
                    }
                }
                else
                {
                    if (memberExpression.Expression is ParameterExpression parameterExpression)
                    {
                        string parentClass = memberExpression.Member.ReflectedType.Name;
                        Value = queryFormatter.Format(expressionType, memberExpression.Member.ReflectedType, memberExpression.Member);
                    }
                    else
                    {
                        LambdaExpression lamdaExpression = LambdaExpression.Lambda(memberExpression);
                        Value = lamdaExpression.Compile().DynamicInvoke() ?? DBNull.Value;
                    }
                }
                sb.Append(formatType(Value));
            }
            else if (expression is ConstantExpression constantExpression)
            {
                Value = (object)constantExpression.Value ?? DBNull.Value;
                if (stack.Count > 0)
                {
                    sb.Append(formatType(Value));
                }
                else
                {
                    if (Value is bool _value)
                    {
                        if (_value)
                        {
                            sb.Append(" 1 = 1");
                        }
                        else
                        {
                            sb.Append(" 1 != 1");
                        }
                    }
                }
            }
            else if (expression is MethodCallExpression methodCallExpression)
            {
                switch (methodCallExpression.Method.Name.ToLower())
                {
                    case "startswith":
                        {
                            MemberExpression member = ((MemberExpression)methodCallExpression.Object); ;
                            var value = ((ConstantExpression)methodCallExpression.Arguments[0]).Value.ToString();
                            var info = queryFormatter.Format(expressionType, member.Member.ReflectedType, member.Member);
                            sb.Append(" ").Append(info).Append(" LIKE '%").Append(value).Append("'");
                            break;
                        }
                    case "endswith":
                        {
                            MemberExpression member = ((MemberExpression)methodCallExpression.Object); ;
                            var value = ((ConstantExpression)methodCallExpression.Arguments[0]).Value.ToString();
                            var info = queryFormatter.Format(expressionType, member.Member.ReflectedType, member.Member);
                            sb.Append(" ").Append(info).Append(" LIKE '").Append(value).Append("%'");
                            break;
                        }
                    case "contains":
                        {
                            MemberExpression member = ((MemberExpression)methodCallExpression.Object); ;
                            var value = ((ConstantExpression)methodCallExpression.Arguments[0]).Value.ToString();
                            var info = queryFormatter.Format(expressionType, member.Member.ReflectedType, member.Member);
                            sb.Append(" ").Append(info).Append(" LIKE '%").Append(value).Append("%'");
                            break;
                        }
                    default:
                        {
                            LambdaExpression lambdaExpression = LambdaExpression.Lambda(methodCallExpression);
                            Value = lambdaExpression.Compile().DynamicInvoke() ?? DBNull.Value;
                            sb.Append(formatType(Value));
                            break;
                        }
                }
            }
            else if (expression is UnaryExpression convertExpression)
            {
                if (convertExpression.Operand is NewExpression newExpression)
                {
                    LambdaExpression lambdaExpression = LambdaExpression.Lambda(newExpression);
                    Value = lambdaExpression.Compile().DynamicInvoke() ?? DBNull.Value;
                }
                else if (convertExpression.Operand is MethodCallExpression callExpression)
                {
                    if (callExpression.Type == typeof(DBO))
                    {
                        LambdaExpression lambdaExpression = LambdaExpression.Lambda(callExpression);
                        DBO lamda = (DBO)lambdaExpression.Compile().DynamicInvoke();
                        Value = lamda.EExpression(expressionType);
                        if (stack.Count > 0)
                        {
                            sb.Append(convertNodeType(stack.Pop()));
                        }
                        sb.Append(Value.ToString());
                    }
                    else
                    {
                        if (stack.Count == 0)
                        {
                            throw new Exception("Invalid Expression");
                        }
                        else
                        {
                            LambdaExpression lambdaExpression = LambdaExpression.Lambda(callExpression);
                            Value = lambdaExpression.Compile().DynamicInvoke() ?? DBNull.Value;
                            sb.Append(convertNodeType(stack.Pop()));
                            sb.Append(Value.ToString());
                        }
                    }
                }
                else if (convertExpression.Operand is MemberExpression member && stack.Count > 0)
                {
                    LambdaExpression lambdaExpression = LambdaExpression.Lambda(member);
                    Value = lambdaExpression.Compile().DynamicInvoke() ?? DBNull.Value;
                    sb.Append(convertNodeType(stack.Pop()));
                    sb.Append(Value.ToString());
                }
                else if (convertExpression.Operand is MemberExpression member2 && stack.Count == 0)
                {
                    if (DSS.TryGetTableInfo(member2.Member.ReflectedType, out TableInfo tableInfo))
                    {
                        Value = queryFormatter.Format(expressionType, member2.Member.ReflectedType, member2.Member);
                        sb.Append(formatType(Value));
                    }
                    else
                    {
                        Value = LambdaExpression.Lambda(expression).Compile().DynamicInvoke();
                        sb.Append(formatType(Value));
                    }
                }
                else
                {
                    new ExpressionReader(convertExpression.Operand, expressionType, stack, sb, queryFormatter);
                }
            }
            else if (expression is ConditionalExpression conditionalExpression)
            {
                LambdaExpression lamdaExpression = LambdaExpression.Lambda(expression);
                Value = lamdaExpression.Compile().DynamicInvoke() ?? DBNull.Value;
                if (stack.Count > 0)
                {
                    sb.Append(convertNodeType(stack.Pop()));
                }
                sb.Append(formatType(Value));
            }
            else if (expression is NewExpression newExpression1)
            {
                if (stack.Count > 0)
                {
                    throw new Exception("Invalid Expression");
                }
                for (int index = 0; index < newExpression1.Arguments.Count; index++)
                {
                    if (newExpression1.Arguments[index] is MemberExpression argumentMemberExpression)
                    {
                        Value = queryFormatter.Format(expressionType, argumentMemberExpression.Member.ReflectedType, argumentMemberExpression.Member);
                        sb.Append(formatType(Value));
                    }
                    else
                    {
                        var name = newExpression1.Members[index].Name;
                        if (newExpression1.Arguments[index] is ConstantExpression argumentConstantExpression)
                        {
                            Value = new Name(name + " = " + formatType(argumentConstantExpression.Value));
                            sb.Append(formatType(Value));
                        }
                        else if (newExpression1.Arguments[index].Type == typeof(DBO))
                        {
                            LambdaExpression lambdaExpression = Expression.Lambda(newExpression1.Arguments[index]);
                            DBO lamda = (DBO)lambdaExpression.Compile().DynamicInvoke();
                            ExpressionTypes _expressionType = default;
                            switch (expressionType)
                            {
                                case ExpressionTypes.FullyEvaluated:
                                case ExpressionTypes.FullyEvaluatedWithAlias:
                                    _expressionType = ExpressionTypes.FullyEvaluated;
                                    break;
                                default:
                                    _expressionType = ExpressionTypes.FullyEvaluatedWithTypeName;
                                    break;
                            }
                            Value = new Name(lamda.EExpression(_expressionType).ToString());
                            sb.Append(formatType(Value) + " AS " + name);
                        }
                        else
                        {
                            throw new Exception("Unsupported Expression");
                        }
                    }
                    if (index < newExpression1.Arguments.Count - 1)
                    {
                        sb.Append(", ");
                    }
                }
            }
            else
            {
                throw new Exception("Unsupported Expression");
            }
        }
        private void read(Expression exp)
        {
            if (exp is BinaryExpression binaryExpression)
            {
                sb.Append("(");
                var leftExpression = new ExpressionReader(binaryExpression.Left, expressionType, stack, sb, queryFormatter);
                switch (binaryExpression.NodeType)
                {
                    case ExpressionType.Equal:
                    case ExpressionType.NotEqual:
                        {
                            stack.Push(binaryExpression.NodeType);
                            break;
                        }
                    default:
                        {
                            sb.Append(convertNodeType(binaryExpression.NodeType));
                            break;
                        }
                }
                var rightExpression = new ExpressionReader(binaryExpression.Right, expressionType, stack, sb, queryFormatter);
                sb.Append(")");
            }
            else
            {
                var value = new ExpressionReader(exp, expressionType, stack, sb, queryFormatter);
            }
        }
        private static string convertNodeType(ExpressionType expressionType)
        {
            switch (expressionType)
            {
                case ExpressionType.AndAlso: return " AND ";
                case ExpressionType.OrElse: return " OR ";
                case ExpressionType.Equal: return " = ";
                case ExpressionType.NotEqual: return " != ";
                case ExpressionType.GreaterThan: return " > ";
                case ExpressionType.GreaterThanOrEqual: return " >= ";
                case ExpressionType.LessThan: return " < ";
                case ExpressionType.LessThanOrEqual: return " <= ";
                default: throw new Exception("Unsupported Operation");
            }

        }
        private string formatType(object input)
        {
            return FormatType(input, ref stack, ref sb);
        }
        public static string FormatType(object input, ref Stack<ExpressionType> stack, ref StringBuilder sb)
        {
            switch (Type.GetTypeCode(input.GetType()))
            {
                case TypeCode.Boolean:
                    {
                        if (stack.Count > 0)
                        {
                            sb.Append(convertNodeType(stack.Pop()));
                        }
                        bool tmp = (bool)input;
                        return tmp ? "1" : "0";
                    }
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Double:
                case TypeCode.Single:
                case TypeCode.Decimal:
                    {
                        if (stack.Count > 0)
                        {
                            sb.Append(convertNodeType(stack.Pop()));
                        }
                        return input.ToString();
                    }
                case TypeCode.DBNull:
                    {
                        if (stack.Count > 0)
                        {
                            if (stack.Pop() == ExpressionType.Equal)
                            {
                                sb.Append(" IS ");
                            }
                            else
                            {
                                sb.Append(" IS NOT ");
                            }
                        }
                        return "null";
                    }
                case TypeCode.Object:
                    {
                        if (input is Name name)
                        {
                            return name.Value;
                        }
                        else
                        {
                            return null;
                        }
                    }
                default:
                    {
                        if (stack.Count > 0)
                        {
                            sb.Append(convertNodeType(stack.Pop()));
                        }
                        return $"'{input.ToString()}'";
                    }
            }
        }

        public static string FormatType(object input)
        {
            switch (Type.GetTypeCode(input.GetType()))
            {
                case TypeCode.Boolean:
                    {
                        bool tmp = (bool)input;
                        return tmp ? "1" : "0";
                    }
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Double:
                case TypeCode.Single:
                case TypeCode.Decimal:
                    {
                        return input.ToString();
                    }
                case TypeCode.DBNull:
                    {
                        return "null";
                    }
                case TypeCode.Object:
                    {
                        if (input is Name name)
                        {
                            return name.Value;
                        }
                        else
                        {
                            return null;
                        }
                    }
                default:
                    {
                        return $"'{input.ToString()}'";
                    }
            }
        }
    }
    public readonly struct Name
    {
        public string Value { get; }
        public Name(string value)
        {
            Value = value;
        }
        public override string ToString()
        {
            return Value;
        }
    }
    public enum ExpressionTypes
    {
        FullyEvaluated,
        FullyEvaluatedWithAlias,
        FullyEvaluatedWithTypeName,
        FullyEvaluatedWithTypeNameAndAlias,
        Alias
    }
}
