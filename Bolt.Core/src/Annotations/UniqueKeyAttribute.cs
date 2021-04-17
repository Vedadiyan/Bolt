using Bolt.Core.Abstraction;

namespace Bolt.Core.Annotations
{
    public class UniqueKeyAttribute : KeyVariant
    {
        public UniqueKeyAttribute(): base() {

        }   
        public UniqueKeyAttribute(string keyName): base(keyName) {

        }    
    }
}