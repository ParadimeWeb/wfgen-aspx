using System.Data.SqlClient;

namespace ParadimeWeb.WorkflowGen.Data
{
    public static class ExtensionMethods
    {
        public static QueryResult<T> CreateQueryResult<T>(this SqlCommand comm, string fromSQL, int page, int pageSize)
        {
            return new QueryResult<T>(comm, fromSQL, page, pageSize);
        }
    }
}
