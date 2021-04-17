using Bolt.Core.Abstraction;

namespace Bolt.Core.Annotations
{
    public class CompositePrimaryKeyAttribute : CompsiteKeyVariant
    {
        public CompositePrimaryKeyAttribute(string compositionGroup) : base(compositionGroup)
        {
        }
        public CompositePrimaryKeyAttribute(string keyName, string compositionGroup) : base(keyName, compositionGroup)
        {
        }
    }
}