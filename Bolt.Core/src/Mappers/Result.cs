using System;
using System.Collections.Generic;
using System.Dynamic;
using Bolt.Core.Abstraction;

namespace Bolt.Core.Mappers {
    public class Result: IResult
    {
        private Dictionary<Type, Object> result;
        public Result(Dictionary<Type, Object> result)
        {
            this.result = result;
        }
        public T GetEntity<T>()
        {
            if (result.TryGetValue(typeof(T), out object value))
            {
                return (T)value;
            }
            else
            {
                return default;
            }
        }
        public dynamic GetUnbindValues()
        {
            if (result.TryGetValue(typeof(ExpandoObject), out object value))
            {
                return value;
            }
            else
            {
                return default;
            }
        }
    }
}