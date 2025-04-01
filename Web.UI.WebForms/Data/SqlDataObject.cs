using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace ParadimeWeb.WorkflowGen.Data
{
    public abstract class SqlDataObject<T> : JsonObject
        where T : SqlDataObject<T>, new()
    {
        private Dictionary<string, int> ordinals = null;
        private SqlDataReader sqlReader = null;
        public string Source { get; private set; }

        public T CreateInstance(SqlDataReader sqlReader, string source, string[] extraAttributes = null, object[] args = null)
        {
            var t = new T();
            t.Populate(sqlReader, source, extraAttributes, ordinals, args);
            return t;
        }

        public virtual void Populate(string key, string text, string source)
        {
            Source = source;
            Values.Add("key", key);
            Values.Add("text", text);
        }
        public void GetOrdinalsAndPopulate(SqlDataReader sqlReader, string source, string[] extraAttributes, Dictionary<string, int> extraOrdinals = null, params object[] args)
        {
            GetOrdinals(sqlReader, source, extraAttributes, extraOrdinals);
            Populate(sqlReader, source, extraAttributes, ordinals, args);
        }
        public virtual void Populate(SqlDataReader sqlReader, string source, string[] extraAttributes, Dictionary<string, int> ordinals, params object[] args)
        {
            this.sqlReader = sqlReader;
            if (this.ordinals == null)
            {
                this.ordinals = ordinals;
            }
            Source = source;
            if (extraAttributes != null)
            {
                foreach (var attribute in extraAttributes)
                {
                    setValue(attribute);
                }
            }
        }
        public Dictionary<string, int> GetOrdinals()
        {
            return ordinals;
        }

        public virtual void GetOrdinals(SqlDataReader sqlReader, string source, string[] extraAttributes, Dictionary<string, int> extraOrdinals)
        {
            if (ordinals == null)
            {
                this.sqlReader = sqlReader;
                Source = source;
                ordinals = extraOrdinals == null ? new Dictionary<string, int>() : new Dictionary<string, int>(extraOrdinals);
                if (extraAttributes != null)
                {
                    foreach (var colName in extraAttributes)
                    {
                        addOrdinal(colName);
                    }
                }
            }
            else
            {
                throw new Exception("GetOrdinals already called");
            }
        }

        protected void addOrdinal(string colName)
        {
            ordinals.Add(colName, sqlReader.GetOrdinal(colName));
        }
        public bool IsDBNull(string colName)
        {
            return sqlReader.IsDBNull(ordinals[colName]);
        }
        protected VType setValue<VType>(string jsonProperty, VType value)
        {
            Values.Add(jsonProperty, value);
            return value;
        }
        public object GetValue(string colName) => sqlReader.GetValue(ordinals[colName]);
        protected object setValue(string colName)
        {
            object value = GetValue(colName);
            Values.Add(colName, value);
            return value;
        }
        public string GetString(string colName) => sqlReader.GetString(ordinals[colName]);
        protected string setString(string colName, string jsonProperty)
        {
            string value = GetString(colName);
            Values.Add(jsonProperty, value);
            return value;
        }
        public string GetNullableString(string colName) => IsDBNull(colName) ? null : sqlReader.GetString(ordinals[colName]);
        protected string setNullableString(string colName, string jsonProperty)
        {
            string value = GetNullableString(colName);
            Values.Add(jsonProperty, value);
            return value;
        }
        public int GetInt32(string colName) => sqlReader.GetInt32(ordinals[colName]);
        protected int setInt32(string colName, string jsonProperty)
        {
            int value = GetInt32(colName);
            Values.Add(jsonProperty, value);
            return value;
        }
        public int? GetNullableInt32(string colName)
        {
            int? value = null;
            if (!IsDBNull(colName))
            {
                value = sqlReader.GetInt32(ordinals[colName]);
            }
            return value;
        }
        protected int? setNullableInt32(string colName, string jsonProperty)
        {
            int? value = GetNullableInt32(colName);
            Values.Add(jsonProperty, value);
            return value;
        }
        public decimal GetDecimal(string colName) => sqlReader.GetDecimal(ordinals[colName]);
        protected decimal setDecimal(string colName, string jsonProperty)
        {
            decimal value = GetDecimal(colName);
            Values.Add(jsonProperty, value);
            return value;
        }
        public decimal? GetNullableDecimal(string colName)
        {
            decimal? value = null;
            if (!IsDBNull(colName))
            {
                value = sqlReader.GetDecimal(ordinals[colName]);
            }
            return value;
        }
        protected decimal? setNullableDecimal(string colName, string jsonProperty)
        {
            decimal? value = GetNullableDecimal(colName);
            Values.Add(jsonProperty, value);
            return value;
        }
        public DateTime GetDateTime(string colName) => sqlReader.GetDateTime(ordinals[colName]);
        protected DateTime setDateTime(string colName, string jsonProperty)
        {
            DateTime value = GetDateTime(colName);
            Values.Add(jsonProperty, value);
            return value;
        }
        public DateTime? GetNullableDateTime(string colName)
        {
            DateTime? value = null;
            if (!IsDBNull(colName))
            {
                value = sqlReader.GetDateTime(ordinals[colName]);
            }
            return value;
        }
        protected DateTime? setNullableDateTime(string colName, string jsonProperty)
        {
            DateTime? value = GetNullableDateTime(colName);
            Values.Add(jsonProperty, value);
            return value;
        }
        public bool GetBooleanEquals(string colName, string equalValue) => sqlReader.GetString(ordinals[colName]) == equalValue;
        protected bool setBooleanEquals(string colName, string jsonProperty, string equalValue)
        {
            bool value = GetBooleanEquals(colName, equalValue);
            Values.Add(jsonProperty, value);
            return value;
        }
        public bool GetBoolean(string colName) => sqlReader.GetByte(ordinals[colName]) == 1;
        protected bool setBoolean(string colName, string jsonProperty)
        {
            bool value = GetBoolean(colName);
            Values.Add(jsonProperty, value);
            return value;
        }
    }
}
