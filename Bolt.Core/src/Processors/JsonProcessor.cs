using System;
using System.Text.Json;
using Bolt.Core.Abstraction;

namespace Bolt.Core.Processors {
    public class JsonProcessor: IProcessor {
        private readonly Type type;
        public JsonProcessor(Type type) {
            this.type = type;
        }
        public object Process(object obj)
        {
            return JsonSerializer.Deserialize(obj.ToString(), type);
        }
    }
}