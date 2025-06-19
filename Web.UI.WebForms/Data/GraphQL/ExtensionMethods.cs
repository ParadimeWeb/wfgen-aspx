using System;
using System.Text;

namespace ParadimeWeb.WorkflowGen.Data.GraphQL
{
    public static class ExtensionMethods
    {
        public static string ToEncodedUserId(this int id)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes($"User:{id}"));
        }
    }
}
