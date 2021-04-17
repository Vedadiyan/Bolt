using Bolt.Core.Abstraction;

namespace Bolt.Core.Annotations
{
    public class CompositeUniqueKeyAttribute : CompsiteKeyVariant
    {
        public CompositeUniqueKeyAttribute(string compositionGroup) : base(compositionGroup)
        {
        }
        public CompositeUniqueKeyAttribute(string keyName, string compositionGroup) : base(keyName, compositionGroup)
        {
        }
    }
}