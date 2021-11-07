using System;

namespace Bolt.Core.Abstraction {
    public abstract class KeyVariant : Attribute {

        public bool IsKeyInfoSpecified { get; }
        public string KeyName { get; }
        protected KeyVariant() {
            IsKeyInfoSpecified = false;
        }
        protected KeyVariant(string keyName) {
            IsKeyInfoSpecified = true;
            KeyName = keyName;
        }
    }
}