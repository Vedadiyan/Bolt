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
        public ExpressionReader(Expression expression, ExpressionTypes expressionType, Stack<ExpressionType> stack, StringBuilder sb)
        {
            this.expressionType = expressionType;
            this.stack = stack;
            this.sb = sb;
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
                            Value = getValue(expressionType, innerMemberExpression.Type, memberExpression.Member);
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
                        Value = getValue(expressionType, memberExpression.Member.ReflectedType, memberExpression.Member);
                    }
                }
                else
                {
                    if (memberExpression.Expression is ParameterExpression parameterExpression)
                    {
                        string parentClass = memberExpression.Member.ReflectedType.Name;
                        Value = getValue(expressionType, memberExpression.Member.ReflectedType, memberExpression.Member);
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
                            var info = getValue(expressionType, member.Member.ReflectedType, member.Member);
                            sb.Append(" ").Append(info).Append(" LIKE '%").Append(value).Append("'");
                            break;
                        }
                    case "endswith":
                        {
                            MemberExpression member = ((MemberExpression)methodCallExpression.Object); ;
                            var value = ((ConstantExpression)methodCallExpression.Arguments[0]).Value.ToString();
                            var info = getValue(expressionType, member.Member.ReflectedType, member.Member);
                            sb.Append(" ").Append(info).Append(" LIKE '").Append(value).Append("%'");
                            break;
                        }
                    case "contains":
                        {
                            MemberExpression member = ((MemberExpression)methodCallExpression.Object); ;
                            var value = ((ConstantExpression)methodCallExpression.Arguments[0]).Value.ToString();
                            var info = getValue(expressionType, member.Member.ReflectedType, member.Member);
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
                        // Value = ((ConstantExpression)callExpression.Arguments[0]).Value.ToString() + "(";
                        // if (!(callExpression.Arguments[1] is ConstantExpression))
                        // {
                        //     LambdaExpression lambdaExpression = (LambdaExpression)((UnaryExpression)callExpression.Arguments[1]).Operand;
                        //     if (lambdaExpression.Body is UnaryExpression lamdaUnaryExpression)
                        //     {
                        //         MemberExpression member = ((MemberExpression)((UnaryExpression)lamdaUnaryExpression).Operand);
                        //         Value += getValue(expressionType, member.Member.ReflectedType, member.Member).ToString();
                        //     }
                        //     else if (lambdaExpression.Body is NewExpression lamdaNewExpression)
                        //     {
                        //         string[] memberNames = new string[lamdaNewExpression.Members.Count];
                        //         for (int memberIndex = 0; memberIndex < lamdaNewExpression.Arguments.Count; memberIndex++)
                        //         {
                        //             var arg = lamdaNewExpression.Arguments[memberIndex];
                        //             if (arg is MemberExpression argMemberExpression && argMemberExpression.Expression is ParameterExpression argParameterExpression)
                        //             {
                        //                 memberNames[memberIndex] = getValue(expressionType, argParameterExpression.Type, argMemberExpression.Member).ToString();
                        //             }
                        //             else
                        //             {
                        //                 throw new Exception("Unsupported Expression");
                        //             }
                        //         }
                        //         Value += String.Join(',', memberNames);
                        //     }
                        //     else if (lambdaExpression.Body is MemberExpression lamdaMemberExpression)
                        //     {
                        //         Value += getValue(expressionType, lamdaMemberExpression.Member.ReflectedType, lamdaMemberExpression.Member).ToString();
                        //     }
                        // }
                        // Value += ")";
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
                        Value = getValue(expressionType, member2.Member.ReflectedType, member2.Member);
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
                    new ExpressionReader(convertExpression.Operand, expressionType, stack, sb);
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
                        Value = getValue(expressionType, argumentMemberExpression.Member.ReflectedType, argumentMemberExpression.Member);
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
                            //Value = new Name(name + " = " + lamda.EExpression(expressionType).ToString());
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
                var leftExpression = new ExpressionReader(binaryExpression.Left, expressionType, stack, sb);
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
                var rightExpression = new ExpressionReader(binaryExpression.Right, expressionType, stack, sb);
                sb.Append(")");
            }
            else
            {
                var value = new ExpressionReader(exp, expressionType, stack, sb);
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
        internal static object getValue(ExpressionTypes expressionType, Type type, MemberInfo member)
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
