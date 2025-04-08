using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using ParadimeWeb.WorkflowGen.Data;
using ParadimeWeb.WorkflowGen.Web.UI.WebForms.Model;
using WorkflowGen.My.Globalization;

namespace ParadimeWeb.WorkflowGen.Web.UI.WebForms
{
    public static class DataSet
    {
        public static int ActivityId(this System.Data.DataSet ds) => (int)ds.GetConfigurationParam(ConfigurationColumn.ActivityId);
        public static void SetActivityId(this System.Data.DataSet ds, int value) => ds.SetConfigurationParam(ConfigurationColumn.ActivityId, value);
        public static int ActivityInstanceId(this System.Data.DataSet ds) => (int)ds.GetConfigurationParam(ConfigurationColumn.ActivityInstanceId);
        public static void SetActivityInstanceId(this System.Data.DataSet ds, int value) => ds.SetConfigurationParam(ConfigurationColumn.ActivityInstanceId, value);
        public static string ActivityName(this System.Data.DataSet ds) => (string)ds.GetConfigurationParam(ConfigurationColumn.ActivityName);
        public static void SetActivityName(this System.Data.DataSet ds, string value) => ds.SetConfigurationParam(ConfigurationColumn.ActivityName, value);
        public static string ActivityDescription(this System.Data.DataSet ds) => (string)ds.GetConfigurationParam(ConfigurationColumn.ActivityDescription);
        public static void SetActivityDescription(this System.Data.DataSet ds, string value) => ds.SetConfigurationParam(ConfigurationColumn.ActivityDescription, value);
        public static int ProcessInstanceId(this System.Data.DataSet ds) => (int)ds.GetConfigurationParam(ConfigurationColumn.ProcessInstanceId);
        public static void SetProcessInstanceId(this System.Data.DataSet ds, int value) => ds.SetConfigurationParam(ConfigurationColumn.ProcessInstanceId, value);
        public static int ProcessId(this System.Data.DataSet ds) => (int)ds.GetConfigurationParam(ConfigurationColumn.ProcessId);
        public static void SetProcessId(this System.Data.DataSet ds, int value) => ds.SetConfigurationParam(ConfigurationColumn.ProcessId, value);
        public static string ProcessName(this System.Data.DataSet ds) => (string)ds.GetConfigurationParam(ConfigurationColumn.ProcessName);
        public static void SetProcessName(this System.Data.DataSet ds, string value) => ds.SetConfigurationParam(ConfigurationColumn.ProcessName, value);
        public static int ProcessVersion(this System.Data.DataSet ds) => (int)ds.GetConfigurationParam(ConfigurationColumn.ProcessVersion);
        public static void SetProcessVersion(this System.Data.DataSet ds, int value) => ds.SetConfigurationParam(ConfigurationColumn.ProcessVersion, value);
        public static DateTime Modified(this System.Data.DataSet ds) => (DateTime)ds.GetConfigurationParam(ConfigurationColumn.Modified);
        public static void SetModified(this System.Data.DataSet ds, DateTime value) => ds.SetConfigurationParam(ConfigurationColumn.Modified, value);
        public static string AbsoluteUrl(this System.Data.DataSet ds) => (string)ds.GetConfigurationParam(ConfigurationColumn.AbsoluteUrl);
        public static void SetAbsoluteUrl(this System.Data.DataSet ds, string value) => ds.SetConfigurationParam(ConfigurationColumn.AbsoluteUrl, value);
        public static User Assignee(this System.Data.DataSet ds) => new User(ds.Tables[TableNames.Assignee].Rows[0]);
        public static void SetAssignee(this System.Data.DataSet ds, User value) => value.Set(ds.Tables[TableNames.Assignee].Rows[0]);
        public static User CurrentUser(this System.Data.DataSet ds) => new User(ds.Tables[TableNames.CurrentUser].Rows[0]);
        public static void SetCurrentUser(this System.Data.DataSet ds, User value) => value.Set(ds.Tables[TableNames.CurrentUser].Rows[0]);
        public static string FormArchiveFileName(this System.Data.DataSet ds) => (string)ds.GetParam(Table1Column.FormArchive);
        public static void SetFormArchiveFileName(this System.Data.DataSet ds, string value) => ds.SetParam(Table1Column.FormArchive, value);
        public static string FormAction(this System.Data.DataSet ds) => (string)ds.GetParam(Table1Column.FormAction);
        public static void SetFormAction(this System.Data.DataSet ds, string value) => ds.SetParam(Table1Column.FormAction, value);
        public static string Commands(this System.Data.DataSet ds) => (string)ds.GetParam(Table1Column.Commands);
        public static void SetCommands(this System.Data.DataSet ds, string value) => ds.SetParam(Table1Column.Commands, value);
        public static string FarCommands(this System.Data.DataSet ds) => (string)ds.GetParam(Table1Column.FarCommands);
        public static void SetFarCommands(this System.Data.DataSet ds, string value) => ds.SetParam(Table1Column.FarCommands, value);
        public static string MoreCommands(this System.Data.DataSet ds) => (string)ds.GetParam(Table1Column.MoreCommands);
        public static void SetMoreCommands(this System.Data.DataSet ds, string value) => ds.SetParam(Table1Column.MoreCommands, value);
        public static string RequiredFields(this System.Data.DataSet ds) => (string)ds.GetParam(Table1Column.RequiredFields);
        public static void SetRequiredFields(this System.Data.DataSet ds, string value) => ds.SetParam(Table1Column.RequiredFields, value);
        public static string ReadonlyFields(this System.Data.DataSet ds) => (string)ds.GetParam(Table1Column.ReadonlyFields);
        public static void SetReadonlyFields(this System.Data.DataSet ds, string value) => ds.SetParam(Table1Column.ReadonlyFields, value);

        public static void CreateColumn<T>(this System.Data.DataSet ds, string tableName, string colName, object defaultValue = null)
        {
            if (!ds.Tables[tableName].Columns.Contains(colName))
            {
                ds.Tables[tableName].Columns.Add(new DataColumn(colName, typeof(T)) { DefaultValue = defaultValue });
            }
        }
        public static void CreateColumn<T>(this System.Data.DataSet ds, string colName, object defaultValue = null)
        {
            CreateColumn<T>(ds, TableNames.Table1, colName, defaultValue);
        }

        public static void SetParam(this DataRow dr, string key, object value, bool onlyIfNull = false)
        {
            if (!dr.Table.Columns.Contains(key) || (onlyIfNull && dr[key] != DBNull.Value && dr[key] != null)) return;
            dr[key] = value == default ? DBNull.Value : value;
        }
        public static void SetParam(this System.Data.DataSet ds, string tableName, string key, object value, bool onlyIfNull = false)
        {
            ds.Tables[tableName].Rows[0].SetParam(key, value, onlyIfNull);
        }
        public static void SetParam(this System.Data.DataSet ds, string key, object value, bool onlyIfNull = false)
        {
            ds.Tables[TableNames.Table1].Rows[0].SetParam(key, value, onlyIfNull);
        }
        public static object GetParam(this System.Data.DataSet ds, string key)
        {
            return GetParam(ds, TableNames.Table1, key);
        }
        public static object GetParam(this System.Data.DataSet ds, string tableName, string key, int row = 0)
        {
            object val = null;
            var rows = ds.Tables[tableName].Rows;
            if (rows.Count > row)
            {
                val = rows[row][key];
            }
            return val == DBNull.Value ? null : val;
        }
        public static bool HasParam(this System.Data.DataSet ds, string key, string tableName = TableNames.Table1)
        {
            return ds.Tables.Contains(tableName) && ds.Tables[tableName].Columns.Contains(key);
        }

        public static void InitializeTable1(this System.Data.DataSet ds)
        {
            ds.CreateColumn<string>(Table1Column.FormArchive);
            ds.CreateColumn<string>(Table1Column.FormAction);
            ds.CreateColumn<string>(Table1Column.Commands);
            ds.CreateColumn<string>(Table1Column.FarCommands);
            ds.CreateColumn<string>(Table1Column.MoreCommands);
            ds.CreateColumn<string>(Table1Column.RequiredFields);
            ds.CreateColumn<string>(Table1Column.ReadonlyFields);
        }
        public static DataTable Table1(this System.Data.DataSet ds) => ds.Tables[TableNames.Table1];
        public static DataTable Comments(this System.Data.DataSet ds) => ds.Tables[TableNames.Comments];
        public static string CommentType(this DataRow row) => (string)row[CommentColumn.Type];
        public static string CommentAuthor(this DataRow row) => (string)row[CommentColumn.Author];
        public static string CommentUserName(this DataRow row) => (string)row[CommentColumn.UserName];
        public static DateTime CommentCreated(this DataRow row) => (DateTime)row[CommentColumn.Created];
        public static int CommentProcessInstanceId(this DataRow row) => (int)row[CommentColumn.ProcessInstanceId];
        public static string CommentProcessName(this DataRow row) => (string)row[CommentColumn.ProcessName];
        public static int CommentActivityInstanceId(this DataRow row) => (int)row[CommentColumn.ActivityInstanceId];
        public static string CommentActivityName(this DataRow row) => (string)row[CommentColumn.ActivityName];
        public static string Comment(this DataRow row) => (string)row[CommentColumn.Comment];
        public static void Initialize(this System.Data.DataSet ds, KeyValuePair<string, Type>[] currentUserExtendedAttributes)
        {
            ds.Namespace = "";
            ds.DataSetName = "NewDataSet";

            if (!ds.Tables.Contains(TableNames.Comments))
            {
                var table = ds.Tables.Add(TableNames.Comments);
                table.Columns.Add(CommentColumn.Type, typeof(string));
                table.Columns.Add(CommentColumn.Role, typeof(string));
                table.Columns.Add(CommentColumn.Author, typeof(string));
                table.Columns.Add(CommentColumn.UserName, typeof(string));
                table.Columns.Add(CommentColumn.Directory, typeof(string));
                table.Columns.Add(new DataColumn(CommentColumn.Created, typeof(DateTime)) { DateTimeMode = DataSetDateTime.Utc });
                table.Columns.Add(CommentColumn.ProcessInstanceId, typeof(int));
                table.Columns.Add(CommentColumn.ProcessName, typeof(string));
                table.Columns.Add(CommentColumn.ActivityInstanceId, typeof(int));
                table.Columns.Add(CommentColumn.ActivityName, typeof(string));
                table.Columns.Add(CommentColumn.Comment, typeof(string));
            }
            if (!ds.Tables.Contains(TableNames.Configuration))
            {
                var table = ds.Tables.Add(TableNames.Configuration);
                table.Columns.Add(ConfigurationColumn.ActivityDescription, typeof(string));
                table.Columns.Add(ConfigurationColumn.ActivityId, typeof(int));
                table.Columns.Add(ConfigurationColumn.ActivityInstanceId, typeof(int));
                table.Columns.Add(ConfigurationColumn.ActivityName, typeof(string));
                table.Columns.Add(ConfigurationColumn.ProcessId, typeof(int));
                table.Columns.Add(ConfigurationColumn.ProcessInstanceId, typeof(int));
                table.Columns.Add(ConfigurationColumn.ProcessName, typeof(string));
                table.Columns.Add(ConfigurationColumn.ProcessVersion, typeof(int));
                table.Columns.Add(ConfigurationColumn.AbsoluteUrl, typeof(string));
                table.Columns.Add(new DataColumn(ConfigurationColumn.Modified, typeof(DateTime)) { DateTimeMode = DataSetDateTime.Utc });
                table.Columns.Add(ConfigurationColumn.ServerVersion, typeof(string));
                table.Columns.Add(ConfigurationColumn.ClientVersion, typeof(string));
            }
            ds.CreateUserTable(TableNames.CurrentUser, currentUserExtendedAttributes);
            ds.CreateUserTable(TableNames.Assignee, currentUserExtendedAttributes);

            ds.InitializeTable1();
        }

        public static void InitializeParams(this System.Data.DataSet ds, int processInstanceId, int activityInstanceId, string absoluteUrl, string clientVersion, string[] currentUserExtendedAttributes = null)
        {
            if (ds.Tables[TableNames.Configuration].Rows.Count == 0)
            {
                ds.Tables[TableNames.Configuration].Rows.Add();
            }
            if (ds.Tables[TableNames.CurrentUser].Rows.Count == 0)
            {
                ds.Tables[TableNames.CurrentUser].Rows.Add();
            }
            if (ds.Tables[TableNames.Assignee].Rows.Count == 0)
            {
                ds.Tables[TableNames.Assignee].Rows.Add();
            }
            ds.SetParam(Table1Column.Commands, "", true);
            ds.SetParam(Table1Column.FarCommands, "", true);
            ds.SetParam(Table1Column.MoreCommands, "", true);
            ds.SetParam(Table1Column.RequiredFields, "", true);
            ds.SetParam(Table1Column.ReadonlyFields, "", true);
            ds.SetConfigurationParam(ConfigurationColumn.AbsoluteUrl, absoluteUrl);
            ds.SetConfigurationParam(ConfigurationColumn.ProcessInstanceId, processInstanceId);
            ds.SetConfigurationParam(ConfigurationColumn.ActivityInstanceId, activityInstanceId);
            ds.SetConfigurationParam(ConfigurationColumn.ServerVersion, Assembly.GetExecutingAssembly().GetName().Version.ToString(3));
            ds.SetConfigurationParam(ConfigurationColumn.ClientVersion, clientVersion);

            using (var db = new DataBaseContext())
            using (var comm = db.Connection.CreateCommand())
            {
                int assigneeId, assigneeRealId;
                db.EnsureConnectionIsOpen();
                comm.CommandText = @"SELECT 
    [p].[ID_PROCESS], 
    [p].[NAME], 
    [p].[VERSION],
    [a].[DESCRIPTION], 
    [a].[NAME],
    [a].[ID_ACTIVITY],
    [ai].[ID_USER_ASSIGNEE],
    [ai].[ID_USER_ASSIGNEE_REAL]
FROM 
    [WFPROCESS_INST] [pi] 
    JOIN [WFPROCESS] [p] ON [p].[ID_PROCESS] = [pi].[ID_PROCESS]
    JOIN [WFACTIVITY_INST] [ai] ON [ai].[ID_PROCESS_INST] = [pi].[ID_PROCESS_INST]
    JOIN [WFACTIVITY] [a] ON [a].[ID_ACTIVITY] = [ai].[ID_ACTIVITY] AND [a].[ID_PROCESS] = [p].[ID_PROCESS]
WHERE 
    [pi].[ID_PROCESS_INST] = @processInstanceId
    AND [ai].[ID_ACTIVITY_INST] = @activityInstanceId";
                comm.Parameters.AddWithValue("@processInstanceId", processInstanceId);
                comm.Parameters.AddWithValue("@activityInstanceId", activityInstanceId);
                using (var reader = comm.ExecuteReader())
                {
                    reader.Read();
                    ds.SetConfigurationParam(ConfigurationColumn.ProcessId, reader.GetInt32(0));
                    ds.SetConfigurationParam(ConfigurationColumn.ProcessName, reader.GetString(1));
                    ds.SetConfigurationParam(ConfigurationColumn.ProcessVersion, reader.GetInt32(2));
                    ds.SetConfigurationParam(ConfigurationColumn.ActivityDescription, reader.GetString(3));
                    ds.SetConfigurationParam(ConfigurationColumn.ActivityName, reader.GetString(4));
                    ds.SetConfigurationParam(ConfigurationColumn.ActivityId, reader.GetInt32(5));
                    assigneeId = reader.GetInt32(6);
                    assigneeRealId = reader.IsDBNull(7) ? assigneeId : reader.GetInt32(7);
                }

                var assigneeUser = db.GetUser<User>(assigneeId, currentUserExtendedAttributes);
                assigneeUser.Set(ds.Tables[TableNames.Assignee].Rows[0]);
                if (assigneeId == assigneeRealId)
                {
                    assigneeUser.Set(ds.Tables[TableNames.CurrentUser].Rows[0]);
                }
                else
                {
                    db.GetUser<User>(assigneeRealId, currentUserExtendedAttributes).Set(ds.Tables[TableNames.CurrentUser].Rows[0]);
                }
            }
        }

        internal static DataTable EnsureZipTable(this System.Data.DataSet ds, string colName)
        {
            var table = ds.Tables.Contains(TableNames.ZipFiles) ? ds.Tables[TableNames.ZipFiles] : ds.Tables.Add(TableNames.ZipFiles);
            if (table.Rows.Count == 0)
            {
                table.Rows.Add();
            }
            if (!table.Columns.Contains(colName)) 
            {
                table.Columns.Add(colName);
            }
            return table;
        }

        internal static void CreateUserTableColumns(this DataTable dt, params KeyValuePair<string, Type>[] extendedAttributes)
        {
            dt.Columns.Add(UserColumn.Id, typeof(int));
            dt.Columns.Add(UserColumn.UserName, typeof(string));
            dt.Columns.Add(UserColumn.EmployeeNumber, typeof(string));
            dt.Columns.Add(UserColumn.CommonName, typeof(string));
            dt.Columns.Add(UserColumn.FirstName, typeof(string));
            dt.Columns.Add(UserColumn.LastName, typeof(string));
            dt.Columns.Add(UserColumn.Email, typeof(string));
            dt.Columns.Add(UserColumn.JobTitle, typeof(string));
            dt.Columns.Add(UserColumn.Locale, typeof(string));
            dt.Columns.Add(UserColumn.TimezoneId, typeof(int));
            dt.Columns.Add(UserColumn.Directory, typeof(string));
            dt.Columns.Add(UserColumn.IsActive, typeof(bool));
            if (extendedAttributes != null)
            {
                foreach (var extendedAttribute in extendedAttributes)
                {
                    dt.Columns.Add(extendedAttribute.Key, extendedAttribute.Value);
                }
            }
        }

        public static DataTable CreateUserTable(this System.Data.DataSet ds, string tableName, params KeyValuePair<string, Type>[] extendedAttributes)
        {
            if (ds.Tables.Contains(tableName))
            {
                return ds.Tables[tableName];
            }
            var table = ds.Tables.Add(tableName);
            CreateUserTableColumns(table, extendedAttributes);
            return table;
        }

        public static DataTable CreateFilesTable(this System.Data.DataSet ds, string tableName)
        {
            if (ds.Tables.Contains(tableName))
            {
                return ds.Tables[tableName];
            }
            var table = ds.Tables.Add(tableName);
            table.Columns.Add("Field", typeof(string));
            return table;
        }

        public static string ApprovalRole(this DataRow row) => (string)row[ApprovalColumn.Role];
        public static int? ApprovalProcessInstId(this DataRow row) => row.IsNull(ApprovalColumn.ProcessInstId) ? (int?)null : (int)row[ApprovalColumn.ProcessInstId];
        public static int? ApprovalActivityInstId(this DataRow row) => row.IsNull(ApprovalColumn.ActivityInstId) ? (int?)null : (int)row[ApprovalColumn.ActivityInstId];
        public static string ApprovalApproverUserName(this DataRow row) => row.IsNull(ApprovalColumn.ApproverUserName) ? null : (string)row[ApprovalColumn.ApproverUserName];
        public static string ApprovalApproverEmployeeNumber(this DataRow row) => row.IsNull(ApprovalColumn.ApproverEmployeeNumber) ? null : (string)row[ApprovalColumn.ApproverEmployeeNumber];
        public static string ApprovalApproverEmail(this DataRow row) => row.IsNull(ApprovalColumn.ApproverEmail) ? null : (string)row[ApprovalColumn.ApproverEmail];
        public static string ApprovalApproverName(this DataRow row) => row.IsNull(ApprovalColumn.ApproverName) ? null : (string)row[ApprovalColumn.ApproverName];
        public static string ApprovalApproverDirectory(this DataRow row) => row.IsNull(ApprovalColumn.ApproverDirectory) ? null : (string)row[ApprovalColumn.ApproverDirectory];
        public static string ApprovalApprovedByUserName(this DataRow row) => row.IsNull(ApprovalColumn.ApprovedByUserName) ? null : (string)row[ApprovalColumn.ApprovedByUserName];
        public static string ApprovalApprovedByEmployeeNumber(this DataRow row) => row.IsNull(ApprovalColumn.ApprovedByEmployeeNumber) ? null : (string)row[ApprovalColumn.ApprovedByEmployeeNumber];
        public static string ApprovalApprovedByEmail(this DataRow row) => row.IsNull(ApprovalColumn.ApprovedByEmail) ? null : (string)row[ApprovalColumn.ApprovedByEmail];
        public static string ApprovalApprovedByName(this DataRow row) => row.IsNull(ApprovalColumn.ApprovedByName) ? null : (string)row[ApprovalColumn.ApprovedByName];
        public static string ApprovalApprovedByDirectory(this DataRow row) => row.IsNull(ApprovalColumn.ApprovedByDirectory) ? null : (string)row[ApprovalColumn.ApprovedByDirectory];
        public static string Approval(this DataRow row) => row.IsNull(ApprovalColumn.Approval) ? null : (string)row[ApprovalColumn.Approval];
        public static DateTime? ApprovalApproved(this DataRow row) => row.IsNull(ApprovalColumn.Approved) ? (DateTime?)null : (DateTime)row[ApprovalColumn.Approved];
        public static DataTable Approvals(this System.Data.DataSet ds) => ds.Tables[TableNames.Approvals];
        public static DataTable CreateApprovalsTable(this System.Data.DataSet ds)
        {
            DataTable dt;
            if (ds.Tables.Contains(TableNames.Approvals))
            {
                dt = ds.Tables[TableNames.Approvals];
            }
            else
            {
                dt = ds.Tables.Add(TableNames.Approvals);
                dt.Columns.Add(ApprovalColumn.ProcessInstId, typeof(int));
                dt.Columns.Add(ApprovalColumn.ActivityInstId, typeof(int));
                dt.Columns.Add(ApprovalColumn.Role, typeof(string));
                dt.Columns.Add(ApprovalColumn.ApproverUserName, typeof(string));
                dt.Columns.Add(ApprovalColumn.ApproverEmployeeNumber, typeof(string));
                dt.Columns.Add(ApprovalColumn.ApproverEmail, typeof(string));
                dt.Columns.Add(ApprovalColumn.ApproverName, typeof(string));
                dt.Columns.Add(ApprovalColumn.ApproverDirectory, typeof(string));
                dt.Columns.Add(ApprovalColumn.ApprovedByUserName, typeof(string));
                dt.Columns.Add(ApprovalColumn.ApprovedByEmployeeNumber, typeof(string));
                dt.Columns.Add(ApprovalColumn.ApprovedByEmail, typeof(string));
                dt.Columns.Add(ApprovalColumn.ApprovedByName, typeof(string));
                dt.Columns.Add(ApprovalColumn.ApprovedByDirectory, typeof(string));
                dt.Columns.Add(ApprovalColumn.Approval, typeof(string));
                dt.Columns.Add(new DataColumn(ApprovalColumn.Approved, typeof(DateTime)) { DateTimeMode = DataSetDateTime.Utc });
            }
            dt.PrimaryKey = new DataColumn[] { dt.Columns[ApprovalColumn.Role] };
            return dt;
        }
        public static void AddApprovalRow(this DataTable dt, string role, ApprovalType approval, DateTime approved, string approverUserName, string approverEmployeeNumber, string approverEmail, string approverName, string approverDirectory, string approvedByUserName, string approvedByEmployeeNumber, string approvedByEmail, string approvedByName, string approvedByDirectory, int processInstId, int activityInstId)
        {
            var r = dt.AddApprovalRow(role);
            r.SetApprovalRow(approval, approved, approverUserName, approverEmployeeNumber, approverEmail, approverName, approverDirectory, approvedByUserName, approvedByEmployeeNumber, approvedByEmail, approvedByName, approvedByDirectory, processInstId, activityInstId);
        }
        public static DataRow AddApprovalRow(this DataTable dt, string role, bool needed = true)
        {
            DataRow r;
            if (dt.Rows.Contains(role))
            {
                r = dt.Rows.Find(role);
            }
            else
            {
                r = dt.NewRow();
                r[ApprovalColumn.Role] = role;
                if (!needed)
                {
                    r[ApprovalColumn.Approval] = Model.Approval.NotNeeded;
                }
                dt.Rows.Add(r);
            }
            return r;
        }
        public static void SetApprovalRow(this DataRow r, ApprovalType approval, DateTime approved, string approverUserName, string approverEmployeeNumber, string approverEmail, string approverName, string approverDirectory, string approvedByUserName, string approvedByEmployeeNumber, string approvedByEmail, string approvedByName, string approvedByDirectory, int processInstId, int activityInstId, bool setApproverOnlyIfNull = false)
        {
            r[ApprovalColumn.Approval] = approval == ApprovalType.Rejected ? Model.Approval.Rejected : Model.Approval.Approved;
            r[ApprovalColumn.ProcessInstId] = processInstId;
            r[ApprovalColumn.ActivityInstId] = activityInstId;
            r[ApprovalColumn.Approved] = approved;
            r.SetParam(ApprovalColumn.ApproverUserName, approverUserName, setApproverOnlyIfNull);
            r.SetParam(ApprovalColumn.ApproverEmployeeNumber, approverEmployeeNumber, setApproverOnlyIfNull);
            r.SetParam(ApprovalColumn.ApproverEmail, approverEmail, setApproverOnlyIfNull);
            r.SetParam(ApprovalColumn.ApproverName, approverName, setApproverOnlyIfNull);
            r.SetParam(ApprovalColumn.ApproverDirectory, approverDirectory, setApproverOnlyIfNull);
            r[ApprovalColumn.ApprovedByUserName] = approvedByUserName;
            r[ApprovalColumn.ApprovedByEmployeeNumber] = approvedByEmployeeNumber;
            r[ApprovalColumn.ApprovedByEmail] = approvedByEmail;
            r[ApprovalColumn.ApprovedByName] = approvedByName;
            r[ApprovalColumn.ApprovedByDirectory] = approvedByDirectory;
        }
        public static void ResetApprovalRow(this DataRow r, string approval = Model.Approval.Pending)
        {
            r[ApprovalColumn.Approval] = approval;
            r[ApprovalColumn.ProcessInstId] =
            r[ApprovalColumn.ActivityInstId] =
            r[ApprovalColumn.Approved] =
            r[ApprovalColumn.ApproverUserName] =
            r[ApprovalColumn.ApproverEmployeeNumber] =
            r[ApprovalColumn.ApproverEmail] =
            r[ApprovalColumn.ApproverName] =
            r[ApprovalColumn.ApproverDirectory] =
            r[ApprovalColumn.ApprovedByUserName] =
            r[ApprovalColumn.ApprovedByEmployeeNumber] =
            r[ApprovalColumn.ApprovedByEmail] =
            r[ApprovalColumn.ApprovedByDirectory] =
            r[ApprovalColumn.ApprovedByName] = DBNull.Value;
        }
        public static void AddUserRow(this DataTable dt, User user)
        {
            var newRow = dt.NewRow();
            user.Set(newRow);
            dt.Rows.Add(newRow);
        }
        public static void AddCommentRow(this System.Data.DataSet ds, string type, string role, string author, string userName, string directory, DateTime created, int processInstanceId, string processName, int activityInstanceId, string activityName, string comment)
        {
            var dt = ds.Tables[TableNames.Comments];
            var newRow = dt.NewRow();
            newRow[CommentColumn.Type] = type;
            newRow[CommentColumn.Role] = role == null ? DBNull.Value : (object)role;
            newRow[CommentColumn.Author] = author;
            newRow[CommentColumn.UserName] = userName;
            newRow[CommentColumn.Directory] = directory;
            newRow[CommentColumn.Created] = created;
            newRow[CommentColumn.ProcessInstanceId] = processInstanceId;
            newRow[CommentColumn.ProcessName] = processName;
            newRow[CommentColumn.ActivityInstanceId] = activityInstanceId;
            newRow[CommentColumn.ActivityName] = activityName;
            newRow[CommentColumn.Comment] = comment;
            dt.Rows.InsertAt(newRow, 0);
        }
        public static object GetInitData(this System.Data.DataSet WfgDataSet, string Locale, TimeZoneInformation timezoneInfo)
        {
            var UTCOffset = timezoneInfo.NativeStructure.Bias + (timezoneInfo.IsDaylightSavingTime(DateTime.UtcNow) ? timezoneInfo.NativeStructure.DaylightBias : timezoneInfo.NativeStructure.StandardBias);
            UTCOffset = 0 - UTCOffset;
            var settings = new JsonSerializerSettings();
            settings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
            return JsonConvert.SerializeObject(new
            {
                Locale,
                TimeZoneInfo = new
                {
                    Name = timezoneInfo.DisplayName,
                    UTCOffset
                },
                WfgDataSet
            }, settings);
        }

        public static object GetConfigurationParam(this System.Data.DataSet ds, string key)
        {
            return GetParam(ds, TableNames.Configuration, key);
        }
        public static void SetConfigurationParam(this System.Data.DataSet ds, string key, object value, bool onlyIfNull = false)
        {
            SetParam(ds, TableNames.Configuration, key, value, onlyIfNull);
        }
        public static bool GetFormData(this System.Data.DataSet formData, DataBaseContext ctx, int processInstId, string name = "FORM_DATA", KeyValuePair<string, Type>[] currentUserExtendedAttributes = null)
        {
            var file = ctx.GetFile(processInstId, name);
            if (file == null)
            {
                return false;
            }
            formData.Initialize(currentUserExtendedAttributes);
            using (var stream = new MemoryStream(file))
            {
                formData.ReadXml(stream);
            }
            return true;
        }
        public static bool GetFormData(this System.Data.DataSet formData, DataBaseContext ctx, int processInstId, int activityInstId, string name = "FORM_DATA", KeyValuePair<string, Type>[] currentUserExtendedAttributes = null)
        {
            var file = ctx.GetFile(processInstId, activityInstId, name);
            if (file == null)
            {
                file = ctx.GetFile(processInstId, name);
                if (file == null)
                {
                    return false;
                }
            }
            formData.Initialize(currentUserExtendedAttributes);
            using (var stream = new MemoryStream(file))
            {
                formData.ReadXml(stream);
            }
            return true;
        }
    }
}
