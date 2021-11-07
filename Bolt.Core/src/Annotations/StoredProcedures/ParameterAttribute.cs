using System;

namespace Bolt.Core.Annotations
{
    public class ParameterAttribute : Attribute
    {
        public string Name { get; }
        public ParameterAttribute() { }
        public ParameterAttribute(string name)
        {
            Name = name;
        }
    }
}