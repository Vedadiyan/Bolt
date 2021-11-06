using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Bolt.Core.Abstraction
{
    public interface IQuery
    {
        CommandType CommandType { get; }
        string GetSqlQuery();
    }
}