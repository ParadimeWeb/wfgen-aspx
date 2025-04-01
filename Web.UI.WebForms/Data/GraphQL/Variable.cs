using Newtonsoft.Json.Linq;

namespace ParadimeWeb.WorkflowGen.Data.GraphQL
{
    public class Variable
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public JToken Value { get; set; }
        public Variable(string name, JToken value, string type = "String!")
        {
            Type = type;
            Name = name;
            Value = value;
        }
    }
}
