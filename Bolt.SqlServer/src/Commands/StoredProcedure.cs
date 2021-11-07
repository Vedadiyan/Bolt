using System.Data.SqlClient;
using System.Text;
using Bolt.Core.Abstraction;
using Bolt.Core.Interpretation;

namespace Bolt.SqlServer.Commands
{
    public class StoredProcedure<TArgument, TResult> : StoredProcedureBase<TArgument, TResult> where TArgument : class, new() where TResult : class, new()
    {
        public override void AddParameter(string name, object value)
        {
            if (parameters.Length > 0)
            {
                parameters.Append(",");
            }
            else {
                parameters.Append(" ");
            }
            parameters.Append("@").Append(name).Append("=").Append(ExpressionReader.FormatType(value));
        }

        public override string GetSqlQuery()
        {
            parameters.Insert(0, $"EXEC {StoredProcedure.StoredProcedureFullName}");
            return parameters.ToString();
        }
    }
}