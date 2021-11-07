using System;

namespace Bolt.Core.Abstraction {
    public abstract class CompsiteKeyVariant : Attribute {

        public bool IsKeyInfoSpecified { get; }
         public string CompositionGroup { get; }
        public string KeyName { get; }
        protected CompsiteKeyVariant(string compositionGroup) {
            IsKeyInfoSpecified = false;
            CompositionGroup = compositionGroup;
        }
        protected CompsiteKeyVariant(string keyName, string compositionGroup) {
            IsKeyInfoSpecified = true;
            KeyName = keyName;
            CompositionGroup = compositionGroup;
        }
    }
}