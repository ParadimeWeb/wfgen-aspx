using ParadimeWeb.WorkflowGen.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.Serialization;

namespace ParadimeWeb.WorkflowGen.Web.UI.WebForms.Model
{
    public class UserColumn
    {
        public const string Id = "Id";
        public const string UserName = "UserName";
        public const string EmployeeNumber = "EmployeeNumber";
        public const string CommonName = "CommonName";
        public const string FirstName = "FirstName";
        public const string LastName = "LastName";
        public const string Email = "Email";
        public const string JobTitle = "JobTitle";
        public const string Directory = "Directory";
        public const string Locale = "Locale";
        public const string TimezoneId = "TimeZoneId";
        public const string IsActive = "IsActive";
    }
    [Serializable]
    public class User : SqlDataObject<User>
    {
        public int Id => (int)Values[UserColumn.Id];
        public string UserName => (string)Values[UserColumn.UserName];
        public string EmployeeNumber => Values[UserColumn.EmployeeNumber] == null ? null : (string)Values[UserColumn.EmployeeNumber];
        public string CommonName => (string)Values[UserColumn.CommonName];
        public string FirstName => (string)Values[UserColumn.FirstName];
        public string LastName => (string)Values[UserColumn.LastName];
        public string Email => (string)Values[UserColumn.Email];
        public string JobTitle => (string)Values[UserColumn.JobTitle];
        public string Locale => (string)Values[UserColumn.Locale];
        public int TimezoneId => (int)Values[UserColumn.TimezoneId];
        public string Directory => (string)Values[UserColumn.Directory];

        public User() { }
        public User(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");

            Values.Add(UserColumn.Id, info.GetInt32(UserColumn.Id));
            Values.Add(UserColumn.UserName, info.GetString(UserColumn.UserName));
            Values.Add(UserColumn.EmployeeNumber, info.GetString(UserColumn.EmployeeNumber));
            Values.Add(UserColumn.CommonName, info.GetString(UserColumn.CommonName));
            Values.Add(UserColumn.FirstName, info.GetString(UserColumn.FirstName));
            Values.Add(UserColumn.LastName, info.GetString(UserColumn.LastName));
            Values.Add(UserColumn.Email, info.GetString(UserColumn.Email));
            Values.Add(UserColumn.JobTitle, info.GetString(UserColumn.JobTitle));
            Values.Add(UserColumn.Locale, info.GetString(UserColumn.Locale));
            Values.Add(UserColumn.TimezoneId, info.GetString(UserColumn.TimezoneId));
            Values.Add(UserColumn.Directory, info.GetString(UserColumn.Directory));
        }
        public User(DataRow row)
        {
            foreach (DataColumn col in row.Table.Columns)
            {
                Values.Add(col.ColumnName, row[col] == DBNull.Value ? null : row[col]);
            }
        }
        public User(int id, string userName, string employeeNumber, string commonName, string firstName, string lastName, string email, string jobTitle, string locale, int timezoneId, string directory = null, params KeyValuePair<string, object>[] extendedAttributes)
        {
            Values.Add(UserColumn.Id, id);
            Values.Add(UserColumn.UserName, userName);
            Values.Add(UserColumn.EmployeeNumber, employeeNumber);
            Values.Add(UserColumn.CommonName, commonName == null ? $"{firstName} {lastName}" : commonName);
            Values.Add(UserColumn.FirstName, firstName);
            Values.Add(UserColumn.LastName, lastName);
            Values.Add(UserColumn.Email, email);
            Values.Add(UserColumn.JobTitle, jobTitle);
            Values.Add(UserColumn.Locale, locale);
            Values.Add(UserColumn.TimezoneId, timezoneId);
            Values.Add(UserColumn.Directory, directory);

            if (extendedAttributes != null)
            {
                foreach (var extendedAttribute in extendedAttributes)
                {
                    Values.Add(extendedAttribute.Key, extendedAttribute.Value);
                }
            }
        }

        public override void GetOrdinals(SqlDataReader sqlReader, string source, string[] extraAttributes, Dictionary<string, int> extraOrdinals)
        {
            base.GetOrdinals(sqlReader, source, extraAttributes, extraOrdinals);

            addOrdinal("ID_USER");
            addOrdinal("USERNAME");
            addOrdinal("FIRSTNAME");
            addOrdinal("LASTNAME");
            addOrdinal("ADS_CN");
            addOrdinal("EMPLOYEENUMBER");
            addOrdinal("EMAIL");
            addOrdinal("JOBTITLE");
            addOrdinal("UP_LANGUAGE_PREF");
            addOrdinal("LANGUAGE");
            addOrdinal("UP_ID_TIMEZONE_PREF");
            addOrdinal("ID_TIMEZONE");
            addOrdinal("DIRNAME");
        }
        public override void Populate(SqlDataReader r, string source, string[] extraAttributes, Dictionary<string, int> ordinals, params object[] args)
        {
            base.Populate(r, source, extraAttributes, ordinals, args);

            var firstName = GetNullableString("FIRSTNAME");
            Values.Add(UserColumn.FirstName, firstName);
            var lastName = GetString("LASTNAME");
            Values.Add(UserColumn.LastName, lastName);

            if (firstName == null) firstName = string.Empty;
            Values.Add(UserColumn.CommonName, IsDBNull("ADS_CN") ? $"{firstName} {lastName}" : GetString("ADS_CN"));

            Values.Add(UserColumn.Id, GetInt32("ID_USER"));
            Values.Add(UserColumn.UserName, GetString("USERNAME"));
            Values.Add(UserColumn.EmployeeNumber, GetNullableString("EMPLOYEENUMBER"));
            Values.Add(UserColumn.Email, GetNullableString("EMAIL"));
            Values.Add(UserColumn.JobTitle, GetNullableString("JOBTITLE"));
            Values.Add(UserColumn.Locale, IsDBNull("UP_LANGUAGE_PREF") ? GetNullableString("LANGUAGE") : GetString("UP_LANGUAGE_PREF"));
            Values.Add(UserColumn.TimezoneId, IsDBNull("UP_ID_TIMEZONE_PREF") ? IsDBNull("ID_TIMEZONE") ? 0 : GetInt32("ID_TIMEZONE") : Convert.ToInt32(GetString("UP_ID_TIMEZONE_PREF")));
            Values.Add(UserColumn.Directory, GetString("DIRNAME"));
            
        }
        public void Set(DataRow row)
        {
            foreach (DataColumn col in row.Table.Columns)
            {
                row[col] = Values.ContainsKey(col.ColumnName) ? Values[col.ColumnName] : DBNull.Value;
            }
        }
        public void SetApprovalApprover(DataRow row)
        {
            row[ApprovalColumn.ApproverEmail] = Email;
            row[ApprovalColumn.ApproverEmployeeNumber] = EmployeeNumber;
            row[ApprovalColumn.ApproverName] = CommonName;
            row[ApprovalColumn.ApproverUserName] = UserName;
        }
        public void SetApprovalApprovedBy(DataRow row)
        {
            row[ApprovalColumn.ApprovedByEmail] = Email;
            row[ApprovalColumn.ApprovedByEmployeeNumber] = EmployeeNumber;
            row[ApprovalColumn.ApprovedByName] = CommonName;
            row[ApprovalColumn.ApprovedByUserName] = UserName;
        }
        public static User GetUser(string userName, string employeeId, params string[] extraAttributes)
        {
            User user = null;
            using (var db = new DataBaseContext())
            {
                user = !string.IsNullOrEmpty(userName) ? db.GetUser<User>(userName, extraAttributes) : !string.IsNullOrEmpty(employeeId) ? db.GetUserByEmployeeId<User>(employeeId, extraAttributes) : null;
            }
            return user;
        }

        public static User GetManager(string userName, params string[] extraAttributes)
        {
            User manager = null;
            using (var db = new DataBaseContext())
            {
                manager = db.GetManager<User>(userName, extraAttributes);
            }
            return manager;
        }

        public static QueryResult<User> GetUsers(string q, string active = null, string archive = null, string dir = null, string[] extraAttributes = null, int page = 1, int pageSize = 20)
        {
            QueryResult<User> results;
            using (var db = new DataBaseContext())
            {
                results = db.GetUsers<User>(q, active, archive, string.IsNullOrEmpty(dir) ? new string[0] : dir.Split(','), extraAttributes ?? new string[0], page, pageSize);
            }
            return results;
        }

        public static QueryResult<User> GetGroupUsers(string name, string q, string active = null, string archive = null, string dir = null, string[] extraAttributes = null, int page = 1, int pageSize = 20)
        {
            QueryResult<User> results;
            using (var db = new DataBaseContext())
            {
                results = db.GetGroupUsers<User>(name, q, active, archive, string.IsNullOrEmpty(dir) ? new string[0] : dir.Split(','), extraAttributes ?? new string[0], page, pageSize);
            }
            return results;
        }

        public static QueryResult<User> GetLocalProcessParticipantUsers(string name, int processId, string q, string[] extraAttributes = null, int page = 1, int pageSize = 20)
        {
            QueryResult<User> results;
            using (var db = new DataBaseContext())
            {
                results = db.GetLocalProcessParticipantUsers<User>(name, processId, q, extraAttributes ?? new string[0], page, pageSize);
            }
            return results;
        }
        public static QueryResult<User> GetLocalProcessParticipantUsers(string name, string processName, string q, string[] extraAttributes = null, int page = 1, int pageSize = 20)
        {
            QueryResult<User> results;
            using (var db = new DataBaseContext())
            {
                results = db.GetLocalProcessParticipantUsers<User>(name, processName, q, extraAttributes: extraAttributes ?? new string[0], page: page, pageSize: pageSize);
            }
            return results;
        }
    }
}
