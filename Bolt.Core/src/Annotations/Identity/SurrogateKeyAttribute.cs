using Bolt.Core.Abstraction;

namespace Bolt.Core.Annotations
{
    public class SurrogateKeyAttribute : KeyVariant
    {
        public SurrogateKeyAttribute(): base() {

        }   
        public SurrogateKeyAttribute(string keyName): base(keyName) {

        }    
    }
}