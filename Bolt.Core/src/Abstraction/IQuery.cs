using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Bolt.Core.Abstraction {
    public interface IQuery {
        string GetSqlQuery();
    }
}