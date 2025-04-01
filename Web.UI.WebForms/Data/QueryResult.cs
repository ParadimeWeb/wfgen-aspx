using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace ParadimeWeb.WorkflowGen.Data
{
    public class QueryResult<T>
    {
        public int Total { get; private set; }
        public IList<T> Rows { get; protected set; }
        public QueryResult() { }
        public QueryResult(SqlCommand comm, string fromSql)
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
        }
        public QueryResult(IList<T> rows, int total)
        {
            Rows = rows;
            Total = total;
        }
        public QueryResult(IList<T> rows) : this(rows, rows.Count()) { }
    }
}
