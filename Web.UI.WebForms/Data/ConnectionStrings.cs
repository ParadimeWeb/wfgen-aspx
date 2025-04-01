using System.Configuration;

namespace ParadimeWeb.WorkflowGen.Data
{
    public static class ConnectionStrings
    {
        public static string MainDbSource = ConfigurationManager.ConnectionStrings["MainDbSource"].ConnectionString;
    }
}
