using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ParadimeWeb.WorkflowGen.Data
{
    [Serializable]
    public abstract class JsonObject : ISerializable
    {
        public Dictionary<string, object> Values { get; private set; }

        public JsonObject()
        {
            Values = new Dictionary<string, object>();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            foreach (var kv in Values)
            {
                info.AddValue(kv.Key, kv.Value);
            }
        }
    }
}
