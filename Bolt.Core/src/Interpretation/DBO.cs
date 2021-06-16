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
        public DBO(Func<ExpressionTypes, Convertible> expression)
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