using Bolt.Core.Abstraction;

namespace Bolt.Core.Annotations
{
    public class CompositeSurrogateKeyAttribute : CompsiteKeyVariant
    {
        public CompositeSurrogateKeyAttribute(string compositionGroup) : base(compositionGroup)
        {
        }
        public CompositeSurrogateKeyAttribute(string keyName, string compositionGroup) : base(keyName, compositionGroup)
        {
        }
    }
}