using System;
using System.Data.SqlClient;

namespace ParadimeWeb.WorkflowGen.Data
{
    public class DataConnection : IDisposable
    {
        public SqlConnection Connection { get; }
        public DataConnection(string connectionString)
        {
            Connection = new SqlConnection(connectionString);
        }
        public void EnsureConnectionIsOpen()
        {
            if (Connection.State != System.Data.ConnectionState.Open)
            {
                Connection.Open();
            }
        }
        public void Dispose()
        {
            if (Connection != null)
            {
                if (Connection.State != System.Data.ConnectionState.Closed)
                {
                    Connection.Close();
                }
                Connection.Dispose();
            }
        }
    }
}
