using System;

namespace Bolt.Core.Abstraction
{
    public class Convertible
    {
        public object Value { get; }
        public Convertible(object value)
        {
            Value = value;
        }
        public override string ToString()
        {
            return Value.ToString();
        }
        public static implicit operator int(Convertible convertible)
        {
            return default;
        }
        public static implicit operator byte(Convertible convertible)
        {
            return default;
        }
        public static implicit operator long(Convertible convertible)
        {
            return default;
        }
        public static implicit operator double(Convertible convertible)
        {
            return default;
        }
        public static implicit operator decimal(Convertible convertible)
        {
            return default;
        }
        public static implicit operator float(Convertible convertible)
        {
            return default;
        }
        public static implicit operator short(Convertible convertible)
        {
            return default;
        }
        public static implicit operator uint(Convertible convertible)
        {
            return default;
        }
        public static implicit operator sbyte(Convertible convertible)
        {
            return default;
        }
        public static implicit operator ulong(Convertible convertible)
        {
            return default;
        }
        public static implicit operator ushort(Convertible convertible)
        {
            return default;
        }
        public static implicit operator DateTime(Convertible convertible)
        {
            return default;
        }
        public static implicit operator string(Convertible convertible)
        {
            return default;
        }
        public static implicit operator bool(Convertible convertible)
        {
            return default;
        }
        public static implicit operator char(Convertible convertible)
        {
            return default;
        }
    }
    public class Convertible<T> : Convertible
    {

        private Convertible(string value) : base(value)
        {

        }
        public static implicit operator T(Convertible<T> max)
        {
            return default;
        }
        public override string ToString()
        {
            return base.ToString();
        }

    }
}