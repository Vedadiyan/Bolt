using System;

namespace Bolt.Core.Annotations
{
    public abstract class DefaultValueAttribute : Attribute
    {
        public abstract object Value { get; }
    }
}