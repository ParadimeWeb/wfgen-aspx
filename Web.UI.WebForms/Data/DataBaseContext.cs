using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace ParadimeWeb.WorkflowGen.Data
{
    public class DataBaseContext : DataConnection
    {
        public const string SOURCE = "WFGEN";
        private const string SELECT_USERPROFILES = @"
    u.*
    ,d.DIRNAME
    ,d.DESCRIPTION AS DIR_DESCRIPTION
    ,upl.VALUE AS UP_LANGUAGE_PREF
    ,uptz.VALUE AS UP_ID_TIMEZONE_PREF";
        private const string QUERY_USERPROFILES = @"
FROM 
	USERS u
	JOIN DIRECTORY d ON d.ID_DIRECTORY = u.ID_DIRECTORY
	LEFT JOIN USERS_PREFS upl ON upl.ID_USER = u.ID_USER AND upl.NAME = 'LANGUAGE_PREF'
    LEFT JOIN USERS_PREFS uptz ON uptz.ID_USER = u.ID_USER AND uptz.NAME = 'ID_TIMEZONE_PREF'";
        private const string FILTER_SEARCHUSERS = "((COALESCE(FIRSTNAME + ' ', '') + LASTNAME) LIKE @query OR (LASTNAME + COALESCE(', ' + FIRSTNAME, '')) LIKE @query)";
        private string filterEmployeeNumber(string table = "u") => $"REPLACE(LTRIM(REPLACE({table}.EXTATT_1, '0', ' ')), ' ', '0') = @EMPLOYEENUMBER";
        public DataBaseContext() : base(ConnectionStrings.MainDbSource) { }
        public DataBaseContext(string connectionString) : base(connectionString) { }

        private T getUser<T>(Dictionary<string, KeyValuePair<string, object>> filters, params string[] extraAttributes) where T : SqlDataObject<T>, new()
        {
            if (filters.Count == 0) return null;
            T up = null;
            using (var comm = Connection.CreateCommand())
            {
                EnsureConnectionIsOpen();
                comm.CommandText = $@"SELECT{SELECT_USERPROFILES}{QUERY_USERPROFILES}
WHERE 
	{string.Join(" AND ", filters.Select(i => i.Value.Key))}
";
                foreach (var kv in filters)
                {
                    comm.Parameters.AddWithValue(kv.Key, kv.Value.Value);
                }

                using (var r = comm.ExecuteReader())
                {
                    if (r.Read())
                    {
                        up = new T();
                        up.GetOrdinalsAndPopulate(r, SOURCE, extraAttributes);
                    }
                }
            }
            return up;
        }
        private T getManager<T>(Dictionary<string, KeyValuePair<string, object>> filters, params string[] extraAttributes) where T : SqlDataObject<T>, new()
        {
            if (filters.Count == 0) return null;
            T up = null;
            using (var comm = Connection.CreateCommand())
            {
                EnsureConnectionIsOpen();
                comm.CommandText = $@"SELECT{SELECT_USERPROFILES}{QUERY_USERPROFILES}
    JOIN USERS e ON e.ID_USER_MANAGER = u.ID_USER
WHERE 
	{string.Join(" AND ", filters.Select(i => i.Value.Key))}
";
                foreach (var kv in filters)
                {
                    comm.Parameters.AddWithValue(kv.Key, kv.Value.Value);
                }

                using (var r = comm.ExecuteReader())
                {
                    if (r.Read())
                    {
                        up = new T();
                        up.GetOrdinalsAndPopulate(r, SOURCE, extraAttributes);
                    }
                }
            }
            return up;
        }

        public List<T> GetUserProfiles<T>(params string[] extraAttributes) where T : SqlDataObject<T>, new()
        {
            var items = new List<T>();
            using (var comm = Connection.CreateCommand())
            {
                EnsureConnectionIsOpen();
                comm.CommandText = $"SELECT{SELECT_USERPROFILES}{QUERY_USERPROFILES}";
                using (var r = comm.ExecuteReader())
                {
                    var factory = new T();
                    factory.GetOrdinals(r, SOURCE, extraAttributes, null);
                    while (r.Read())
                    {
                        items.Add(factory.CreateInstance(r, SOURCE, extraAttributes));
                    }
                }
            }
            return items;
        }
        public T GetUser<T>(int id, params string[] extraAttributes) where T : SqlDataObject<T>, new()
        {
            return getUser<T>(new Dictionary<string, KeyValuePair<string, object>> {
                { "@ID_USER", new KeyValuePair<string, object>("u.ID_USER = @ID_USER", id) }
            }, extraAttributes);
        }
        public T GetUser<T>(string userName, params string[] extraAttributes) where T : SqlDataObject<T>, new()
        {
            return getUser<T>(new Dictionary<string, KeyValuePair<string, object>> {
                { "@USERNAME", new KeyValuePair<string, object>("u.USERNAME = @USERNAME", userName) }
            }, extraAttributes);
        }
        public T GetUserByEmployeeId<T>(string employeeId, params string[] extraAttributes) where T : SqlDataObject<T>, new()
        {
            return getUser<T>(new Dictionary<string, KeyValuePair<string, object>> {
                { "@EMPLOYEENUMBER", new KeyValuePair<string, object>($"u.EMPLOYEENUMBER = @EMPLOYEENUMBER OR {filterEmployeeNumber()}", employeeId) }
            }, extraAttributes);
        }
        public T GetUserByEmail<T>(string email, params string[] extraAttributes) where T : SqlDataObject<T>, new()
        {
            return getUser<T>(new Dictionary<string, KeyValuePair<string, object>> {
                { "@EMAIL", new KeyValuePair<string, object>($"u.EMAIL = @EMAIL", email) }
            }, extraAttributes);
        }
        public T GetManager<T>(int id, params string[] extraAttributes) where T : SqlDataObject<T>, new()
        {
            return getManager<T>(new Dictionary<string, KeyValuePair<string, object>> {
                { "@ID_USER", new KeyValuePair<string, object>("e.ID_USER = @ID_USER", id) }
            }, extraAttributes);
        }
        public T GetManager<T>(string userName, params string[] extraAttributes) where T : SqlDataObject<T>, new()
        {
            return this.getManager<T>(new Dictionary<string, KeyValuePair<string, object>> {
                { "@USERNAME", new KeyValuePair<string, object>("e.USERNAME = @USERNAME", userName) }
            }, extraAttributes);
        }
        public T GetManagerByEmployeeId<T>(string employeeId, params string[] extraAttributes) where T : SqlDataObject<T>, new()
        {
            return this.getManager<T>(new Dictionary<string, KeyValuePair<string, object>> {
                { "@EMPLOYEENUMBER", new KeyValuePair<string, object>($"e.EMPLOYEENUMBER = @EMPLOYEENUMBER OR {filterEmployeeNumber("e")}", employeeId) }
            }, extraAttributes);
        }
        public QueryResult<T> GetUsers<T>(string q, string active = null, string archive = null, string[] dir = null, string[] extraAttributes = null, int page = 1, int pageSize = 20)
            where T : SqlDataObject<T>, new()
        {
            QueryResult<T> results;
            var filters = new List<string>();
            using (var comm = Connection.CreateCommand())
            {
                EnsureConnectionIsOpen();
                if (active != null)
                {
                    filters.Add("ACTIVE = @active");
                    comm.Parameters.AddWithValue("@active", active);
                }
                if (archive != null)
                {
                    filters.Add("ARCHIVE = @archive");
                    comm.Parameters.AddWithValue("@archive", archive);
                }
                if (dir != null && dir.Length > 0)
                {
                    filters.Add($"DIRNAME IN ('{string.Join("', '", dir)}')");
                }
                var fromSql = $@"{QUERY_USERPROFILES}
WHERE
    {FILTER_SEARCHUSERS}
    {(filters.Any() ? $"AND {string.Join(" AND ", filters)}" : string.Empty)}
";
                comm.Parameters.AddWithValue("@query", q + "%");
                results = comm.CreateQueryResult<T>(fromSql);
                comm.CommandText = $@"SELECT {SELECT_USERPROFILES}{fromSql}
ORDER BY
    LASTNAME, FIRSTNAME
OFFSET {(page - 1) * pageSize} ROWS
FETCH NEXT {pageSize} ROWS ONLY
";
                using (var r = comm.ExecuteReader())
                {
                    var factory = new T();
                    factory.GetOrdinals(r, SOURCE, extraAttributes, null);
                    while (r.Read())
                    {
                        results.Rows.Add(factory.CreateInstance(r, SOURCE, extraAttributes));
                    }
                }
            }
            return results;
        }
        public QueryResult<T> GetGroupUsers<T>(string name, string q, string active = null, string archive = null, string[] dir = null, string[] extraAttributes = null, int page = 1, int pageSize = 20)
            where T : SqlDataObject<T>, new()
        {
            QueryResult<T> results;
            var filters = new List<string>();
            using (var comm = Connection.CreateCommand())
            {
                EnsureConnectionIsOpen();
                if (active != null)
                {
                    filters.Add("ACTIVE = @active");
                    comm.Parameters.AddWithValue("@active", active);
                }
                if (archive != null)
                {
                    filters.Add("ARCHIVE = @archive");
                    comm.Parameters.AddWithValue("@archive", archive);
                }
                if (dir != null && dir.Length > 0)
                {
                    filters.Add($"DIRNAME IN ('{string.Join("', '", dir)}')");
                }
                var fromSql = $@"{QUERY_USERPROFILES}
    JOIN USERS_GROUPS ug ON ug.ID_USER = u.ID_USER
	JOIN GROUPS g ON g.ID_GROUP = ug.ID_GROUP
WHERE
    GROUPNAME = @name
    {(filters.Any() ? $"AND {string.Join(" AND ", filters)}" : string.Empty)}
    {(string.IsNullOrWhiteSpace(q) ? "" : $@"AND {FILTER_SEARCHUSERS}")}";

                comm.Parameters.AddWithValue("@name", name);
                comm.Parameters.AddWithValue("@query", q + "%");
                results = comm.CreateQueryResult<T>(fromSql);

                comm.CommandText = $@"SELECT {SELECT_USERPROFILES}{fromSql}
ORDER BY
    LASTNAME, FIRSTNAME
OFFSET {(page - 1) * pageSize} ROWS
FETCH NEXT {pageSize} ROWS ONLY
";
                using (var r = comm.ExecuteReader())
                {
                    var factory = new T();
                    factory.GetOrdinals(r, SOURCE, extraAttributes, null);
                    while (r.Read())
                    {
                        results.Rows.Add(factory.CreateInstance(r, SOURCE, extraAttributes));
                    }
                }
            }

            return results;
        }
        public QueryResult<T> GetLocalProcessParticipantUsers<T>(string name, string processName, string q, int? processVersion = null, string[] extraAttributes = null, int page = 1, int pageSize = 20)
            where T : SqlDataObject<T>, new()
        {
            QueryResult<T> results = new QueryResult<T>(new List<T>());

            using (var comm = Connection.CreateCommand())
            {
                EnsureConnectionIsOpen();
                var filters = new List<string>();
                filters.Add("NAME = @processName");
                comm.Parameters.AddWithValue("@processName", processName);
                if (processVersion.HasValue)
                {
                    filters.Add("VERSION = @processVersion");
                    comm.Parameters.AddWithValue("@processVersion", processVersion.Value);
                }
                comm.CommandText = $"SELECT ID_PROCESS FROM WFPROCESS WHERE {string.Join(" AND ", filters)} ORDER BY VERSION DESC";
                var r = comm.ExecuteReader();
                if (r.Read())
                {
                    int processId = r.GetInt32(0);
                    r.Close();
                    comm.Parameters.Clear();
                    var fromSql = $@"{QUERY_USERPROFILES}
JOIN
	(SELECT 
		u.ID_USER
	FROM
		WFPROCESS_PARTICIPANT pp
	JOIN
		WFPARTICIPANT p on pp.ID_PARTICIPANT = p.ID_PARTICIPANT
	JOIN 
		WFPARTICIPANT_MAPTO pm on pm.ID_PARTICIPANT = p.ID_PARTICIPANT
	LEFT JOIN 
		USERS_GROUPS ug on pm.ID_GROUP = ug.ID_GROUP
	JOIN
		USERS u on u.ID_DIRECTORY = pm.ID_DIRECTORY OR u.ID_USER = ug.ID_USER OR u.ID_USER = pm.ID_USER
	WHERE
		pp.ID_PROCESS = @processId
		AND p.GLOBAL_SCOPE = 'N'
		AND p.NAME = @name
		AND u.ACTIVE = 'Y'
	GROUP BY u.ID_USER) filtered ON filtered.ID_USER = u.ID_USER{(string.IsNullOrWhiteSpace(q) ? "" : $@"
WHERE
{FILTER_SEARCHUSERS}")}
";

                    comm.Parameters.AddWithValue("@processId", processId);
                    comm.Parameters.AddWithValue("@name", name);
                    comm.Parameters.AddWithValue("@query", q + "%");
                    results = comm.CreateQueryResult<T>(fromSql);

                    comm.CommandText = $@"SELECT {SELECT_USERPROFILES}{fromSql}
ORDER BY
LASTNAME, FIRSTNAME
OFFSET {(page - 1) * pageSize} ROWS
FETCH NEXT {pageSize} ROWS ONLY
";
                    r = comm.ExecuteReader();

                    var factory = new T();
                    factory.GetOrdinals(r, SOURCE, extraAttributes, null);
                    while (r.Read())
                    {
                        results.Rows.Add(factory.CreateInstance(r, SOURCE, extraAttributes));
                    }
                    r.Close();
                }
            }

            return results;
        }
        public QueryResult<T> GetLocalProcessParticipantUsers<T>(string name, int processId, string q, string[] extraAttributes = null, int page = 1, int pageSize = 20)
            where T : SqlDataObject<T>, new()
        {
            QueryResult<T> results;

            using (var comm = Connection.CreateCommand())
            {
                EnsureConnectionIsOpen();
                var fromSql = $@"{QUERY_USERPROFILES}
    JOIN
	    (SELECT 
		    u.ID_USER
	    FROM
		    WFPROCESS_PARTICIPANT pp
	    JOIN
		    WFPARTICIPANT p on pp.ID_PARTICIPANT = p.ID_PARTICIPANT
	    JOIN 
		    WFPARTICIPANT_MAPTO pm on pm.ID_PARTICIPANT = p.ID_PARTICIPANT
	    LEFT JOIN 
		    USERS_GROUPS ug on pm.ID_GROUP = ug.ID_GROUP
	    JOIN
		    USERS u on u.ID_DIRECTORY = pm.ID_DIRECTORY OR u.ID_USER = ug.ID_USER OR u.ID_USER = pm.ID_USER
	    WHERE
		    pp.ID_PROCESS = @processId
		    AND p.GLOBAL_SCOPE = 'N'
		    AND p.NAME = @name
		    AND u.ACTIVE = 'Y'
	    GROUP BY u.ID_USER) filtered ON filtered.ID_USER = u.ID_USER{(string.IsNullOrWhiteSpace(q) ? "" : $@"
WHERE
    {FILTER_SEARCHUSERS}")}
";

                comm.Parameters.AddWithValue("@processId", processId);
                comm.Parameters.AddWithValue("@name", name);
                comm.Parameters.AddWithValue("@query", q + "%");
                results = comm.CreateQueryResult<T>(fromSql);

                comm.CommandText = $@"SELECT {SELECT_USERPROFILES}{fromSql}
ORDER BY
    LASTNAME, FIRSTNAME
OFFSET {(page - 1) * pageSize} ROWS
FETCH NEXT {pageSize} ROWS ONLY
";
                using (var r = comm.ExecuteReader())
                {
                    var factory = new T();
                    factory.GetOrdinals(r, SOURCE, extraAttributes, null);
                    while (r.Read())
                    {
                        results.Rows.Add(factory.CreateInstance(r, SOURCE, extraAttributes));
                    }
                }
            }

            return results;
        }
        public QueryResult<Dictionary<string, object>> GetGlobalListItems(string listName, string[] columns, string sortBy, bool sortAsc, string filterColumnName, string filterComparison, string filterValue, string filterLanguage = "default", int page = 1, int pageSize = 20)
        {
            Dictionary<string, object> row = null;
            var rows = new List<Dictionary<string, object>>();
            var addRow = true;
            int total = 0;
            using (var comm = Connection.CreateCommand())
            {
                EnsureConnectionIsOpen();
                comm.CommandText = @"
SELECT 
    NB_ITEMS, -- 0
	FIELD1_NAME,-- 1
	FIELD1_TYPE,-- 2
	FIELD2_NAME,-- 3
	FIELD2_TYPE,-- 4
	FIELD3_NAME,
	FIELD3_TYPE,
	FIELD4_NAME,
	FIELD4_TYPE,
	FIELD5_NAME,
	FIELD5_TYPE,
	FIELD6_NAME,
	FIELD6_TYPE,
	FIELD7_NAME,
	FIELD7_TYPE,
	FIELD8_NAME,
	FIELD8_TYPE,
	FIELD9_NAME,
	FIELD9_TYPE,
	FIELD10_NAME,
	FIELD10_TYPE,
	FIELD11_NAME,
	FIELD11_TYPE,
	FIELD12_NAME,
	FIELD12_TYPE,
	FIELD13_NAME,
	FIELD13_TYPE,
	FIELD14_NAME,
	FIELD14_TYPE,
	FIELD15_NAME,
	FIELD15_TYPE,
	FIELD16_NAME,
	FIELD16_TYPE,
	FIELD17_NAME,
	FIELD17_TYPE,
	FIELD18_NAME,
	FIELD18_TYPE,
	FIELD19_NAME,
	FIELD19_TYPE,
	FIELD20_NAME,--39
	FIELD20_TYPE,--40
	[LANGUAGE],--41
	FIELD1_TEXT,--42
	FIELD1_NUMERIC,--43
	FIELD1_DATE,--44
	FIELD2_TEXT,--45
	FIELD2_NUMERIC,
	FIELD2_DATE,
	FIELD3_TEXT,
	FIELD3_NUMERIC,
	FIELD3_DATE,
	FIELD4_TEXT,
	FIELD4_NUMERIC,
	FIELD4_DATE,
	FIELD5_TEXT,
	FIELD5_NUMERIC,
	FIELD5_DATE,
	FIELD6_TEXT,
	FIELD6_NUMERIC,
	FIELD6_DATE,
	FIELD7_TEXT,
	FIELD7_NUMERIC,
	FIELD7_DATE,
	FIELD8_TEXT,
	FIELD8_NUMERIC,
	FIELD8_DATE,
	FIELD9_TEXT,
	FIELD9_NUMERIC,
	FIELD9_DATE,
	FIELD10_TEXT,
	FIELD10_NUMERIC,
	FIELD10_DATE,
	FIELD11_TEXT,
	FIELD11_NUMERIC,
	FIELD11_DATE,
	FIELD12_TEXT,
	FIELD12_NUMERIC,
	FIELD12_DATE,
	FIELD13_TEXT,
	FIELD13_NUMERIC,
	FIELD13_DATE,
	FIELD14_TEXT,
	FIELD14_NUMERIC,
	FIELD14_DATE,
	FIELD15_TEXT,
	FIELD15_NUMERIC,
	FIELD15_DATE,
	FIELD16_TEXT,
	FIELD16_NUMERIC,
	FIELD16_DATE,
	FIELD17_TEXT,
	FIELD17_NUMERIC,
	FIELD17_DATE,
	FIELD18_TEXT,
	FIELD18_NUMERIC,
	FIELD18_DATE,
	FIELD19_TEXT,
	FIELD19_NUMERIC,
	FIELD19_DATE,
	FIELD20_TEXT,
	FIELD20_NUMERIC,
	FIELD20_DATE
FROM 
    [WFLIST] lists
    JOIN 
    [WFLIST_ITEMS] items ON lists.ID_LIST = items.ID_LIST
WHERE lists.[NAME] = @listName
ORDER BY 
    DISP_ORDER, CASE WHEN [LANGUAGE] = 'default' THEN '_' ELSE [LANGUAGE] END
";
                comm.Parameters.AddWithValue("@listName", listName);
                using (var reader = comm.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        total = reader.GetInt32(0);
                        int field = 1;
                        int fieldValue = 42;
                        string lang = reader.GetString(41);
                        string fieldName;
                        if (lang == "default")
                        {
                            if (row != null && addRow)
                            {
                                rows.Add(row);
                            }
                            row = new Dictionary<string, object>();
                            addRow = true;
                        }
                        while ((fieldName = field > 39 || reader.IsDBNull(field) ? null : reader.GetString(field)) != null)
                        {
                            string valueType = reader.GetString(field + 1);
                            DateTime? dateValue = null;
                            double? numericValue = null;
                            string stringValue = null;
                            object value = null;
                            switch (valueType)
                            {
                                case "DATE":
                                    if (!reader.IsDBNull(fieldValue + 2))
                                    {
                                        value = dateValue = reader.GetDateTime(fieldValue + 2);
                                    }
                                    break;
                                case "NUMERIC":
                                    if (!reader.IsDBNull(fieldValue + 1))
                                    {
                                        value = numericValue = reader.GetDouble(fieldValue + 1);
                                    }
                                    break;
                                default:
                                    value = stringValue = reader.IsDBNull(fieldValue) ? null : reader.GetString(fieldValue);
                                    break;
                            }
                            if (filterLanguage == lang && fieldName.Equals(filterColumnName, StringComparison.InvariantCultureIgnoreCase))
                            {
                                Type t = typeof(string);
                                switch (valueType)
                                {
                                    case "DATE":
                                        DateTime filterDateValue;
                                        switch (filterComparison)
                                        {
                                            case "IsNotNull":
                                                addRow = dateValue.HasValue;
                                                break;
                                            case "IsNull":
                                                addRow = !dateValue.HasValue;
                                                break;
                                            case "LessThanOrEqual":
                                                DateTime.TryParse(filterValue, out filterDateValue);
                                                addRow = dateValue <= filterDateValue;
                                                break;
                                            case "LessThan":
                                                DateTime.TryParse(filterValue, out filterDateValue);
                                                addRow = dateValue < filterDateValue;
                                                break;
                                            case "GreaterThanOrEqual":
                                                DateTime.TryParse(filterValue, out filterDateValue);
                                                addRow = dateValue >= filterDateValue;
                                                break;
                                            case "GreaterThan":
                                                DateTime.TryParse(filterValue, out filterDateValue);
                                                addRow = dateValue > filterDateValue;
                                                break;
                                            case "DoesNotEqual":
                                                DateTime.TryParse(filterValue, out filterDateValue);
                                                addRow = dateValue != filterDateValue;
                                                break;
                                            default:
                                                DateTime.TryParse(filterValue, out filterDateValue);
                                                addRow = dateValue == filterDateValue;
                                                break;
                                        }
                                        break;
                                    case "NUMERIC":
                                        double filterNumericValue;
                                        switch (filterComparison)
                                        {
                                            case "IsNotNull":
                                                addRow = numericValue.HasValue;
                                                break;
                                            case "IsNull":
                                                addRow = !numericValue.HasValue;
                                                break;
                                            case "LessThanOrEqual":
                                                double.TryParse(filterValue, out filterNumericValue);
                                                addRow = numericValue <= filterNumericValue;
                                                break;
                                            case "LessThan":
                                                double.TryParse(filterValue, out filterNumericValue);
                                                addRow = numericValue < filterNumericValue;
                                                break;
                                            case "GreaterThanOrEqual":
                                                double.TryParse(filterValue, out filterNumericValue);
                                                addRow = numericValue >= filterNumericValue;
                                                break;
                                            case "GreaterThan":
                                                double.TryParse(filterValue, out filterNumericValue);
                                                addRow = numericValue > filterNumericValue;
                                                break;
                                            case "DoesNotEqual":
                                                double.TryParse(filterValue, out filterNumericValue);
                                                addRow = numericValue != filterNumericValue;
                                                break;
                                            default:
                                                double.TryParse(filterValue, out filterNumericValue);
                                                addRow = numericValue == filterNumericValue;
                                                break;
                                        }
                                        break;
                                    default:
                                        switch (filterComparison)
                                        {
                                            case "Between":
                                                break;
                                            case "NotIn":
                                                break;
                                            case "In":
                                                break;
                                            case "IsNotNull":
                                                addRow = stringValue != null;
                                                break;
                                            case "IsNull":
                                                addRow = stringValue == null;
                                                break;
                                            case "EndsWith":
                                                addRow = stringValue.EndsWith(filterValue, StringComparison.InvariantCultureIgnoreCase);
                                                break;
                                            case "BeginsWith":
                                                addRow = stringValue.StartsWith(filterValue, StringComparison.InvariantCultureIgnoreCase);
                                                break;
                                            case "Contains":
                                                addRow = stringValue.IndexOf(filterValue, StringComparison.InvariantCultureIgnoreCase) >= 0;
                                                break;
                                            case "LessThanOrEqual":
                                                addRow = string.Compare(stringValue, filterValue) <= 0;
                                                break;
                                            case "LessThan":
                                                addRow = string.Compare(stringValue, filterValue) < 0;
                                                break;
                                            case "GreaterThanOrEqual":
                                                addRow = string.Compare(stringValue, filterValue) >= 0;
                                                break;
                                            case "GreaterThan":
                                                addRow = string.Compare(stringValue, filterValue) > 0;
                                                break;
                                            case "DoesNotEqual":
                                                addRow = !stringValue.Equals(filterValue, StringComparison.InvariantCultureIgnoreCase);
                                                break;
                                            default:
                                                addRow = stringValue.Equals(filterValue, StringComparison.InvariantCultureIgnoreCase);
                                                break;
                                        }
                                        break;
                                }
                            }
                            if (!addRow) break;
                            if (columns.Any(c => c.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase)))
                            {
                                row.Add(lang == "default" ? fieldName : $"{fieldName}_{lang}", value);
                            }
                            field += 2;
                            fieldValue += 3;
                        }
                    }
                }
                if (row != null && addRow)
                {
                    rows.Add(row);
                }
            }

            if (!string.IsNullOrEmpty(sortBy))
            {
                var sortByArr = sortBy.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (sortAsc)
                {
                    var sort = sortByArr[0].Trim();
                    var rowsOrdered = rows.OrderByDescending(r => r[sort] != null).ThenBy(r => r[sort]);
                    for (var i = 1; i < sortByArr.Length; i++)
                    {
                        var otherSort = sortByArr[i].Trim();
                        rowsOrdered = rowsOrdered.ThenBy(r => r[otherSort]);
                    }
                    rows = rowsOrdered.ToList();
                }
                else
                {
                    var rowsOrdered = rows.OrderByDescending(r => r[sortByArr[0].Trim()]);
                    for (var i = 1; i < sortByArr.Length; i++)
                    {
                        var sort = sortByArr[i].Trim();
                        rowsOrdered = rowsOrdered.ThenByDescending(r => r[sort]);
                    }
                    rows = rowsOrdered.ToList();
                }
            }

            return new QueryResult<Dictionary<string, object>>(rows.Skip(pageSize * (page - 1)).Take(pageSize).ToList(), total);
        }
        public byte[] GetFile(string processName, string name, int? processVersion = null)
        {
            byte[] file = null;
            using (var comm = Connection.CreateCommand())
            {
                EnsureConnectionIsOpen();
                var filters = new List<string>
                {
                    "DSV.VALUE_FILE_CONTENT IS NOT NULL",
                    "RD.NAME = @name",
                    "P.NAME = @processName"
                };
                comm.Parameters.AddWithValue("@name", name);
                comm.Parameters.AddWithValue("@processName", processName);
                if (processVersion.HasValue)
                {
                    filters.Add("P.VERSION = @version");
                    comm.Parameters.AddWithValue("@processVersion", processVersion.Value);
                }
                comm.CommandText = $@"SELECT 
	DSV.VALUE_FILE_CONTENT
FROM 
	WFDATASET_VALUE DSV 
	JOIN WFPROCESS_RELDATA PRD ON DSV.ID_DATASET = PRD.ID_DATASET_DEFAULT
	JOIN WFPROCESS P ON PRD.ID_PROCESS = P.ID_PROCESS
	JOIN WFRELDATA RD ON PRD.ID_RELDATA = RD.ID_RELDATA 
WHERE
    {string.Join(" AND ", filters)}
ORDER BY
	P.VERSION DESC";
                using (var reader = comm.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        file = reader.GetSqlBinary(0).Value;
                    }
                }
            }
            return file;
        }
        public byte[] GetFile(int processInstId, string name)
        {
            byte[] file = null;
            using (var comm = Connection.CreateCommand())
            {
                EnsureConnectionIsOpen();
                comm.CommandText = @"SELECT 
	DSV.VALUE_FILE_CONTENT
FROM 
	WFDATASET_VALUE DSV 
	JOIN WFPROCESS_INST_RELDATA PRD ON DSV.ID_DATASET = PRD.ID_DATASET 
	JOIN WFRELDATA RD ON PRD.ID_RELDATA = RD.ID_RELDATA 
WHERE 
	DSV.VALUE_FILE_CONTENT IS NOT NULL AND RD.NAME = @name AND ID_PROCESS_INST = @processInstId";
                comm.Parameters.AddWithValue("@name", name);
                comm.Parameters.AddWithValue("@processInstId", processInstId);
                using (var reader = comm.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        file = reader.GetSqlBinary(0).Value;
                    }
                }
            }
            return file;
        }
        public byte[] GetFile(int processInstId, int activityInstId, string name)
        {
            byte[] file = null;
            using (var comm = Connection.CreateCommand())
            {
                EnsureConnectionIsOpen();
                comm.CommandText = @"SELECT 
	DSV.VALUE_FILE_CONTENT
FROM 
	WFDATASET_VALUE DSV 
	JOIN WFACTIVITY_INST_RELDATA ARD ON DSV.ID_DATASET = ARD.ID_DATASET 
    JOIN WFACTIVITY_INST AI ON ARD.ID_PROCESS_INST = AI.ID_PROCESS_INST AND ARD.ID_ACTIVITY_INST = AI.ID_ACTIVITY_INST
	JOIN WFRELDATA RD ON ARD.ID_RELDATA = RD.ID_RELDATA 
WHERE 
	DSV.VALUE_FILE_CONTENT IS NOT NULL AND RD.NAME = @name
    AND AI.ID_STATE = 'closed' AND AI.ID_SUBSTATE = 'completed'
    AND ARD.ID_PROCESS_INST = @processInstId AND ARD.ID_ACTIVITY_INST = @activityInstId";
                comm.Parameters.AddWithValue("@name", name);
                comm.Parameters.AddWithValue("@processInstId", processInstId);
                comm.Parameters.AddWithValue("@activityInstId", activityInstId);
                using (var reader = comm.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        file = reader.GetSqlBinary(0).Value;
                    }
                }
            }
            return file;
        }
        public T GetFormData<T>(int processInstId, string name = "FORM_DATA")
            where T : DataSet, new()
        {
            var file = GetFile(processInstId, name);
            if (file == null)
            {
                return null;
            }
            var formData = new T();
            using (var stream = new MemoryStream(file))
            {
                formData.ReadXml(stream);
            }
            return formData;
        }
        public DataSet GetFormData(int processInstId, string name = "FORM_DATA") => GetFormData<DataSet>(processInstId, name);
        public T GetFormData<T>(int processInstId, int activityInstId, string name = "FORM_DATA")
            where T : DataSet, new()
        {
            var file = GetFile(processInstId, activityInstId, name);
            if (file == null)
            {
                return null;
            }
            var formData = new T();
            using (var stream = new MemoryStream(file))
            {
                formData.ReadXml(stream);
            }
            return formData;
        }
        public DataSet GetFormData(int processInstId, int activityInstId, string name = "FORM_DATA") => GetFormData<DataSet>(processInstId, activityInstId, name);
    }
}
