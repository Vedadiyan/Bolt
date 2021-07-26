using System;
using System.Reflection;
using Bolt.Core.Interpretation;

namespace Bolt.Core.Abstraction {
    public interface IQueryFormatter {
        object Format(ExpressionTypes expressionType, Type type, MemberInfo member);
         string Format(string input);
    }
}