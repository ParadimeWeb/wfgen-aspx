using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.UI;

namespace ParadimeWeb.WorkflowGen.Data
{
    public class QueryResult<T>
    {
        public int Total { get; private set; }
        public bool HasNextPage { get; private set; }
        public IList<T> Rows { get; protected set; }
        public QueryResult() { }
        public QueryResult(SqlCommand comm, string fromSql, int page, int pageSize)
        {
            Rows = new List<T>();
            Total = 0;
            comm.CommandText = $"SELECT COUNT(*) {fromSql}";
            using (var r = comm.ExecuteReader())
            {
                if (r.Read())
                {
                    Total = r.GetInt32(0);
                }
            }
            HasNextPage = Total > page * pageSize;
        }
        public QueryResult(IList<T> rows, int total, int page, int pageSize)
        {
            Rows = rows;
            Total = total;
            HasNextPage = total > page * pageSize;
        }
        public QueryResult(IList<T> rows) 
        {
            Total = rows.Count();
            Rows = rows;
            HasNextPage = false;
        }
    }
}
