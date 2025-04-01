using System.Data.SqlClient;
using ParadimeWeb.WorkflowGen.Data;

namespace ParadimeWeb.WorkflowGen.Data
{
    public static class ExtensionMethods
    {
        public static QueryResult<T> CreateQueryResult<T>(this SqlCommand comm, string fromSQL)
        {
            return new QueryResult<T>(comm, fromSQL);
        }
    }
}
